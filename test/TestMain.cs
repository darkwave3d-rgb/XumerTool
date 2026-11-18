using System;

namespace XumerTool
{
    static class TestMain
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int dev = args.Length > 0 ? int.Parse(args[0]) : 8;
            try
            {
                using (Xum1541 x = Xum1541.Open())
                {
                    Console.WriteLine("xum1541 firmware v{0}, capacita' 0x{1:X2}, stato 0x{2:X2}", x.Version, x.Capabilities, x.InitStatus);
                    CbmDrive d = new CbmDrive(x);
                    d.Dev = dev;
                    d.Log = s => Console.WriteLine(s);
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
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRORE: " + ex.Message);
                return 1;
            }
        }
    }
}
