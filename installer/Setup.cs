// Installer e disinstallatore di XUMER TOOL.
// Installa per l'utente corrente (senza permessi di amministratore) in %LOCALAPPDATA%\Programs\XUMER TOOL,
// crea i collegamenti e registra la voce in "App installate".
//
// Opzioni (per prove automatiche): /quiet /dir:"cartella" /noshortcuts /noregistry /nolaunch /uninstall
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using XumerTool;

namespace XumSetup
{
    static class Product
    {
        public const string Name = "XUMER TOOL";
        public const string Version = "1.2.1b";   // "b" = beta
        public const string Publisher = "darkwave3D";
        public const string Exe = "XumerTool.exe";
        public const string OldExe = "XumTool.exe";   // nome dell'eseguibile fino alla 1.0b del 18/11/2026
        public const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\XUMERTool";
        public static string DefaultDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", Name); } }
        public static string StartMenuLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Name + ".lnk"); } }
        public static string DesktopLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Name + ".lnk"); } }
        public static string SettingsDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XUMERTool"); } }
    }

    class Options
    {
        public bool Quiet, Uninstall, NoShortcuts, NoRegistry, NoLaunch;
        public string Dir;

        public static Options Parse(string[] args)
        {
            Options o = new Options();
            foreach (string a in args)
            {
                string l = a.ToLowerInvariant();
                if (l == "/quiet") o.Quiet = true;
                else if (l == "/uninstall") o.Uninstall = true;
                else if (l == "/noshortcuts") o.NoShortcuts = true;
                else if (l == "/noregistry") o.NoRegistry = true;
                else if (l == "/nolaunch") o.NoLaunch = true;
                else if (l.StartsWith("/dir:")) o.Dir = a.Substring(5).Trim('"');
            }
            string self = Path.GetFileName(Application.ExecutablePath).ToLowerInvariant();
            if (self.StartsWith("disinstalla")) o.Uninstall = true;
            return o;
        }
    }

    static class Installer
    {
        /// <summary>File inclusi nell'installer: nome della risorsa → percorso relativo nella cartella di installazione.</summary>
        static Dictionary<string, string> Payload()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach (string r in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                if (r.StartsWith("payload/")) d[r] = r.Substring("payload/".Length).Replace('/', '\\');
            return d;
        }

        public static bool AppRunning(string dir)
        {
            List<Process> procs = new List<Process>(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Product.Exe)));
            procs.AddRange(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Product.OldExe)));
            foreach (Process p in procs)
            {
                try
                {
                    string f = p.MainModule.FileName;
                    if (dir == null || f.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch { return true; }
            }
            return false;
        }

        public static void Install(Options o, string dir, bool desktop, Action<string, int> progress)
        {
            Directory.CreateDirectory(dir);
            Dictionary<string, string> files = Payload();
            int i = 0;
            foreach (KeyValuePair<string, string> f in files)
            {
                progress(Lang.T("Copio ") + f.Value, 10 + 70 * i++ / Math.Max(1, files.Count));
                string target = Path.Combine(dir, f.Value);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(f.Key))
                using (FileStream fs = File.Create(target))
                    s.CopyTo(fs);
            }
            // il disinstallatore è una copia dell'installer stesso
            string uninstaller = Path.Combine(dir, Uninstaller);
            if (!string.Equals(Path.GetFullPath(Application.ExecutablePath), Path.GetFullPath(uninstaller), StringComparison.OrdinalIgnoreCase))
                File.Copy(Application.ExecutablePath, uninstaller, true);

            // l'eseguibile col vecchio nome non serve più
            try { string old = Path.Combine(dir, Product.OldExe); if (File.Exists(old)) File.Delete(old); } catch { }

            string exe = Path.Combine(dir, Product.Exe);
            if (!o.NoShortcuts)
            {
                progress(Lang.T("Creo i collegamenti"), 85);
                Shortcut(Product.StartMenuLink, exe, dir);
                if (desktop) Shortcut(Product.DesktopLink, exe, dir);
                else if (File.Exists(Product.DesktopLink)) File.Delete(Product.DesktopLink);
                // collegamenti delle prime versioni, col vecchio nome "XUM1571 Tool"
                foreach (Environment.SpecialFolder sf in new[] { Environment.SpecialFolder.Programs, Environment.SpecialFolder.DesktopDirectory })
                    try { string old = Path.Combine(Environment.GetFolderPath(sf), "XUM1571 Tool.lnk"); if (File.Exists(old)) File.Delete(old); } catch { }
            }
            if (!o.NoRegistry)
                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\XUM1571Tool", false); } catch { }
            if (!o.NoRegistry)
            {
                progress(Lang.T("Registro l'applicazione"), 92);
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Product.RegKey))
                {
                    k.SetValue("DisplayName", Product.Name);
                    k.SetValue("DisplayVersion", Product.Version);
                    k.SetValue("Publisher", Product.Publisher);
                    k.SetValue("DisplayIcon", exe);
                    k.SetValue("InstallLocation", dir);
                    k.SetValue("UninstallString", "\"" + uninstaller + "\" /uninstall");
                    k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    k.SetValue("EstimatedSize", (int)(DirSize(dir) / 1024), RegistryValueKind.DWord);
                }
            }
            progress(Lang.T("Installazione completata"), 100);
        }

        static long DirSize(string dir)
        {
            long n = 0;
            foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) n += new FileInfo(f).Length;
            return n;
        }

        /// <summary>Crea un collegamento .lnk con WScript.Shell (presente in tutte le versioni di Windows).</summary>
        static void Shortcut(string link, string target, string workDir)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(t);
            try
            {
                object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { link });
                Type lt = lnk.GetType();
                lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
                lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
                lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
                lt.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { Lang.T("Drive Commodore via USB con l'adattatore xum1541") });
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lnk);
            }
            finally { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
        }

        public static string InstalledDir()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Product.RegKey))
                return k == null ? null : k.GetValue("InstallLocation") as string;
        }

        public static void Uninstall(Options o, string dir, bool removeSettings)
        {
            if (!o.NoShortcuts)
            {
                foreach (string l in new[] { Product.StartMenuLink, Product.DesktopLink })
                    try { if (File.Exists(l)) File.Delete(l); } catch { }
            }
            if (!o.NoRegistry)
                try { Registry.CurrentUser.DeleteSubKeyTree(Product.RegKey, false); } catch { }
            if (removeSettings)
                try { if (Directory.Exists(Product.SettingsDir)) Directory.Delete(Product.SettingsDir, true); } catch { }

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            dir = Path.GetFullPath(dir).TrimEnd('\\');
            // sicurezza: si tocca la cartella solo se contiene davvero XUMER TOOL,
            // e si eliminano solo i file installati (mai il resto della cartella)
            if (!File.Exists(Path.Combine(dir, Product.Exe)) && !File.Exists(Path.Combine(dir, Product.OldExe))) return;

            string self = Path.GetFullPath(Application.ExecutablePath);
            List<string> files = new List<string>(Payload().Values);
            files.Add(Product.Exe); files.Add(Product.OldExe); files.Add(Uninstaller);
            List<string> subdirs = new List<string>();
            foreach (string rel in files)
            {
                string f = Path.GetFullPath(Path.Combine(dir, rel));
                string sub = Path.GetDirectoryName(f);
                if (!string.Equals(sub, dir, StringComparison.OrdinalIgnoreCase) && !subdirs.Contains(sub)) subdirs.Add(sub);
                if (string.Equals(f, self, StringComparison.OrdinalIgnoreCase)) continue;   // eliminato dopo la chiusura
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
            // sottocartelle (es. Licenze) e cartella principale: solo se rimaste vuote
            foreach (string d in subdirs)
                try { if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d, false); } catch { }
            if (string.Equals(Path.GetDirectoryName(self), dir, StringComparison.OrdinalIgnoreCase))
            {
                // il disinstallatore in esecuzione si elimina da sé dopo la chiusura (rmdir senza /s: solo se vuota)
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                    "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + self + "\" & rmdir /q \"" + dir + "\"")
                { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
                Process.Start(psi);
            }
            else
                try { if (Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir, false); } catch { }
        }

        const string Uninstaller = "Disinstalla.exe";
    }

    class SetupForm : Form
    {
        static readonly Color Header = Color.FromArgb(0x1F, 0x29, 0x37), Accent = Color.FromArgb(0x25, 0x63, 0xEB),
                              Muted = Color.FromArgb(0x6B, 0x72, 0x80);
        readonly Options opt;
        readonly TextBox txtDir;
        readonly CheckBox chkDesktop, chkLaunch, chkSettings;
        readonly Button btnGo, btnCancel;
        readonly ProgressBar bar;
        readonly Label lblStep;
        bool done;

        public SetupForm(Options o)
        {
            opt = o;
            Text = (o.Uninstall ? Lang.T("Disinstalla ") : Lang.T("Installa ")) + Product.Name;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;
            ClientSize = new Size(560, o.Uninstall ? 300 : 360);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Panel head = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Header };
            PictureBox logo = new PictureBox { Size = new Size(64, 64), Location = new Point(18, 14), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Header };
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))
                    if (s != null) using (Image img = Image.FromStream(s)) logo.Image = new Bitmap(img);
            }
            catch { }
            head.Controls.Add(logo);
            head.Controls.Add(new Label { Text = Product.Name, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 16f), Location = new Point(96, 16), BackColor = Header });
            head.Controls.Add(new Label
            {
                Text = (o.Uninstall ? Lang.T("Disinstallazione") : Lang.T("Installazione")) + Lang.T(" · versione ") + Product.Version + Lang.T(" · di ") + Product.Publisher,
                AutoSize = true, ForeColor = Color.FromArgb(0x9C, 0xA3, 0xAF), Location = new Point(98, 54), BackColor = Header
            });
            Controls.Add(head);

            int y = 108;
            if (!o.Uninstall)
            {
                Controls.Add(new Label { Text = Lang.T("Cartella di installazione"), AutoSize = true, ForeColor = Muted, Location = new Point(20, y) });
                txtDir = new TextBox { Text = o.Dir ?? Product.DefaultDir, Location = new Point(20, y + 22), Width = 420 };
                Controls.Add(txtDir);
                Button browse = new Button { Text = Lang.T("Sfoglia…"), Location = new Point(448, y + 20), Width = 92, FlatStyle = FlatStyle.System };
                browse.Click += delegate
                {
                    using (FolderBrowserDialog d = new FolderBrowserDialog { SelectedPath = txtDir.Text })
                        if (d.ShowDialog(this) == DialogResult.OK) txtDir.Text = Path.Combine(d.SelectedPath, Product.Name);
                };
                Controls.Add(browse);
                chkDesktop = new CheckBox { Text = Lang.T("Crea un collegamento sul Desktop"), Checked = true, AutoSize = true, Location = new Point(20, y + 62) };
                chkLaunch = new CheckBox { Text = Lang.T("Avvia ") + Product.Name + Lang.T(" al termine"), Checked = true, AutoSize = true, Location = new Point(20, y + 88) };
                Controls.Add(chkDesktop); Controls.Add(chkLaunch);
                Controls.Add(new Label
                {
                    Text = Lang.T("Non servono permessi di amministratore. Il driver WinUSB per l'adattatore si installa a parte con Zadig\n(nell'app: scheda Adattatore → Strumenti necessari)."),
                    AutoSize = true, ForeColor = Muted, Location = new Point(20, y + 118)
                });
            }
            else
            {
                Controls.Add(new Label
                {
                    Text = Lang.T("Verranno rimossi il programma, i collegamenti e la voce in \"App installate\".\nI file che hai salvato (immagini disco, file per VICE) non vengono toccati."),
                    AutoSize = true, Location = new Point(20, y)
                });
                chkSettings = new CheckBox { Text = Lang.T("Elimina anche le impostazioni (tema, carattere, percorsi)"), AutoSize = true, Location = new Point(20, y + 50) };
                Controls.Add(chkSettings);
            }

            lblStep = new Label { AutoSize = true, ForeColor = Muted, Location = new Point(20, ClientSize.Height - 96) };
            bar = new ProgressBar { Location = new Point(20, ClientSize.Height - 72), Width = 520, Height = 10 };
            Controls.Add(lblStep); Controls.Add(bar);

            btnGo = new Button
            {
                Text = o.Uninstall ? Lang.T("Disinstalla") : Lang.T("Installa"), Size = new Size(120, 34), Location = new Point(420, ClientSize.Height - 48),
                FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9.5f)
            };
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.Click += OnGo;
            btnCancel = new Button { Text = Lang.T("Annulla"), Size = new Size(100, 34), Location = new Point(312, ClientSize.Height - 48), FlatStyle = FlatStyle.System };
            btnCancel.Click += delegate { Close(); };
            Controls.Add(btnGo); Controls.Add(btnCancel);
            AcceptButton = btnGo; CancelButton = btnCancel;
        }

        void Step(string text, int pct)
        {
            lblStep.Text = text;
            bar.Value = Math.Max(0, Math.Min(100, pct));
            Application.DoEvents();
        }

        void OnGo(object sender, EventArgs e)
        {
            if (done) { Close(); return; }
            string dir = opt.Uninstall ? (Installer.InstalledDir() ?? Path.GetDirectoryName(Application.ExecutablePath)) : txtDir.Text.Trim();
            while (Installer.AppRunning(dir))
            {
                if (MessageBox.Show(this, Product.Name + Lang.T(" è aperto. Chiudilo e premi Riprova."), Text, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry)
                    return;
            }
            btnGo.Enabled = btnCancel.Enabled = false;
            try
            {
                if (opt.Uninstall)
                {
                    Step(Lang.T("Rimozione in corso…"), 30);
                    Installer.Uninstall(opt, dir, chkSettings.Checked);
                    Step(Product.Name + Lang.T(" è stato disinstallato."), 100);
                }
                else
                {
                    Installer.Install(opt, dir, chkDesktop.Checked, Step);
                    if (chkLaunch.Checked && !opt.NoLaunch)
                        Process.Start(new ProcessStartInfo(Path.Combine(dir, Product.Exe)) { WorkingDirectory = dir, UseShellExecute = true });
                }
                done = true;
                btnGo.Text = Lang.T("Fine");
                btnGo.Enabled = true;
            }
            catch (Exception ex)
            {
                Step(Lang.T("Errore: ") + ex.Message, 0);
                MessageBox.Show(this, Lang.T("Operazione non riuscita:\n") + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnGo.Enabled = btnCancel.Enabled = true;
            }
        }
    }

    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // l'installer segue la lingua di Windows ("lang=en" la forza, per le prove)
            string lang = Lang.SystemDefault();
            foreach (string a in args) if (a.StartsWith("lang=")) lang = a.Substring(5);
            Lang.Load(lang);
            Options o = Options.Parse(args);
            if (o.Quiet)
            {
                // modalità senza finestra, per prove automatiche
                try
                {
                    if (o.Uninstall) Installer.Uninstall(o, o.Dir ?? Installer.InstalledDir(), false);
                    else Installer.Install(o, o.Dir ?? Product.DefaultDir, false, (s, p) => Console.WriteLine(p + "% " + s));
                    return 0;
                }
                catch (Exception ex) { Console.WriteLine(Lang.T("ERRORE: ") + ex.Message); return 1; }
            }
            Application.EnableVisualStyles();
            Application.Run(new SetupForm(o));
            return 0;
        }
    }
}
