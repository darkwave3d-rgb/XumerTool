// Collegamento con l'emulatore VICE (https://vice-emu.sourceforge.io/).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace XumerTool
{
    static class Vice
    {
        public static readonly string[] Machines = { Lang.T("C64 (x64sc)"), Lang.T("C128 (x128)") };

        /// <summary>Il percorso di VICE salvato nelle impostazioni, se esiste ancora (immediato, nessuna ricerca).</summary>
        public static string Saved()
        {
            string saved = Settings.Get("vice", "");
            return saved.Length > 0 && File.Exists(saved) ? saved : null;
        }

        /// <summary>
        /// Cerca x64sc.exe nelle cartelle più comuni. Può durare alcuni secondi:
        /// va chiamata da un thread in sottofondo, mai da quello dell'interfaccia.
        /// </summary>
        public static string Scan()
        {
            List<string> roots = new List<string>();
            foreach (Environment.SpecialFolder f in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86,
                                                            Environment.SpecialFolder.Desktop, Environment.SpecialFolder.LocalApplicationData })
            {
                string p = Environment.GetFolderPath(f);
                if (p.Length > 0) roots.Add(p);
            }
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            roots.Add(Path.Combine(profile, "Downloads"));
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"));
            roots.Add(@"C:\");

            foreach (string root in roots)
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (string dir in Directory.GetDirectories(root, "*vice*"))
                    {
                        string found = Search(dir, 4);
                        if (found != null) return found;
                    }
                }
                catch { }
            }
            // anche nel PATH
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string d in path.Split(';'))
            {
                try
                {
                    string exe = Path.Combine(d.Trim(), "x64sc.exe");
                    if (d.Trim().Length > 0 && File.Exists(exe)) return exe;
                }
                catch { }
            }
            return null;
        }

        static string Search(string dir, int depth)
        {
            try
            {
                string exe = Path.Combine(dir, "x64sc.exe");
                if (File.Exists(exe)) return exe;
                if (depth <= 0) return null;
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    string r = Search(sub, depth - 1);
                    if (r != null) return r;
                }
            }
            catch { }
            return null;
        }

        /// <summary>Eseguibile della macchina scelta, nella stessa cartella di x64sc.exe.</summary>
        public static string ExeFor(string x64sc, int machine)
        {
            string dir = Path.GetDirectoryName(x64sc);
            string name = machine == 1 ? "x128.exe" : "x64sc.exe";
            string exe = Path.Combine(dir, name);
            if (!File.Exists(exe)) throw new FileNotFoundException(Lang.T("Non trovo ") + name + Lang.T(" nella cartella di VICE: ") + dir);
            return exe;
        }

        public static string Version(string exe)
        {
            try
            {
                FileVersionInfo v = FileVersionInfo.GetVersionInfo(exe);
                if (!string.IsNullOrEmpty(v.ProductVersion)) return v.ProductVersion;
            }
            catch { }
            return "";
        }

        /// <summary>Avvia VICE caricando ed eseguendo automaticamente un programma o un'immagine disco.</summary>
        public static void Autostart(string exe, string target)
        {
            string ext = Path.GetExtension(target).ToLowerInvariant();
            string args = "";
            if (ext == ".d71") args += "-drive8type 1571 ";
            else if (ext == ".d81") args += "-drive8type 1581 ";
            args += "-autostart \"" + target + "\"";
            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.WorkingDirectory = Path.GetDirectoryName(exe);
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        public static string DefaultFilesDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XUMER TOOL", "VICE");
        }

        /// <summary>Cartella dove finiscono i file letti dal disco e avviati in VICE (scelta dall'utente).</summary>
        public static string FilesDir()
        {
            string d = Settings.Get("vicedir", "");
            if (d.Length == 0) d = DefaultFilesDir();
            Directory.CreateDirectory(d);
            return d;
        }

        /// <summary>Elimina la vecchia cartella temporanea usata dalle versioni precedenti.</summary>
        public static void RemoveOldTempDir()
        {
            try
            {
                string old = Path.Combine(Path.GetTempPath(), "XUM1571Tool"); // nome delle prime versioni
                if (Directory.Exists(old)) Directory.Delete(old, true);
            }
            catch { }
        }
    }
}
