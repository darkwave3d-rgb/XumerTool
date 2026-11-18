// Scrittura del firmware sul Pro Micro (ATmega32U4) attraverso il bootloader Caterina,
// con il protocollo AVR109 (lo stesso che usa avrdude -c avr109). Il bootloader non viene toccato.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Threading;

namespace XumerTool
{
    static class IntelHex
    {
        /// <summary>Legge un file Intel HEX; restituisce l'immagine (byte vuoti a 0xFF) e la lunghezza usata.</summary>
        public static byte[] Load(string path, int maxSize, out int length)
        {
            byte[] img = new byte[maxSize];
            for (int i = 0; i < img.Length; i++) img[i] = 0xFF;
            length = 0;
            int baseAddr = 0, lineNo = 0;
            bool eof = false;
            foreach (string raw in File.ReadAllLines(path))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] != ':' || line.Length < 11 || (line.Length - 1) % 2 != 0)
                    throw new InvalidDataException(Lang.T("File HEX non valido alla riga ") + lineNo);
                byte[] b = new byte[(line.Length - 1) / 2];
                for (int i = 0; i < b.Length; i++)
                    b[i] = byte.Parse(line.Substring(1 + i * 2, 2), NumberStyles.HexNumber);
                int count = b[0];
                if (b.Length != count + 5) throw new InvalidDataException(Lang.T("Lunghezza errata alla riga ") + lineNo);
                byte sum = 0;
                foreach (byte x in b) sum += x;
                if (sum != 0) throw new InvalidDataException(Lang.T("Checksum errato alla riga ") + lineNo);
                int addr = (b[1] << 8) | b[2];
                int type = b[3];
                if (type == 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int a = baseAddr + addr + i;
                        if (a >= maxSize) throw new InvalidDataException(Lang.T("Il firmware è troppo grande per il Pro Micro (oltre ") + maxSize + Lang.T(" byte): sovrascriverebbe il bootloader."));
                        img[a] = b[4 + i];
                        length = Math.Max(length, a + 1);
                    }
                }
                else if (type == 1) { eof = true; break; }
                else if (type == 2) baseAddr = ((b[4] << 8) | b[5]) << 4;
                else if (type == 4) baseAddr = ((b[4] << 8) | b[5]) << 16;
            }
            if (!eof) throw new InvalidDataException(Lang.T("Il file HEX è incompleto (manca la riga finale)."));
            if (length == 0) throw new InvalidDataException(Lang.T("Il file HEX non contiene dati."));
            return img;
        }
    }

    class Avr109 : IDisposable
    {
        public const int AppFlashSize = 0x7000; // 28 KB: oltre c'è il bootloader Caterina
        readonly SerialPort port;

        public Avr109(string portName)
        {
            port = new SerialPort(portName, 57600) { ReadTimeout = 2000, WriteTimeout = 2000, DtrEnable = true };
            port.Open();
            Thread.Sleep(50);
            port.DiscardInBuffer();
        }

        byte[] Cmd(byte[] send, int reply)
        {
            port.Write(send, 0, send.Length);
            byte[] r = new byte[reply];
            int got = 0;
            while (got < reply) got += port.Read(r, got, reply - got);
            return r;
        }

        void Expect(byte[] send, string what)
        {
            byte[] r = Cmd(send, 1);
            if (r[0] != '\r') throw new IOException(Lang.T("Il bootloader ha rifiutato il comando: ") + what);
        }

        public void Program(byte[] img, int length, Action<string> log, Action<int, int> progress)
        {
            string id = Encoding.ASCII.GetString(Cmd(new[] { (byte)'S' }, 7));
            log(Lang.T("Bootloader: ") + id);
            byte[] sig = Cmd(new[] { (byte)'s' }, 3);
            log(string.Format(Lang.T("Firma del chip: {0:X2} {1:X2} {2:X2}"), sig[2], sig[1], sig[0]));
            if (!(sig[2] == 0x1E && sig[1] == 0x95 && sig[0] == 0x87))
                throw new IOException(Lang.T("Il chip non è un ATmega32U4: scrittura annullata."));
            byte[] bs = Cmd(new[] { (byte)'b' }, 3);
            int block = bs[0] == 'Y' ? (bs[1] << 8) | bs[2] : 128;
            if (block <= 0 || block > 1024) block = 128;
            Expect(new[] { (byte)'P' }, Lang.T("modalità di programmazione"));

            int total = (length + block - 1) / block * block;
            // scrittura
            for (int a = 0; a < total; a += block)
            {
                int w = a / 2;
                Expect(new byte[] { (byte)'A', (byte)(w >> 8), (byte)w }, Lang.T("indirizzo"));
                byte[] cmd = new byte[4 + block];
                cmd[0] = (byte)'B'; cmd[1] = (byte)(block >> 8); cmd[2] = (byte)block; cmd[3] = (byte)'F';
                Array.Copy(img, a, cmd, 4, block);
                Expect(cmd, Lang.T("scrittura flash"));
                progress(a + block, total * 2);
            }
            // verifica
            for (int a = 0; a < total; a += block)
            {
                int w = a / 2;
                Expect(new byte[] { (byte)'A', (byte)(w >> 8), (byte)w }, Lang.T("indirizzo"));
                byte[] data = Cmd(new byte[] { (byte)'g', (byte)(block >> 8), (byte)block, (byte)'F' }, block);
                for (int i = 0; i < block; i++)
                    if (data[i] != img[a + i])
                        throw new IOException(string.Format(Lang.T("Verifica fallita all'indirizzo {0:X4}: il firmware non è stato scritto correttamente. Riprova."), a + i));
                progress(total + a + block, total * 2);
            }
            Expect(new[] { (byte)'L' }, Lang.T("fine programmazione"));
            try { Expect(new[] { (byte)'E' }, Lang.T("uscita")); } catch (Exception) { } // il chip si riavvia subito
        }

        /// <summary>Aspetta che compaia una nuova porta seriale (il bootloader del Pro Micro).</summary>
        public static string WaitForNewPort(string[] before, int timeoutMs, Func<bool> cancelled)
        {
            HashSet<string> old = new HashSet<string>(before, StringComparer.OrdinalIgnoreCase);
            DateTime end = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < end)
            {
                if (cancelled()) throw new OperationCanceledException();
                foreach (string p in SerialPort.GetPortNames())
                    if (!old.Contains(p)) { Thread.Sleep(300); return p; }
                Thread.Sleep(100);
            }
            return null;
        }

        public void Dispose()
        {
            try { if (port.IsOpen) port.Close(); } catch { }
            port.Dispose();
        }
    }

    static class Firmware
    {
        public static readonly string[] Choices =
        {
            Lang.T("xum1541 v08 per Pro Micro (OpenCBM)"),
            Lang.T("xum1541 v08 per Pro Micro con 7406 (OpenCBM)"),
            Lang.T("File .hex dal PC…"),
        };

        static readonly string[] Urls =
        {
            "https://raw.githubusercontent.com/OpenCBM/OpenCBM/master/xum1541/xum1541-PROMICRO-v08.hex",
            "https://raw.githubusercontent.com/OpenCBM/OpenCBM/master/xum1541/xum1541-PROMICRO_7406-v08.hex",
        };

        public static string Dir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XUMERTool", "firmware");
            Directory.CreateDirectory(d);
            return d;
        }

        /// <summary>Scarica (una volta sola) il firmware ufficiale scelto e ne restituisce il percorso.</summary>
        public static string Get(int choice, Action<string> log)
        {
            string url = Urls[choice];
            string path = Path.Combine(Dir(), url.Substring(url.LastIndexOf('/') + 1));
            if (File.Exists(path)) return path;
            log(Lang.T("Scarico ") + url);
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "XUMERTool");
                string tmp = path + ".tmp";
                wc.DownloadFile(url, tmp);
                int len;
                IntelHex.Load(tmp, Avr109.AppFlashSize, out len); // controlla che sia un HEX valido
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            return path;
        }
    }
}
