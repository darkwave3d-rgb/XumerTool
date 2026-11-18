// Strumenti esterni: controllo del driver WinUSB, download di Zadig e dei firmware, pagine utili.
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace XumerTool
{
    static class ExternalTools
    {
        public const string ZadigPage = "https://zadig.akeo.ie/";
        public const string VicePage = "https://vice-emu.sourceforge.io/windows.html";
        public const string RetroFixerPage = "https://retrofixer.it/xum1541/";

        /// <summary>True se l'adattatore xum1541 (16D0:0504) ha già il driver WinUSB.</summary>
        public static bool WinUsbInstalled()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_16D0&PID_0504"))
                {
                    if (k == null) return false;
                    foreach (string sub in k.GetSubKeyNames())
                        using (RegistryKey d = k.OpenSubKey(sub))
                        {
                            string service = d == null ? null : d.GetValue("Service") as string;
                            if (service != null && service.Equals("WinUSB", StringComparison.OrdinalIgnoreCase)) return true;
                        }
                }
            }
            catch { }
            return false;
        }

        public static string DownloadsDir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(d);
            return d;
        }

        static WebClient Client()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "XUMERTool");
            return wc;
        }

        /// <summary>Scarica l'ultima versione di Zadig dalla pagina ufficiale delle release (GitHub pbatard/libwdi).</summary>
        public static string DownloadZadig(Action<string> log)
        {
            string url;
            using (WebClient wc = Client())
            {
                log(Lang.T("Cerco l'ultima versione di Zadig…"));
                string json = wc.DownloadString("https://api.github.com/repos/pbatard/libwdi/releases/latest");
                Match m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"(https://[^\"]+/zadig[^\"/]*\\.exe)\"", RegexOptions.IgnoreCase);
                if (!m.Success) throw new Exception(Lang.T("Non trovo Zadig tra le release. Scaricalo da ") + ZadigPage);
                url = m.Groups[1].Value;
            }
            string path = Path.Combine(DownloadsDir(), url.Substring(url.LastIndexOf('/') + 1));
            if (File.Exists(path)) { log(Lang.T("Zadig è già nei Download: ") + path); return path; }
            using (WebClient wc = Client())
            {
                log(Lang.T("Scarico ") + url);
                wc.DownloadFile(url, path + ".tmp");
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".tmp", path);
            log(Lang.T("Zadig salvato in ") + path);
            return path;
        }

        public static void Open(string urlOrPath)
        {
            Process.Start(new ProcessStartInfo(urlOrPath) { UseShellExecute = true });
        }

        public static int FirmwareDownloaded()
        {
            int n = 0;
            foreach (string f in new[] { "xum1541-PROMICRO-v08.hex", "xum1541-PROMICRO_7406-v08.hex" })
                if (File.Exists(Path.Combine(Firmware.Dir(), f))) n++;
            return n;
        }
    }
}
