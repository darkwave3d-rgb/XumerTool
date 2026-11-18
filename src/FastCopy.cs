// Copia veloce di immagini disco col protocollo "S2" di OpenCBM/d64copy:
// un piccolo programma 6502 (drivecode\*.a65) viene caricato nella RAM del drive
// e trasferisce i settori 2 bit alla volta usando le linee ATN/CLK/DATA.
using System;
using System.IO;
using System.Threading;

namespace XumerTool
{
    public partial class CbmDrive
    {
        /// <summary>Modello del drive ("1541", "1571", "1570", "1581") ricavato dal messaggio 73.</summary>
        public string Model;

        // come d64copy per il protocollo S2: default_interleave[] = { auto -1, original 17, serial1 4, serial2 13, parallel 7 }
        const int Interleave = 13;

        public static string ModelFromStatus(string st)
        {
            if (StatusCode(st) != 73) return null;
            foreach (string m in new[] { "1571", "1570", "1581", "1541" })
                if (st.Contains(m)) return m;
            return "?";
        }

        void EnsureModel()
        {
            if (Model != null) return;
            Log(Lang.T("Identificazione del drive (reset bus IEC)…"));
            x.BusReset();
            Thread.Sleep(2500);
            ReadStatus();
            if (Model == null) Model = "?";
        }

        /// <summary>
        /// Si può fare la copia veloce di questo tipo d'immagine con il drive collegato?
        /// Identifica il modello se serve (con un reset del bus, come ReadImageFast).
        /// </summary>
        public bool CanFastCopy(ImageKind kind)
        {
            EnsureModel();
            return FastSupported(kind);
        }

        public bool FastSupported(ImageKind kind)
        {
            if (kind == ImageKind.D81) return false;
            if (kind == ImageKind.D71) return Model == "1571";   // d64copy: le D71 richiedono un 1571
            return Model == "1571" || Model == "1570" || Model == "1541";
        }

        static byte[] DriveProgram(string name, int expectedOrigin)
        {
            using (Stream s = typeof(CbmDrive).Assembly.GetManifestResourceStream(name + ".a65"))
            {
                if (s == null) throw new Exception(Lang.T("Programma drive mancante: ") + name);
                string src = new StreamReader(s).ReadToEnd();
                int org;
                byte[] b = Asm6502.Assemble(src, out org);
                if (org != expectedOrigin) throw new Exception(name + Lang.T(": indirizzo di partenza inatteso"));
                return b;
            }
        }

        public void MemoryWrite(int addr, byte[] data)
        {
            for (int o = 0; o < data.Length; o += 32)
            {
                int n = Math.Min(32, data.Length - o);
                byte[] cmd = new byte[6 + n];
                cmd[0] = (byte)'M'; cmd[1] = (byte)'-'; cmd[2] = (byte)'W';
                cmd[3] = (byte)((addr + o) & 0xFF);
                cmd[4] = (byte)((addr + o) >> 8);
                cmd[5] = (byte)n;
                Array.Copy(data, o, cmd, 6, n);
                SendCommand(cmd, false);
            }
        }

        static int[] TrackOrder(ImageKind kind)
        {
            int tracks = Tracks(kind);
            int[] order = new int[tracks];
            if (kind == ImageKind.D71)
            {
                // come d64copy: alterna le due facce restando sulla stessa traccia fisica
                for (int t = 1; t <= 35; t++) { order[(t - 1) * 2] = t; order[(t - 1) * 2 + 1] = t + 35; }
            }
            else for (int t = 1; t <= tracks; t++) order[t - 1] = t;
            return order;
        }

        void StartS2(byte[] turbo)
        {
            MemoryWrite(0x0500, turbo);
            MemoryWrite(0x0700, DriveProgram("s2", 0x0700));
            SendCommand(Petscii.FromText("U4:"), false);   // esegue $0503
            x.IecSetRelease(0, Xum1541.IEC_CLOCK);
            DateTime t0 = DateTime.Now;
            while ((x.IecPoll() & Xum1541.IEC_CLOCK) == 0)
            {
                if ((DateTime.Now - t0).TotalMilliseconds > 3000)
                    throw new DriveException(Lang.T("Il drive non ha avviato il programma veloce"));
                Thread.Sleep(1);
            }
            x.IecSetRelease(Xum1541.IEC_ATN, 0);
            Thread.Sleep(20);
        }

