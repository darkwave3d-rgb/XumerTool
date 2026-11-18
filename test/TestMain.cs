// xumtest: prove da riga di comando con il drive vero (e confronto di immagini, senza drive).
//   xumtest.exe 8 [reset] [dir]            stato del drive, eventualmente reset del bus e directory
//   xumtest.exe 8 slow-read disco.d64       copia lenta (U1, circa 10 minuti): l'immagine di riferimento
//   xumtest.exe 8 fast-read disco.d64       copia veloce S2 (solo lettura, il disco non viene scritto)
//   xumtest.exe compare a.d64 b.d64         confronta due immagini settore per settore
//   xumtest.exe image disco.d64 [cartella]  directory di un'immagine dal PC (con una cartella estrae i file)
// .d71 al posto di .d64 = 1571 doppia faccia. Ctrl+C interrompe in modo pulito.
using System;
using System.Diagnostics;
using System.IO;

namespace XumerTool
{
    static class TestMain
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length >= 3 && args[0] == "compare") return Compare(args[1], args[2]);
            if (args.Length >= 2 && args[0] == "image") return ShowImage(args[1], args.Length >= 3 ? args[2] : null);
            int dev = 8;
            if (args.Length > 0 && (!int.TryParse(args[0], out dev) || dev < 8 || dev > 30))
            {
                Console.WriteLine("Uso:");
                Console.WriteLine("  xumtest.exe 8 [reset] [dir]          stato del drive 8 (reset del bus, directory)");
                Console.WriteLine("  xumtest.exe 8 slow-read disco.d64     copia lenta (U1), immagine di riferimento");
                Console.WriteLine("  xumtest.exe 8 fast-read disco.d64     copia veloce S2 (solo lettura)");
                Console.WriteLine("  xumtest.exe compare a.d64 b.d64       confronta due immagini (senza drive)");
                Console.WriteLine("  xumtest.exe image disco.d64 [cartella] directory di un'immagine (ed estrae i file)");
                Console.WriteLine("Il primo valore e' il numero del drive (8-11 di solito). .d71 = 1571 doppia faccia.");
                return 2;
            }
            try
            {
                using (Xum1541 x = Xum1541.Open())
                {
                    Console.WriteLine("xum1541 firmware v{0}, capacita' 0x{1:X2}, stato 0x{2:X2}", x.Version, x.Capabilities, x.InitStatus);
                    CbmDrive d = new CbmDrive(x);
                    d.Dev = dev;
                    d.Log = s => Console.WriteLine(s);
                    int lastPct = -1;
                    d.Progress = (done, total) =>
                    {
                        int pct = done * 100 / Math.Max(1, total);
                        if (pct / 5 != lastPct / 5) { lastPct = pct; Console.WriteLine("  {0,3}%  ({1}/{2})", pct, done, total); }
                    };
                    Console.CancelKeyPress += (s, e) => { e.Cancel = true; d.Cancel = true; Console.WriteLine("Interruzione richiesta..."); };

                    if (Array.IndexOf(args, "reset") >= 0)
                    {
                        Console.WriteLine("reset bus IEC...");
                        x.BusReset();
                        System.Threading.Thread.Sleep(2500);
                    }
                    d.ReadStatus();
                    if (Array.IndexOf(args, "dir") >= 0)
                    {
                        DiskDirectory dir = d.ReadDirectory();
                        foreach (DirEntry e in dir.Entries)
                            Console.WriteLine("{0,5}  \"{1}\"  {2}", e.Blocks, e.Name, e.Type);
                    }
                    int i = Array.IndexOf(args, "fast-read"), j = Array.IndexOf(args, "slow-read");
                    if (i >= 0 || j >= 0)
                    {
                        bool fast = i >= 0;
                        int k = fast ? i : j;
                        if (k + 1 >= args.Length) { Console.WriteLine("Manca il nome del file immagine."); return 2; }
                        string path = args[k + 1];
                        ImageKind kind = Path.GetExtension(path).ToLowerInvariant() == ".d71" ? ImageKind.D71 : ImageKind.D64;
                        Stopwatch sw = Stopwatch.StartNew();
                        byte[] img = fast ? d.ReadImageFast(kind) : d.ReadImage(kind);
                        sw.Stop();
                        File.WriteAllBytes(path, img);
                        Console.WriteLine("Immagine salvata: {0} ({1} byte) in {2:0.0} s", path, img.Length, sw.Elapsed.TotalSeconds);
                    }
                }
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Interrotto.");
                return 3;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRORE: " + ex.Message);
                return 1;
            }
        }

        /// <summary>Directory di un'immagine dal PC (senza drive); con una cartella estrae anche tutti i file.</summary>
        static int ShowImage(string path, string outDir)
        {
            try
            {
                DiskImage img = DiskImage.Load(path);
                DiskDirectory d = img.ReadDirectory();
                Console.WriteLine("{0}  \"{1}\" {2}{3}", img.Kind, d.DiskName, d.DiskId, img.HasErrorTable ? "  (con tabella errori)" : "");
                foreach (DirEntry e in d.Entries)
                {
                    string info = "";
                    if (outDir != null && e.Type != "CBM")
                    {
                        byte[] data = img.ReadFile(e);
                        Directory.CreateDirectory(outDir);
                        string n = e.Name; foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
                        File.WriteAllBytes(Path.Combine(outDir, n + "." + e.Type.ToLowerInvariant()), data);
                        info = "  -> " + data.Length + " byte";
                    }
                    Console.WriteLine("{0,5}  \"{1}\"{2}{3}{4}{5}", e.Blocks, e.Name, new string(' ', Math.Max(1, 17 - e.Name.Length)), e.Splat ? "*" : " ", e.Type, (e.Locked ? "<" : "") + info);
                }
                Console.WriteLine("{0} BLOCKS FREE.", d.BlocksFree);
                return 0;
            }
            catch (Exception ex) { Console.WriteLine("ERRORE: " + ex.Message); return 1; }
        }

        /// <summary>Confronta due immagini: settori diversi (traccia/settore) e tabelle degli errori.</summary>
        static int Compare(string a, string b)
        {
            byte[] x = File.ReadAllBytes(a), y = File.ReadAllBytes(b);
            ImageKind? kx = CbmDrive.KindFromSize(x.Length), ky = CbmDrive.KindFromSize(y.Length);
            if (kx == null || ky == null || kx != ky)
            {
                Console.WriteLine("Immagini di tipo diverso o non riconosciuto ({0} e {1} byte).", x.Length, y.Length);
                return 2;
            }
            ImageKind kind = kx.Value;
            int total = CbmDrive.TotalBlocks(kind), diff = 0, shown = 0;
            for (int t = 1; t <= CbmDrive.Tracks(kind); t++)
                for (int s = 0; s < CbmDrive.SectorsPerTrack(kind, t); s++)
                {
                    int o = CbmDrive.BlockOffset(kind, t, s);
                    int n = 0;
                    for (int k = 0; k < 256; k++) if (x[o + k] != y[o + k]) n++;
                    if (n == 0) continue;
                    diff++;
                    if (shown++ < 40) Console.WriteLine("  traccia {0,2} settore {1,2}: {2} byte diversi", t, s, n);
                }
            if (shown > 40) Console.WriteLine("  ... e altri {0} settori", shown - 40);
            bool ex = x.Length > total * 256, ey = y.Length > total * 256;
            if (ex || ey)
                Console.WriteLine("Tabella errori: {0} / {1}", ex ? "presente" : "assente", ey ? "presente" : "assente");
            if (ex && ey)
                for (int i = 0; i < total; i++)
                    if (x[total * 256 + i] != y[total * 256 + i])
                        Console.WriteLine("  blocco {0}: codice errore {1} / {2}", i, x[total * 256 + i], y[total * 256 + i]);
            Console.WriteLine(diff == 0 ? "IDENTICHE: tutti i {0} settori coincidono." : "DIVERSE: {1} settori su {0} non coincidono.", total, diff);
            return diff == 0 ? 0 : 1;
        }
    }
}
