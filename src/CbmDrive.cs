// Operazioni DOS sul drive Commodore (1541/1571/1581) tramite xum1541.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XumerTool
{
    public class DriveException : Exception
    {
        public DriveException(string msg) : base(msg) { }
    }

    public static class Petscii
    {
        /// <summary>Testo PC -> PETSCII (le lettere diventano maiuscole C64).</summary>
        public static byte[] FromText(string s)
        {
            byte[] b = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 'a' && c <= 'z') b[i] = (byte)(c - 32);
                else if (c >= 0x20 && c <= 0x5D) b[i] = (byte)c;
                else if (c == '£') b[i] = 0x5C;
                else b[i] = (byte)'?';
            }
            return b;
        }

        public static char ToChar(byte b)
        {
            if (b >= 0x41 && b <= 0x5A) return (char)b;
            if (b >= 0xC1 && b <= 0xDA) return (char)(b - 0xC1 + 'a');
            if (b >= 0x61 && b <= 0x7A) return (char)(b - 0x61 + 'a');
            if (b == 0x5C) return '£';
            if (b == 0x5E) return '↑';
            if (b == 0x5F) return '←';
            if (b >= 0x20 && b <= 0x5D) return (char)b;
            if (b == 0xA0) return ' ';
            if (b == 0x0D) return '\n';
            return '·';
        }

        public static string ToText(byte[] b) { return ToText(b, 0, b.Length); }

        public static string ToText(byte[] b, int offset, int count)
        {
            StringBuilder sb = new StringBuilder(count);
            for (int i = offset; i < offset + count; i++) sb.Append(ToChar(b[i]));
            return sb.ToString();
        }

        public static byte[] Concat(params byte[][] parts)
        {
            int n = 0;
            foreach (byte[] p in parts) n += p.Length;
            byte[] r = new byte[n];
            int o = 0;
            foreach (byte[] p in parts) { Array.Copy(p, 0, r, o, p.Length); o += p.Length; }
            return r;
        }
    }

    public class DirEntry
    {
        public int Blocks;
        public byte[] NameBytes;
        public string Name;
        public string Type;      // "PRG", "SEQ", "USR", "REL", "DEL", "CBM", "DIR"
        public bool Splat;       // file non chiuso (*)
        public bool Locked;      // protetto (<)

        public char TypeLetter
        {
            get
            {
                switch (Type)
                {
                    case "SEQ": return 'S';
                    case "USR": return 'U';
                    case "REL": return 'L';
                    default: return 'P';
                }
            }
        }
    }

    public class DiskDirectory
    {
        public string DiskName = "";
        public string DiskId = "";
        public int BlocksFree = -1;
        public List<DirEntry> Entries = new List<DirEntry>();

        /// <summary>Interpreta il listato BASIC ottenuto aprendo "$".</summary>
        public static DiskDirectory Parse(byte[] d)
        {
            DiskDirectory dir = new DiskDirectory();
            int p = 2; // indirizzo di caricamento
            bool first = true;
            while (p + 4 <= d.Length)
            {
                if (d[p] == 0 && d[p + 1] == 0) break;
                int blocks = d[p + 2] | (d[p + 3] << 8);
                p += 4;
                int start = p;
                while (p < d.Length && d[p] != 0) p++;
                int len = p - start;
                p++;
                byte[] line = new byte[len];
                Array.Copy(d, start, line, 0, len);

                int q1 = Array.IndexOf(line, (byte)'"');
                int q2 = q1 >= 0 ? Array.IndexOf(line, (byte)'"', q1 + 1) : -1;
                if (first)
                {
                    first = false;
                    if (q1 >= 0 && q2 > q1)
                    {
                        dir.DiskName = Petscii.ToText(line, q1 + 1, q2 - q1 - 1).TrimEnd();
                        dir.DiskId = Petscii.ToText(line, q2 + 1, line.Length - q2 - 1).Trim();
                    }
                    continue;
                }
                if (q1 < 0 || q2 < 0)
                {
                    dir.BlocksFree = blocks; // "BLOCKS FREE."
                    continue;
                }
                DirEntry e = new DirEntry();
                e.Blocks = blocks;
                e.NameBytes = new byte[q2 - q1 - 1];
                Array.Copy(line, q1 + 1, e.NameBytes, 0, e.NameBytes.Length);
                e.Name = Petscii.ToText(e.NameBytes);
                string rest = Petscii.ToText(line, q2 + 1, line.Length - q2 - 1).Trim();
                e.Splat = rest.StartsWith("*");
                e.Locked = rest.EndsWith("<");
                e.Type = rest.Trim('*', '<', ' ');
                dir.Entries.Add(e);
            }
            return dir;
        }
    }

    public enum ImageKind { D64, D71, D81 }

    public partial class CbmDrive
    {
        readonly Xum1541 x;
        public int Dev = 8;
        public Action<string> Log = delegate { };
        public Action<int, int> Progress = delegate { };
        public volatile bool Cancel;

        public CbmDrive(Xum1541 xum) { x = xum; }

        void CheckCancel()
        {
            if (Cancel) throw new OperationCanceledException(Lang.T("Operazione annullata"));
        }

        void NotPresent() { throw new DriveException(Lang.T("Il drive ") + Dev + Lang.T(" non risponde (è acceso? numero giusto? cavo IEC collegato?)")); }

        // ---------- canale comandi ----------

        public string ReadStatus() { return ReadStatus(true); }

        public string ReadStatus(bool log)
        {
            if (!x.Talk(Dev, 15)) NotPresent();
            byte[] b = x.RawRead(64);
            x.Untalk();
            string s = Petscii.ToText(b).TrimEnd('\n', '\r', ' ');
            string model = ModelFromStatus(s);
            if (model != null) Model = model;
            if (log) Log("< " + s);
            return s;
        }

        public static int StatusCode(string s)
        {
            int c;
            if (s.Length >= 2 && int.TryParse(s.Substring(0, 2), out c)) return c;
            return 99;
        }

        public void SendCommand(byte[] cmd, bool log)
        {
            if (log) Log("> " + Petscii.ToText(cmd));
            if (!x.Listen(Dev, 15)) NotPresent();
            if (cmd.Length > 0 && x.RawWrite(cmd, 0, cmd.Length) != cmd.Length)
            {
                x.Unlisten();
                throw new DriveException(Lang.T("Invio comando fallito"));
            }
            x.Unlisten();
        }

        public void SendCommand(string cmd) { SendCommand(Petscii.FromText(cmd), true); }

        /// <summary>Invia un comando e restituisce lo stato del drive.</summary>
        public string Command(string cmd)
        {
            SendCommand(cmd);
            return ReadStatus();
        }

        void Expect(string status, string what)
        {
            if (StatusCode(status) >= 20) throw new DriveException(what + ": " + status);
        }

        // ---------- canali dati ----------

        void OpenFile(int sec, byte[] name)
        {
            if (!x.OpenChannel(Dev, sec)) NotPresent();
            if (name.Length > 0) x.RawWrite(name, 0, name.Length);
            x.Unlisten();
        }

        void CloseFile(int sec)
        {
            x.CloseChannel(Dev, sec);
            x.Unlisten();
        }

        byte[] ReadChannel(int sec, int expectedTotal)
        {
            const int CHUNK = 254;
            MemoryStream ms = new MemoryStream();
            if (!x.Talk(Dev, sec)) NotPresent();
            try
            {
                while (true)
                {
                    CheckCancel();
                    byte[] b = x.RawRead(CHUNK);
                    ms.Write(b, 0, b.Length);
                    if (expectedTotal > 0) Progress((int)Math.Min(ms.Length, expectedTotal), expectedTotal);
                    if (b.Length < CHUNK) break;
                    if (x.GetEoi()) break;
                }
            }
            finally { x.Untalk(); }
            return ms.ToArray();
        }

        // ---------- operazioni sui file ----------

        public DiskDirectory ReadDirectory()
        {
            Log(Lang.T("> $ (directory)"));
            OpenFile(0, Petscii.FromText("$"));
            byte[] data;
            try
            {
                string st = ReadStatus(false);
                Expect(st, Lang.T("Lettura directory"));
                data = ReadChannel(0, 0);
            }
            finally { CloseFile(0); }
            DiskDirectory d = DiskDirectory.Parse(data);
            Log(string.Format(Lang.T("Directory: \"{0}\" {1} - {2} file, {3} blocchi liberi"), d.DiskName, d.DiskId, d.Entries.Count, d.BlocksFree));
            return d;
        }

        public byte[] ReadFile(DirEntry e)
        {
            if (e.Type == "REL") throw new DriveException(Lang.T("I file REL non sono supportati"));
            byte[] name = Petscii.Concat(e.NameBytes, Petscii.FromText("," + e.TypeLetter + ",R"));
            Log(Lang.F("> apri \"{0}\" in lettura", e.Name));
            OpenFile(2, name);
            byte[] data;
            try
            {
                Expect(ReadStatus(), Lang.T("Apertura file"));
                data = ReadChannel(2, Math.Max(1, e.Blocks) * 254);
            }
            finally { CloseFile(2); }
            Log(Lang.T("Letti ") + data.Length + Lang.T(" byte"));
            return data;
        }

        public void WriteFile(byte[] nameBytes, char type, byte[] data)
        {
            byte[] name = Petscii.Concat(Petscii.FromText("0:"), nameBytes, Petscii.FromText("," + type + ",W"));
            Log(Lang.T("> scrivi \"") + Petscii.ToText(nameBytes) + "\" (" + data.Length + Lang.T(" byte)"));
            OpenFile(2, name);
            bool closed = false;
            try
            {
                Expect(ReadStatus(), Lang.T("Apertura file in scrittura"));
                if (!x.Listen(Dev, 2)) NotPresent();
                int o = 0;
                while (o < data.Length)
                {
                    CheckCancel();
                    int n = Math.Min(256, data.Length - o);
                    int w = x.RawWrite(data, o, n);
                    if (w != n) { x.Unlisten(); throw new DriveException(Lang.T("Scrittura interrotta dopo ") + (o + Math.Max(w, 0)) + Lang.T(" byte")); }
                    o += n;
                    Progress(o, data.Length);
                }
                x.Unlisten();
                CloseFile(2);
                closed = true;
                Expect(ReadStatus(), Lang.T("Scrittura file"));
            }
            finally { if (!closed) CloseFile(2); }
        }

        public string Scratch(DirEntry e)
        {
            byte[] cmd = Petscii.Concat(Petscii.FromText("S0:"), e.NameBytes);
            SendCommand(cmd, true);
            return ReadStatus();
        }

        public string Rename(DirEntry e, string newName)
        {
            byte[] cmd = Petscii.Concat(Petscii.FromText("R0:" + newName + "="), e.NameBytes);
            SendCommand(cmd, true);
            string st = ReadStatus();
            Expect(st, Lang.T("Rinomina"));
            return st;
        }

        public string Format(string name, string id)
        {
            int old = x.DataTimeoutMs;
            x.DataTimeoutMs = 240000; // la formattazione può durare oltre un minuto
            try
            {
                string cmd = "N0:" + name + (string.IsNullOrEmpty(id) ? "" : "," + id);
                SendCommand(cmd);
                Log(Lang.T("Formattazione in corso, attendi..."));
                string st = ReadStatus();
                Expect(st, Lang.T("Formattazione"));
                return st;
            }
            finally { x.DataTimeoutMs = old; }
        }

        public string Validate()
        {
            int old = x.DataTimeoutMs;
            x.DataTimeoutMs = 120000;
            try { return Command("V0"); }
            finally { x.DataTimeoutMs = old; }
        }

        // ---------- immagini disco ----------

        public static int SectorsPerTrack(ImageKind k, int t)
        {
            if (k == ImageKind.D81) return 40;
            if (k == ImageKind.D71 && t > 35) t -= 35;
            if (t <= 17) return 21;
            if (t <= 24) return 19;
            if (t <= 30) return 18;
            return 17;
        }

        public static int Tracks(ImageKind k)
        {
            switch (k)
            {
                case ImageKind.D71: return 70;
                case ImageKind.D81: return 80;
                default: return 35;
            }
        }

        public static int TotalBlocks(ImageKind k)
        {
            int n = 0;
            for (int t = 1; t <= Tracks(k); t++) n += SectorsPerTrack(k, t);
            return n;
        }

        public static ImageKind? KindFromSize(long size)
        {
            if (size == 174848 || size == 175531) return ImageKind.D64;
            if (size == 349696 || size == 351062) return ImageKind.D71;
            if (size == 819200 || size == 822400) return ImageKind.D81;
            return null;
        }

        public byte[] ReadImage(ImageKind kind)
        {
            int total = TotalBlocks(kind);
            byte[] img = new byte[total * 256];
            byte[] err = new byte[total];
            bool anyError = false;

            if (kind == ImageKind.D71) Command("U0>M1"); // 1571 in modalità doppia faccia
            Log(Lang.T("Lettura disco -> ") + kind + " (" + total + Lang.T(" blocchi)..."));
            OpenFile(2, Petscii.FromText("#"));
            try
            {
                Expect(ReadStatus(), Lang.T("Apertura buffer"));
                int i = 0;
                for (int t = 1; t <= Tracks(kind); t++)
                {
                    int spt = SectorsPerTrack(kind, t);
                    for (int s = 0; s < spt; s++, i++)
                    {
                        CheckCancel();
                        SendCommand(Petscii.FromText("U1 2 0 " + t + " " + s), false);
                        string st = ReadStatus(false);
                        int code = StatusCode(st);
                        if (code == 74) throw new DriveException(Lang.T("Drive non pronto (manca il disco?): ") + st);
                        if (code >= 20)
                        {
                            anyError = true;
                            err[i] = (byte)(code >= 20 && code <= 29 ? code - 18 : 15);
                            Log(string.Format(Lang.T("Errore traccia {0} settore {1}: {2}"), t, s, st));
                        }
                        else
                        {
                            err[i] = 1;
                            if (!x.Talk(Dev, 2)) NotPresent();
                            byte[] b = x.RawRead(256);
                            x.Untalk();
                            Array.Copy(b, 0, img, i * 256, Math.Min(256, b.Length));
                        }
                        Progress(i + 1, total);
                    }
                }
            }
            finally { CloseFile(2); }

            if (!anyError) return img;
            Log(Lang.T("Il disco contiene errori: salvo la tabella errori nell'immagine."));
            return Petscii.Concat(img, err);
        }

        public static void ImageHeader(ImageKind kind, byte[] img, out string name, out string id)
        {
            int off, nameOff, idOff;
            if (kind == ImageKind.D81) { off = BlockOffset(kind, 40, 0); nameOff = 0x04; idOff = 0x16; }
            else { off = BlockOffset(kind, 18, 0); nameOff = 0x90; idOff = 0xA2; }
            name = Petscii.ToText(img, off + nameOff, 16).TrimEnd().Replace(",", "");
            id = Petscii.ToText(img, off + idOff, 2);
        }

        public static int BlockOffset(ImageKind kind, int track, int sector)
        {
            int n = 0;
            for (int t = 1; t < track; t++) n += SectorsPerTrack(kind, t);
            return (n + sector) * 256;
        }

        public void WriteImage(ImageKind kind, byte[] img, bool formatFirst)
        {
            int total = TotalBlocks(kind);
            if (img.Length < total * 256) throw new DriveException(Lang.T("Immagine troppo corta"));
            if (kind == ImageKind.D71) Command("U0>M1");
            if (formatFirst)
            {
                string name, id;
                ImageHeader(kind, img, out name, out id);
                Format(name.ToUpperInvariant(), id);
            }
            Log(Lang.T("Scrittura ") + kind + Lang.T(" su disco (") + total + Lang.T(" blocchi)..."));
            OpenFile(2, Petscii.FromText("#"));
            try
            {
                Expect(ReadStatus(), Lang.T("Apertura buffer"));
                int i = 0;
                for (int t = 1; t <= Tracks(kind); t++)
                {
                    int spt = SectorsPerTrack(kind, t);
                    for (int s = 0; s < spt; s++, i++)
                    {
                        CheckCancel();
                        SendCommand(Petscii.FromText("B-P 2 0"), false);
                        if (!x.Listen(Dev, 2)) NotPresent();
                        int w = x.RawWrite(img, i * 256, 256);
                        x.Unlisten();
                        if (w != 256) throw new DriveException(string.Format(Lang.T("Invio dati fallito (traccia {0} settore {1})"), t, s));
                        SendCommand(Petscii.FromText("U2 2 0 " + t + " " + s), false);
                        string st = ReadStatus(false);
                        if (StatusCode(st) >= 20)
                            throw new DriveException(string.Format(Lang.T("Errore scrittura traccia {0} settore {1}: {2}"), t, s, st));
                        Progress(i + 1, total);
                    }
                }
            }
            finally { CloseFile(2); }
            Command("I0");
            Log(Lang.T("Immagine scritta."));
        }
    }
}