        /// <summary>
        /// Chiude il programma veloce come close_disk() di OpenCBM: traccia 0 (con conferma), poi il byte
        /// del settore senza attendere l'ultima conferma (il drive libera subito le linee ed esce:
        /// il firmware potrebbe perdere quel segnale brevissimo e restare in attesa), infine linee a riposo.
        /// </summary>
        void StopS2()
        {
            try
            {
                x.Write(Xum1541.S2, new byte[] { 0 }, 0, 1);   // traccia 0 = fine
                S2WriteByteNoAck(0);
                Thread.Sleep(1);
                x.IecSetRelease(0, Xum1541.IEC_DATA);
                x.IecSetRelease(0, Xum1541.IEC_ATN);
                x.IecSetRelease(Xum1541.IEC_CLOCK, 0);
                Thread.Sleep(50);
            }
            catch (Exception)
            {
                Log(Lang.T("Uscita dal programma veloce non riuscita: reset del bus."));
                x.Abort();
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Un byte S2 pilotando a mano le linee IEC (come s2_write_byte_nohs di OpenCBM): due bit per volta
        /// su DATA, ATN fa da clock; si aspetta la conferma su CLK tranne dopo l'ultima coppia di bit.
        /// </summary>
        void S2WriteByteNoAck(byte c)
        {
            for (int i = 4; ; i--)
            {
                SetData((c & 1) != 0); c >>= 1;
                x.IecSetRelease(0, Xum1541.IEC_ATN);
                WaitClock(false);
                SetData((c & 1) != 0); c >>= 1;
                x.IecSetRelease(Xum1541.IEC_ATN, 0);
                if (i <= 1) return;
                WaitClock(true);
            }
        }

        void SetData(bool bit)
        {
            if (bit) x.IecSetRelease(Xum1541.IEC_DATA, 0);
            else x.IecSetRelease(0, Xum1541.IEC_DATA);
        }

        /// <summary>Aspetta che CLK sia attiva (true) o rilasciata (false), al massimo 2 secondi.</summary>
        void WaitClock(bool active)
        {
            DateTime t0 = DateTime.Now;
            while (((x.IecPoll() & Xum1541.IEC_CLOCK) != 0) != active)
            {
                if ((DateTime.Now - t0).TotalMilliseconds > 2000)
                    throw new DriveException(Lang.T("Il drive non risponde sulla linea CLK"));
                Thread.Sleep(1);
            }
        }

        string TurboName(bool write)
        {
            // come d64copy: il 1541 usa i suoi programmi, 1570 e 1571 quelli del 1571
            return (write ? "turbowrite" : "turboread") + (Model == "1541" ? "1541" : "1571");
        }

        public byte[] ReadImageFast(ImageKind kind)
        {
            EnsureModel();
            if (!FastSupported(kind)) throw new DriveException(Lang.T("Copia veloce non disponibile per ") + kind + Lang.T(" sul drive ") + Model);
            byte[] turbo = DriveProgram(TurboName(false), 0x0500);
            if (kind == ImageKind.D71) Command("U0>M1");
            Expect(Command("I0"), Lang.T("Inizializzazione disco"));

            int total = TotalBlocks(kind);
            byte[] img = new byte[total * 256];
            byte[] err = new byte[total];
            bool anyError = false;
            int done = 0;
            Log(Lang.T("Copia veloce (S2): disco -> ") + kind + " (" + total + Lang.T(" blocchi)…"));

            int oldTimeout = x.DataTimeoutMs;
            x.DataTimeoutMs = 30000;
            StartS2(turbo);
            try
            {
                foreach (int t in TrackOrder(kind))
                {
                    int n = SectorsPerTrack(kind, t);
                    bool[] got = new bool[n];
                    int se = 0;
                    for (int k = 0; k < n; k++)
                    {
                        CheckCancel();
                        while (got[se]) se = (se + 1) % n;
                        Block(t, se, BlockState.Working);
                        x.Write(Xum1541.S2, new byte[] { (byte)t, (byte)se }, 0, 2);
                        byte[] r = x.Read(Xum1541.S2, 257);
                        if (r.Length != 257) throw new IOException(Lang.T("Risposta incompleta dal drive (") + r.Length + Lang.T(" byte)"));
                        int idx = BlockOffset(kind, t, se) / 256;
                        if (r[0] == 0)
                        {
                            Array.Copy(r, 1, img, idx * 256, 256);
                            err[idx] = 1;
                            Block(t, se, BlockState.Done);
                        }
                        else
                        {
                            anyError = true;
                            err[idx] = r[0];
                            Block(t, se, BlockState.Error);
                            Log(string.Format(Lang.T("Errore traccia {0} settore {1}: codice {2} (errore DOS {3})"), t, se, r[0], r[0] + 18));
                        }
                        got[se] = true;
                        Progress(++done, total);
                        se = (se + Interleave) % n;
                    }
                }
            }
            catch (OperationCanceledException) { x.DataTimeoutMs = oldTimeout; StopS2(); throw; }
            catch { x.DataTimeoutMs = oldTimeout; x.Abort(); throw; }
            x.DataTimeoutMs = oldTimeout;
            StopS2();
            ReadStatus();

            if (!anyError) return img;
            Log(Lang.T("Il disco contiene errori: salvo la tabella errori nell'immagine."));
            return Petscii.Concat(img, err);
        }

        public void WriteImageFast(ImageKind kind, byte[] img, bool formatFirst)
        {
            EnsureModel();
            if (!FastSupported(kind)) throw new DriveException(Lang.T("Scrittura veloce non disponibile per ") + kind + Lang.T(" sul drive ") + Model);
            int total = TotalBlocks(kind);
            if (img.Length < total * 256) throw new DriveException(Lang.T("Immagine troppo corta"));
            byte[] turbo = DriveProgram(TurboName(true), 0x0500);
            if (kind == ImageKind.D71) Command("U0>M1");
            if (formatFirst)
            {
                string name, id;
                ImageHeader(kind, img, out name, out id);
                Format(name.ToUpperInvariant(), id);
            }
            else Expect(Command("I0"), Lang.T("Inizializzazione disco"));

            Log(Lang.T("Scrittura veloce (S2): ") + kind + Lang.T(" -> disco (") + total + Lang.T(" blocchi)…"));
            int oldTimeout = x.DataTimeoutMs;
            x.DataTimeoutMs = 30000;
            int done = 0;
            string failure = null;
            StartS2(turbo);
            try
            {
                byte[] buf = new byte[258];
                foreach (int t in TrackOrder(kind))
                {
                    int n = SectorsPerTrack(kind, t);
                    bool[] put = new bool[n];
                    int se = 0;
                    for (int k = 0; k < n && failure == null; k++)
                    {
                        CheckCancel();
                        while (put[se]) se = (se + 1) % n;
                        Block(t, se, BlockState.Working);
                        buf[0] = (byte)t; buf[1] = (byte)se;
                        Array.Copy(img, BlockOffset(kind, t, se), buf, 2, 256);
                        for (int attempt = 0; ; attempt++)
                        {
                            x.Write(Xum1541.S2, buf, 0, 258);
                            byte[] r = x.Read(Xum1541.S2, 1);
                            if (r.Length != 1) throw new IOException(Lang.T("Nessuna risposta dal drive"));
                            if (r[0] == 0) break;
                            if (r[0] == 8) { failure = Lang.T("il disco è protetto da scrittura"); break; }
                            Log(string.Format(Lang.T("Errore scrittura traccia {0} settore {1}: codice {2}"), t, se, r[0]));
                            if (attempt >= 2) { failure = string.Format(Lang.T("errore di scrittura su traccia {0} settore {1} (codice {2})"), t, se, r[0]); break; }
                        }
                        put[se] = true;
                        Block(t, se, failure == null ? BlockState.Done : BlockState.Error);
                        Progress(++done, total);
                        se = (se + Interleave) % n;
                    }
                    if (failure != null) break;
                }
            }
            catch (OperationCanceledException) { x.DataTimeoutMs = oldTimeout; StopS2(); throw; }
            catch { x.DataTimeoutMs = oldTimeout; x.Abort(); throw; }
            x.DataTimeoutMs = oldTimeout;
            StopS2();
            if (failure != null) throw new DriveException(Lang.T("Scrittura interrotta: ") + failure);
            Command("I0");
            Log(Lang.T("Immagine scritta."));
        }

        /// <summary>Lettura di un singolo blocco col metodo standard (per verifiche).</summary>
        public byte[] ReadBlock(int t, int s)
        {
            OpenFile(2, Petscii.FromText("#"));
            try
            {
                Expect(ReadStatus(false), Lang.T("Apertura buffer"));
                SendCommand(Petscii.FromText("U1 2 0 " + t + " " + s), false);
                Expect(ReadStatus(false), Lang.T("Lettura blocco"));
                if (!x.Talk(Dev, 2)) NotPresent();
                byte[] b = x.RawRead(256);
                x.Untalk();
                return b;
            }
            finally { CloseFile(2); }
        }
    }
}
