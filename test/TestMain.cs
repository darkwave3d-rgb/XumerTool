// xumtest: prove da riga di comando con il drive vero (e confronto di immagini, senza drive).
//   xumtest.exe 8 [reset] [dir]            stato del drive, eventualmente reset del bus e directory
//   xumtest.exe 8 slow-read disco.d64       copia lenta (U1, circa 10 minuti): l'immagine di riferimento
//   xumtest.exe 8 fast-read disco.d64       copia veloce S2 (solo lettura, il disco non viene scritto)
//   xumtest.exe compare a.d64 b.d64         confronta due immagini settore per settore
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
            int dev = args.Length > 0 ? int.Parse(args[0]) : 8;
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
