using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace XumerTool
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Theme.Scale = g.DpiX / 96f;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // lingua: quella salvata, altrimenti quella di Windows; "lang=en" la forza (modalità screenshot)
            string lang = Settings.Get("lang", Lang.SystemDefault());
            foreach (string a in args) if (a.StartsWith("lang=")) lang = a.Substring(5);
            Lang.Load(lang);
            Start(args);
        }

        // Tutto il resto in un metodo a parte: le tabelle statiche con i testi (comandi DOS, caratteri, crediti)
        // vengono così create solo dopo che la lingua è stata caricata.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void Start(string[] args)
        {
            if (args.Length >= 2 && (args[0] == "--shot-game" || args[0] == "--shot-saver" || args[0] == "--shot-assault"))
            {
                Theme.Apply(args.Length >= 3 ? args[2] : "Chiaro");
                Form f;
                if (args[0] == "--shot-game") { GameForm gf = new GameForm(); gf.PreviewState(); f = gf; }
                else if (args[0] == "--shot-assault") { AssaultForm af = new AssaultForm(); af.PreviewState(); f = af; }
                else f = new ScreenSaverForm(new Rectangle(-20000, -20000, 1280, 720));
                f.ShowInTaskbar = false;
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-20000, -20000);
                f.Show();
                if (f is ScreenSaverForm) { f.Location = new Point(-20000, -20000); f.Size = new Size(1280, 720); Application.DoEvents(); ((ScreenSaverForm)f).SeekForPreview(4.0f); }
                for (int i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(60); }
                using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
                    bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                }
                f.Dispose();
                return;
            }
            if (args.Length >= 2 && args[0] == "--shot-themes")
            {
                // la tendina dei temi aperta, da sola: --shot-themes file.png [tema]
                Theme.Apply(args.Length >= 3 ? args[2] : "Chiaro");
                PreferencesDialog.ShotThemeList(args[1]);
                return;
            }
            if (args.Length >= 2 && (args[0] == "--shot-about" || args[0] == "--shot-prefs"))
            {
                Theme.Apply(args.Length >= 3 ? args[2] : "Chiaro");
                Form a = args[0] == "--shot-prefs" ? (Form)new PreferencesDialog(false) : new AboutDialog();
                a.ShowInTaskbar = false; a.StartPosition = FormStartPosition.Manual; a.Location = new Point(-20000, -20000);
                a.Show();
                for (int i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(60); }
                using (Bitmap bmp = new Bitmap(a.Width, a.Height))
                {
                    a.DrawToBitmap(bmp, new Rectangle(Point.Empty, a.Size));
                    bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                }
                a.Close();
                return;
            }
            if (args.Length >= 2 && args[0] == "--shot")
            {
                // anteprima dell'interfaccia salvata in PNG (per sviluppo): --shot file.png [tema] [scheda]
                Theme.Apply(args.Length >= 3 ? args[2] : "Chiaro");
                MainForm f = new MainForm(false);
                f.ShowInTaskbar = false;
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-20000, -20000);
                f.Show();
                foreach (string a in args)   // w=NNN: larghezza della finestra (per provare l'adattamento)
                    if (a.StartsWith("w=")) f.Width = Math.Max(f.MinimumSize.Width, int.Parse(a.Substring(2)));
                f.Demo(args.Length >= 4 ? int.Parse(args[3]) : 1);
                if (args.Length >= 5 && args[4] == "saver") { Application.DoEvents(); f.DemoPanelSaver(4.0f); }
                for (int i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(60); }
                using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
                    bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                }
                f.Close();
                return;
            }
            Theme.Apply(Settings.Get("theme", "Chiaro"));
            Application.Run(new MainForm(true));
        }
    }

    class InputDialog : Form
    {
        readonly TextBox[] boxes;

        InputDialog(string title, string[] labels, string[] values, int[] maxLen)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(Theme.S(14));
            TableLayoutPanel t = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
            boxes = new TextBox[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                t.Controls.Add(new Label { Text = labels[i], AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(3, i == 0 ? 0 : Theme.S(8), 3, 2) });
                boxes[i] = new TextBox
                {
                    Text = values[i], Width = Theme.S(300), MaxLength = maxLen[i], CharacterCasing = CharacterCasing.Upper,
                    Font = new Font("Consolas", 11f), BackColor = Theme.FieldBack, ForeColor = Theme.FieldText, BorderStyle = BorderStyle.FixedSingle
                };
                t.Controls.Add(boxes[i]);
            }
            FlowLayoutPanel b = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Theme.S(12), 0, 0) };
            IconButton ok = new IconButton(null, "OK", ButtonKind.Accent) { DialogResult = DialogResult.OK };
            IconButton cancel = new IconButton(null, Lang.T("Annulla"), ButtonKind.Toolbar) { DialogResult = DialogResult.Cancel };
            b.Controls.Add(ok); b.Controls.Add(cancel);
            t.Controls.Add(b);
            Controls.Add(t);
            AcceptButton = ok; CancelButton = cancel;
        }

        public static string[] Ask(IWin32Window owner, string title, string[] labels, string[] values, int[] maxLen)
        {
            using (InputDialog d = new InputDialog(title, labels, values, maxLen))
            {
                if (d.ShowDialog(owner) != DialogResult.OK) return null;
                string[] r = new string[d.boxes.Length];
                for (int i = 0; i < r.Length; i++) r[i] = d.boxes[i].Text;
                return r;
            }
        }
    }

    class DosCommand
    {
        public string Cmd, Desc;
        public DosCommand(string cmd, string desc) { Cmd = cmd; Desc = desc; }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        const int EM_SETCUEBANNER = 0x1501;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);

        /// <summary>Barre di scorrimento scure (Windows 10/11) per i controlli su sfondo scuro.</summary>
        static void DarkScrollbars(Control c)
        {
            EventHandler apply = delegate { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
            if (c.IsHandleCreated) apply(c, EventArgs.Empty); else c.HandleCreated += apply;
        }

        /// <summary>Menu a tendina disegnato nei colori del tema.</summary>
        static ComboBox ThemedCombo(int width)
        {
            ComboBox cb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Width = width, FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed, BackColor = Theme.InputBack, ForeColor = Theme.InputText
            };
            cb.ItemHeight = (int)(cb.Font.Height * 1.25f);
            cb.DrawItem += (s, e) =>
            {
                bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
                using (SolidBrush b = new SolidBrush(sel ? Theme.Blend(Theme.InputBack, Theme.Accent, 0.45f) : Theme.InputBack))
                    e.Graphics.FillRectangle(b, e.Bounds);
                if (e.Index >= 0)
                    TextRenderer.DrawText(e.Graphics, cb.Items[e.Index].ToString(), cb.Font,
                        new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height), Theme.InputText,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            return cb;
        }

        const string AppName = "XUMER TOOL";

        // glifi Segoe Fluent Icons / MDL2 Assets
        const string G_CONNECT = "", G_POWER = "", G_REFRESH = "", G_CHECK = "",
                     G_ERASE = "", G_SAVE = "", G_UPLOAD = "", G_DOWNLOAD = "",
                     G_RENAME = "", G_DELETE = "", G_CANCEL = "", G_SEND = "",
                     G_INFO = "", G_CLEAR = "", G_PALETTE = "", G_PLAY = "",
                     G_FOLDER = "", G_SEARCH = "", G_DISK = "";

        static readonly string G_OPEN = char.ConvertFromUtf32(0xE838);
        static readonly string G_GAME = char.ConvertFromUtf32(0xE7FC);
        static readonly string G_USB = char.ConvertFromUtf32(0xE88E);
        static readonly string G_LIST = char.ConvertFromUtf32(0xE8FD);     // elenco puntato
        static readonly string G_PROMPT = char.ConvertFromUtf32(0xE756);   // prompt dei comandi
        static readonly string G_MONITOR = char.ConvertFromUtf32(0xE7F4);  // monitor
        static readonly string G_CHIP = char.ConvertFromUtf32(0xE950);
        bool interactiveMode;

        ComboBox cmbFirmware;
        Label lblFirmwareFile;
        string customHex;

        /// <summary>Scheda Adattatore: collegamento automatico e scrittura del firmware sul Pro Micro.</summary>
        Panel BuildAdapterPage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack, Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), Theme.S(6)), AutoScroll = true };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, BackColor = Theme.LogBack };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            CheckBox auto = new CheckBox
            {
                Text = Lang.T("Collega automaticamente l'adattatore all'avvio"), Checked = AutoConnect, AutoSize = true,
                ForeColor = Theme.LogText, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, Theme.S(2), 0, Theme.S(8)), Cursor = Cursors.Hand
            };
            auto.FlatAppearance.BorderColor = Theme.TabAdapter;
            auto.FlatAppearance.CheckedBackColor = Theme.Blend(Theme.LogBack, Theme.TabAdapter, 0.35f);
            auto.CheckedChanged += delegate { Settings.Set("autoconnect", auto.Checked ? "1" : "0"); };
            t.Controls.Add(auto, 0, 0);
            t.SetColumnSpan(auto, 3);

            t.Controls.Add(new Label { Text = Lang.T("FIRMWARE DEL PRO MICRO"), AutoSize = true, ForeColor = Theme.TabAdapter, Font = new Font("Segoe UI", 8f, FontStyle.Bold), Margin = new Padding(0, Theme.S(2), 0, Theme.S(4)) }, 0, 1);
            t.SetColumnSpan(t.GetControlFromPosition(0, 1), 3);

            t.Controls.Add(new Label { Text = Lang.T("Firmware"), AutoSize = true, ForeColor = Theme.HeaderMuted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(10), 0) }, 0, 2);
            cmbFirmware = ThemedCombo(Theme.S(300));
            cmbFirmware.Margin = new Padding(0, Theme.S(4), Theme.S(10), Theme.S(4));
            cmbFirmware.Items.AddRange(Firmware.Choices);
            cmbFirmware.SelectedIndex = 0;
            cmbFirmware.SelectionChangeCommitted += delegate
            {
                if (cmbFirmware.SelectedIndex == 2) ChooseHex();
                UpdateFirmwareLabel();
            };
            t.Controls.Add(cmbFirmware, 1, 2);
            IconButton install = new IconButton(G_CHIP, Lang.T("Installa firmware"), ButtonKind.Accent) { Margin = new Padding(0, Theme.S(2), 0, Theme.S(2)), Anchor = AnchorStyles.Left };
            install.Click += OnInstallFirmware;
            t.Controls.Add(install, 2, 2);

            lblFirmwareFile = new Label { AutoSize = true, ForeColor = Theme.HeaderMuted, Font = new Font("Consolas", 9f), Margin = new Padding(0, Theme.S(2), 0, 0) };
            t.Controls.Add(lblFirmwareFile, 1, 3);
            t.SetColumnSpan(lblFirmwareFile, 2);

            Label hint = new Label
            {
                AutoSize = true, MaximumSize = new Size(Theme.S(1000), 0), ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(8), 0, 0),
                Text = Lang.T("Prima scollega il cavo IEC dal drive. Con lo xum1541 l'adattatore entra da solo nel bootloader; con altri firmware (es. RFIEC) ") +
                       Lang.T("premi due volte il tasto reset del Pro Micro quando richiesto. Il firmware viene scritto e verificato; il bootloader non viene mai toccato.")
            };
            t.Controls.Add(hint, 0, 4);
            t.SetColumnSpan(hint, 3);
            hint.MaximumSize = new Size(Theme.S(560), 0);
            t.Dock = DockStyle.None; t.AutoSize = true; t.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            t.Margin = new Padding(0, 0, Theme.S(24), 0);

            // a destra: gli strumenti necessari
            TableLayoutPanel outer = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = Theme.LogBack };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            outer.Controls.Add(t, 0, 0);
            outer.Controls.Add(BuildToolsPanel(), 1, 0);
            page.Controls.Add(outer);
            UpdateFirmwareLabel();
            return page;
        }

        Label lblDriverState, lblFwState, lblViceTool;

        Control BuildToolsPanel()
        {
            TableLayoutPanel t = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, BackColor = Theme.LogBack, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label head = new Label { Text = Lang.T("STRUMENTI NECESSARI"), AutoSize = true, ForeColor = Theme.TabAdapter, Font = new Font("Segoe UI", 8f, FontStyle.Bold), Margin = new Padding(0, Theme.S(2), 0, Theme.S(4)) };
            t.Controls.Add(head, 0, 0);
            IconButton all = new IconButton(G_DOWNLOAD, Lang.T("Scarica tutto"), ButtonKind.Accent) { Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, 0, Theme.S(4)) };
            all.Click += OnDownloadAll;
            t.Controls.Add(all, 1, 0);

            int row = 1;
            Func<string, Label> addTool = name =>
            {
                FlowLayoutPanel box = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, Theme.S(4), Theme.S(12), Theme.S(2)), BackColor = Theme.LogBack };
                box.Controls.Add(new Label { Text = name, AutoSize = true, ForeColor = Theme.LogText, Font = new Font("Segoe UI Semibold", 9.5f), Margin = new Padding(0) });
                Label st = new Label { AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0), MaximumSize = new Size(Theme.S(280), 0) };
                box.Controls.Add(st);
                t.Controls.Add(box, 0, row);
                return st;
            };
            Action<IconButton[]> addButtons = buttons =>
            {
                FlowLayoutPanel f = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right, Margin = new Padding(0, Theme.S(2), 0, 0), BackColor = Theme.LogBack };
                foreach (IconButton b in buttons) f.Controls.Add(b);
                t.Controls.Add(f, 1, row);
                row++;
            };
            Func<string, string, EventHandler, IconButton> btn = (glyph, text, click) =>
            {
                IconButton b = new IconButton(glyph, text, ButtonKind.Header);
                b.Click += click;
                return b;
            };

            lblDriverState = addTool(Lang.T("Driver WinUSB (Zadig)"));
            addButtons(new[] {
                btn(G_DOWNLOAD, Lang.T("Scarica Zadig"), delegate { Run(Lang.T("Zadig"), delegate { string p = ExternalTools.DownloadZadig(Log); Ui(delegate { OfferRunZadig(p); }); }); }),
                btn(G_INFO, Lang.T("Sito"), delegate { OpenLink(ExternalTools.ZadigPage); }) });
            lblFwState = addTool(Lang.T("Firmware xum1541 (.hex)"));
            addButtons(new[] {
                btn(G_DOWNLOAD, Lang.T("Scarica"), delegate { Run(Lang.T("Firmware"), delegate { Firmware.Get(0, Log); Firmware.Get(1, Log); Ui(UpdateToolStates); }); }),
                btn(G_OPEN, Lang.T("Cartella"), delegate { OpenLink(Firmware.Dir()); }) });
            lblViceTool = addTool(Lang.T("Emulatore VICE"));
            addButtons(new[] { btn(G_INFO, Lang.T("Sito di VICE"), delegate { OpenLink(ExternalTools.VicePage); }) });
            Label rf = addTool(Lang.T("Firmware RFIEC (RetroFixer)"));
            rf.Text = Lang.T("si scarica dalla pagina di RetroFixer (pulsante \"Scarica .hex\")");
            addButtons(new[] { btn(G_INFO, Lang.T("Pagina RetroFixer"), delegate { OpenLink(ExternalTools.RetroFixerPage); }) });
            UpdateToolStates();
            return t;
        }

        void UpdateToolStates()
        {
            if (lblDriverState == null) return;
            bool drv = ExternalTools.WinUsbInstalled();
            lblDriverState.Text = drv ? Lang.T("✓ installato per l'adattatore 16D0:0504") : Lang.T("non installato: apri Zadig, scegli \"xum1541\" e installa WinUSB");
            lblDriverState.ForeColor = drv ? Theme.LogReply : Theme.LogError;
            int fw = ExternalTools.FirmwareDownloaded();
            lblFwState.Text = fw == 2 ? Lang.T("✓ entrambe le versioni scaricate") : fw == 1 ? Lang.T("una versione scaricata") : Lang.T("non ancora scaricati");
            lblFwState.ForeColor = fw == 2 ? Theme.LogReply : Theme.HeaderMuted;
            string vice = Vice.Find();
            lblViceTool.Text = vice != null ? Lang.T("✓ trovato") : Lang.T("non trovato sul PC");
            lblViceTool.ForeColor = vice != null ? Theme.LogReply : Theme.HeaderMuted;
        }

        void OpenLink(string target)
        {
            try { ExternalTools.Open(target); }
            catch (Exception ex) { Log(Lang.T("ERRORE: non riesco ad aprire ") + target + ": " + ex.Message); }
        }

        void OfferRunZadig(string path)
        {
            UpdateToolStates();
            if (MessageBox.Show(this, Lang.T("Zadig è stato scaricato in:\n") + path + Lang.T("\n\nAvviarlo ora? Windows chiederà il permesso di amministratore.\n\n") +
                Lang.T("In Zadig: Options → List All Devices, scegli \"xum1541\", seleziona WinUSB e premi Install Driver."),
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                OpenLink(path);
        }

        void OnDownloadAll(object sender, EventArgs e)
        {
            Run(Lang.T("Download strumenti"), delegate
            {
                Firmware.Get(0, Log);
                Firmware.Get(1, Log);
                string z = ExternalTools.WinUsbInstalled() ? null : ExternalTools.DownloadZadig(Log);
                if (z == null) Log(Lang.T("Il driver WinUSB è già installato: Zadig non serve."));
                Log(Lang.T("Download completati. VICE e RFIEC si scaricano dai loro siti (pulsanti nella scheda Adattatore)."));
                Ui(delegate { if (z != null) OfferRunZadig(z); else UpdateToolStates(); });
            });
        }

        void ChooseHex()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = Lang.T("Firmware Intel HEX (*.hex)|*.hex");
                d.Title = Lang.T("Scegli il firmware da scrivere sul Pro Micro");
                if (d.ShowDialog(this) == DialogResult.OK) customHex = d.FileName;
                else if (customHex == null) cmbFirmware.SelectedIndex = 0;
            }
        }

        void UpdateFirmwareLabel()
        {
            if (lblFirmwareFile == null) return;
            lblFirmwareFile.Text = cmbFirmware.SelectedIndex == 2
                ? (customHex ?? Lang.T("nessun file scelto"))
                : Lang.T("verrà scaricato da github.com/OpenCBM la prima volta");
        }

        void OnInstallFirmware(object sender, EventArgs e)
        {
            if (Busy) return;
            int choice = cmbFirmware.SelectedIndex;
            if (choice == 2 && customHex == null) { ChooseHex(); UpdateFirmwareLabel(); if (customHex == null) return; }
            string label = choice == 2 ? Path.GetFileName(customHex) : Firmware.Choices[choice];
            if (MessageBox.Show(this, Lang.F("Scrivere il firmware \"{0}\" sul Pro Micro?\n\nScollega prima il cavo IEC dal drive. L'operazione dura circa 20 secondi.", label),
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            bool wasConnected = xum != null;
            if (wasConnected) { drive = null; }
            Xum1541 current = xum;
            xum = null;
            SelectTab(TAB_LOG);
            if (drive == null) SetState("FIRMWARE", Lang.T("scrittura del firmware in corso"));
            Run(Lang.T("Firmware"), delegate
            {
                string hex;
                byte[] img;
                int len;
                try
                {
                    hex = choice == 2 ? customHex : Firmware.Get(choice, Log);
                    img = IntelHex.Load(hex, Avr109.AppFlashSize, out len);
                }
                catch
                {
                    // il firmware non è utilizzabile: rilascia l'adattatore, così si può ricollegare
                    if (current != null) current.Dispose();
                    SetState("OFFLINE", Lang.T("firmware non scritto: premi \"Collega\""));
                    throw;
                }
                Log(string.Format(Lang.T("Firmware: {0} ({1} byte)"), Path.GetFileName(hex), len));

                string[] before = System.IO.Ports.SerialPort.GetPortNames();
                bool auto = false;
                try
                {
                    Xum1541 x = current ?? Xum1541.Open();
                    Log(Lang.T("Riavvio dell'adattatore nel bootloader…"));
                    x.EnterBootloader();
                    auto = true;
                }
                catch (Exception)
                {
                    Log(Lang.T(">>> Premi ORA due volte, rapidamente, il tasto reset del Pro Micro (o tocca due volte RST con GND)."));
                }
                string port = Avr109.WaitForNewPort(before, auto ? 10000 : 30000, () => false);
                if (port == null)
                    throw new DriveException(auto ? Lang.T("Il bootloader non è comparso. Premi due volte il tasto reset del Pro Micro e riprova.")
                                                  : Lang.T("Il bootloader non è comparso entro 30 secondi. Riprova premendo due volte il tasto reset."));
                Log(Lang.T("Bootloader trovato su ") + port + Lang.T(": scrittura in corso…"));
                using (Avr109 p = new Avr109(port))
                    p.Program(img, len, Log, SetProgress);
                Log(Lang.T("Firmware scritto e verificato. Il Pro Micro si riavvia."));
                SetState("OFFLINE", Lang.T("firmware aggiornato: premi \"Collega\""));

                if (choice != 2 || hex.ToLowerInvariant().Contains("xum1541"))
                {
                    Thread.Sleep(3500);
                    Log(Lang.T("Ricollego l'adattatore…"));
                    try { ConnectJob(8 + Ui2(() => cmbDev.SelectedIndex))(); }
                    catch (Exception ex) { Log(Lang.T("Ricollegamento non riuscito: ") + ex.Message + Lang.T(" Premi \"Collega\".")); }
                }
            });
        }

        /// <summary>Legge un valore dal thread dell'interfaccia.</summary>
        T Ui2<T>(Func<T> f)
        {
            return InvokeRequired ? (T)Invoke(f) : f();
        }

        Label lblRunnerHi, lblAssaultHi;

        /// <summary>Scheda Giochi: si gioca in una finestra a parte, anche mentre l'app lavora.</summary>
        Panel BuildGamesPage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack, Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(6)), AutoScroll = true };
            FlowLayoutPanel row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Theme.LogBack };
            row.Controls.Add(GameCard(Lang.T("Floppy Runner"), Lang.T("Fai saltare il floppy oltre calamite e torri di blocchi PETSCII. Più corri, più va veloce."),
                Lang.T("Spazio / ↑ salta · ↓ scende · Esc esce"), out lblRunnerHi, delegate { OpenGame(new GameForm()); }));
            row.Controls.Add(GameCard(Lang.T("Assalto PETSCII"), Lang.T("Difendi il drive dagli alieni a blocchi. Colpisci il floppy rosso per un bonus."),
                Lang.T("← → muovi · Spazio spara · Esc esce"), out lblAssaultHi, delegate { OpenGame(new AssaultForm()); }));
            page.Controls.Add(row);
            page.Controls.Add(new Label
            {
                Dock = DockStyle.Top, AutoSize = false, Height = Theme.S(26), ForeColor = Theme.HeaderMuted, TextAlign = ContentAlignment.MiddleLeft,
                Text = Lang.T("I giochi si aprono in una finestra a parte: puoi giocare mentre l'app crea un'immagine disco.")
            });
            RefreshRecords();
            return page;
        }

        Control GameCard(string title, string desc, string keys, out Label record, EventHandler play)
        {
            Color c = Theme.TabGames;
            TableLayoutPanel card = new TableLayoutPanel
            {
                ColumnCount = 2, RowCount = 3, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(Theme.S(430), 0), MaximumSize = new Size(Theme.S(430), 0),
                BackColor = Theme.Blend(Theme.LogBack, c, 0.12f), Margin = new Padding(0, 0, Theme.S(12), Theme.S(8)),
                Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(12), Theme.S(10))
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            card.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = c, Font = new Font("Segoe UI Semibold", 11f), Margin = new Padding(0) }, 0, 0);
            card.Controls.Add(new Label { Text = desc, AutoSize = true, MaximumSize = new Size(Theme.S(280), 0), ForeColor = Theme.LogText, Margin = new Padding(0, Theme.S(2), 0, 0) }, 0, 1);
            record = new Label { AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(4), 0, 0), Font = new Font("Consolas", 9f) };
            card.Controls.Add(new Label { Text = keys, AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(4), 0, 0) }, 0, 2);
            IconButton btn = new IconButton(G_GAME, Lang.T("Gioca"), ButtonKind.Header) { Anchor = AnchorStyles.Right | AnchorStyles.Top };
            btn.Click += play;
            card.Controls.Add(btn, 1, 0);
            card.SetRowSpan(btn, 2);
            card.Controls.Add(record, 1, 2);
            return card;
        }

        void RefreshRecords()
        {
            if (lblRunnerHi != null) lblRunnerHi.Text = Lang.T("RECORD ") + Settings.Get("gamehi", "0").PadLeft(5, '0');
            if (lblAssaultHi != null) lblAssaultHi.Text = Lang.T("RECORD ") + Settings.Get("assaulthi", "0").PadLeft(5, '0');
        }

        void OpenGame(Form game)
        {
            game.FormClosed += delegate { RefreshRecords(); game.Dispose(); };
            game.Show(this);
        }

        static readonly DosCommand[] DosCommands =
        {
            new DosCommand("I0", Lang.T("Inizializza il disco (rilegge la BAM)")),
            new DosCommand("V0", Lang.T("Valida: ricostruisce la BAM e libera i blocchi persi")),
            new DosCommand(Lang.T("S0:NOME"), Lang.T("Cancella un file (ammessi * e ?)")),
            new DosCommand(Lang.T("R0:NUOVO=VECCHIO"), Lang.T("Rinomina un file")),
            new DosCommand(Lang.T("C0:COPIA=ORIGINALE"), Lang.T("Copia un file sullo stesso disco")),
            new DosCommand(Lang.T("C0:NUOVO=FILE1,FILE2"), Lang.T("Unisce due file in uno nuovo")),
            new DosCommand(Lang.T("N0:NOME,ID"), Lang.T("Formattazione completa con nome e ID")),
            new DosCommand(Lang.T("N0:NOME"), Lang.T("Formattazione veloce: svuota la directory")),
            new DosCommand("UJ", Lang.T("Reset del drive")),
            new DosCommand("U0>M1", Lang.T("1571: modalità doppia faccia")),
            new DosCommand("U0>M0", Lang.T("1571: modalità 1541 (una faccia)")),
            new DosCommand("U0>H0", Lang.T("1571: usa la faccia 0")),
            new DosCommand("U0>H1", Lang.T("1571: usa la faccia 1")),
        };

        Xum1541 xum;
        CbmDrive drive;
        Thread worker;
        bool demo;

        ComboBox cmbMachine;
        Segmented cmbDev;
        IconButton btnConnect, btnReset, btnDir, btnSave, btnWrite, btnRename, btnScratch, btnFormat, btnValidate,
                   btnDiskToImg, btnImgToDisk, btnCancel, btnSend, btnStatus, btnClear, btnTheme,
                   btnViceFile, btnViceImage, btnViceDisk;
        TextBox txtCmd, txtVice, txtViceDir;
        RichTextBox txtLog;
        Led led;
        Label lblState, lblStateSub, lblDiskNo, lblDiskHeader, lblFree, lblReady, lblCursor, lblViceState, lblProgress;
        Panel c64Frame;
        ListBox lst, lstDos;
        SlimProgress progress;
        SplitContainer split;
        TabButton[] tabs;
        Panel tabLine;
        TableLayoutPanel cmdBar;
        Panel[] tabPages;
        int currentTab = TAB_LOG;
        const int TAB_ADAPTER = 0, TAB_LOG = 1, TAB_DOS = 2, TAB_VICE = 3, TAB_GAMES = 4;
        DiskDirectory currentDir;
        readonly List<Button> driveButtons = new List<Button>();
        readonly List<KeyValuePair<string, string>> logEntries = new List<KeyValuePair<string, string>>();
        string stateText = "OFFLINE", stateSub = Lang.T("adattatore non collegato");
        System.Windows.Forms.Timer blink;
        // barre che si adattano alla larghezza della finestra
        TableLayoutPanel headerBar, tabBar;
        FlowLayoutPanel toolBar, stateTextBox;
        Label lblSubtitle, lblDevCaption;
        IconButton btnInfo;
        readonly List<IconButton> toolButtons = new List<IconButton>();
        int adaptedWidth = -1, headerLevel;
        bool adapting;

        public MainForm() : this(true) { }

        public MainForm(bool interactive)
        {
            Text = AppName;
            Icon = Theme.MakeAppIcon();
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Theme.S(1200), Theme.S(760));
            MinimumSize = new Size(Theme.S(820), Theme.S(580));
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();
            ShowEmptyScreen();
            UpdateButtons();
            Log(Lang.T("Pronto. Collega l'adattatore, accendi il drive e premi \"Collega\"."));
            Log(Lang.T("Nota: chiudi la pagina web del programma se è collegata all'adattatore."));

            interactiveMode = interactive;
            if (interactive) { Vice.RemoveOldTempDir(); StartIdleWatch(); }

            blink = new System.Windows.Forms.Timer { Interval = 530 };
            blink.Tick += delegate { if (lblCursor != null) lblCursor.ForeColor = lblCursor.ForeColor == Theme.C64Back ? Theme.C64Text : Theme.C64Back; };
            blink.Start();
        }

        // ================= costruzione interfaccia =================

        IconButton ToolButton(Control parent, string glyph, string text, string shortText, EventHandler click, bool needsDrive)
        {
            IconButton b = new IconButton(glyph, text, ButtonKind.Toolbar) { LongText = text, ShortText = shortText };
            toolButtons.Add(b);
            if (parent.Tag is Color)
            {
                Color c = (Color)parent.Tag;
                b.IconColor = c;
                b.HoverColor = Theme.Blend(c, Theme.Surface, 0.75f);
                b.PressedColor = Theme.Blend(c, Theme.Surface, 0.62f);
            }
            b.Click += click;
            parent.Controls.Add(b);
            if (needsDrive) driveButtons.Add(b);
            return b;
        }

        /// <summary>Sezione della barra strumenti: fascia del titolo nel colore vivo, sfondo tenue dello stesso colore.</summary>
        FlowLayoutPanel Group(FlowLayoutPanel toolbar, string caption, Color color)
        {
            Color back = Theme.Blend(color, Theme.Surface, 0.90f);
            TableLayoutPanel g = new TableLayoutPanel
            {
                ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = back,
                Margin = new Padding(0, 0, Theme.S(10), 0), Padding = new Padding(0, 0, 0, Theme.S(4))
            };
            g.Controls.Add(new Label
            {
                Text = caption.ToUpperInvariant(), AutoSize = true, Dock = DockStyle.Fill, BackColor = color, ForeColor = Theme.SectionCaptionText,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), Margin = new Padding(0), Padding = new Padding(Theme.S(10), Theme.S(3), Theme.S(10), Theme.S(3))
            }, 0, 0);
            FlowLayoutPanel row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(Theme.S(4), Theme.S(4), Theme.S(4), 0), BackColor = back, Tag = color };
            g.Controls.Add(row, 0, 1);
            toolbar.Controls.Add(g);
            return row;
        }

        Label C64Label(string text, bool reverse)
        {
            return new Label
            {
                Text = text, AutoSize = true, Margin = new Padding(0), Padding = new Padding(0, Theme.S(3), 0, Theme.S(3)),
                Font = lst.Font, UseMnemonic = false, UseCompatibleTextRendering = true, // GDI+: varianti dei caratteri variabili
                BackColor = reverse ? Theme.C64Text : Theme.C64Back,
                ForeColor = reverse ? Theme.C64Back : Theme.C64Text
            };
        }

        void BuildUi()
        {
            BackColor = Theme.Window;

            // ---------- intestazione scura ----------
            TableLayoutPanel header = headerBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.Header, ColumnCount = 9, RowCount = 1,
                Padding = new Padding(Theme.S(16), Theme.S(10), Theme.S(16), Theme.S(10))
            };
            for (int i = 0; i < 9; i++) header.ColumnStyles.Add(new ColumnStyle(i == 5 ? SizeType.Percent : SizeType.AutoSize, 100));

            FlowLayoutPanel title = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, Theme.S(28), 0), Anchor = AnchorStyles.Left };
            title.Controls.Add(new Label { Text = AppName, AutoSize = true, ForeColor = Theme.HeaderText, Font = new Font("Segoe UI Semibold", 14f), Margin = new Padding(0) });
            lblSubtitle = new Label { Text = Lang.T("Drive Commodore 1541 · 1571 · 1581 via USB"), AutoSize = true, ForeColor = Theme.HeaderMuted, Font = new Font("Segoe UI", 8.5f), Margin = new Padding(Theme.S(2), 0, 0, 0) };
            title.Controls.Add(lblSubtitle);
            // icona del floppy a sinistra del nome, alta quanto nome + sottotitolo
            FlowLayoutPanel brand = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = title.Margin, Anchor = AnchorStyles.Left };
            title.Margin = new Padding(0);
            title.Anchor = AnchorStyles.Left;
            int logo = Theme.S(40);
            brand.Controls.Add(new PictureBox
            {
                Image = Theme.AppLogo(logo), Size = new Size(logo, logo), SizeMode = PictureBoxSizeMode.Normal,
                BackColor = Theme.Header, Margin = new Padding(0, 0, Theme.S(10), 0), Anchor = AnchorStyles.Left
            });
            brand.Controls.Add(title);
            header.Controls.Add(brand, 0, 0);

            FlowLayoutPanel devBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(8), 0) };
            lblDevCaption = new Label { Text = Lang.T("Drive n°"), AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(8), Theme.S(6), 0) };
            devBox.Controls.Add(lblDevCaption);
            cmbDev = new Segmented("8", "9", "10", "11") { Margin = new Padding(0) };
            cmbDev.SelectedIndexChanged += delegate
            {
                if (drive == null) return;
                drive.Dev = DevNumber;
                drive.Model = null;
                SetState(Lang.T("ONLINE · drive ") + DevNumber, Lang.T("premi \"Leggi stato\" per verificare il drive"));
            };
            devBox.Controls.Add(cmbDev);
            header.Controls.Add(devBox, 1, 0);

            btnConnect = new IconButton(G_CONNECT, Lang.T("Collega"), ButtonKind.Accent) { Anchor = AnchorStyles.Left };
            btnConnect.Click += OnConnect;
            header.Controls.Add(btnConnect, 2, 0);
            btnReset = new IconButton(G_POWER, Lang.T("Reset bus"), ButtonKind.Header) { Anchor = AnchorStyles.Left };
            btnReset.Click += OnReset;
            driveButtons.Add(btnReset);
            header.Controls.Add(btnReset, 3, 0);
            btnCancel = new IconButton(G_CANCEL, Lang.T("Annulla"), ButtonKind.Header) { Anchor = AnchorStyles.Left, Visible = false };
            btnCancel.Click += delegate { if (drive != null) drive.Cancel = true; };
            btnCancel.VisibleChanged += delegate { adaptedWidth = -1; AdaptLayout(); };   // "Annulla" occupa spazio quando compare
            header.Controls.Add(btnCancel, 4, 0);
            header.Controls.Add(new Panel { Width = 1, Height = 1, BackColor = Theme.Header }, 5, 0);

            btnInfo = new IconButton(G_INFO, Lang.T("Info"), ButtonKind.Header) { Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, Theme.S(6), 0) };
            btnInfo.Click += delegate
            {
                using (AboutDialog d = new AboutDialog())
                {
                    d.ShowDialog(this);
                    if (d.OpenGames) SelectTab(TAB_GAMES);
                }
            };
            header.Controls.Add(btnInfo, 6, 0);
            btnTheme = new IconButton(char.ConvertFromUtf32(0xE713), Lang.T("Preferenze"), ButtonKind.Header) { Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, Theme.S(16), 0) };
            btnTheme.Click += OnPreferences;
            header.Controls.Add(btnTheme, 7, 0);

            FlowLayoutPanel state = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right };
            led = new Led { Margin = new Padding(0, Theme.S(10), Theme.S(8), 0) };
            state.Controls.Add(led);
            stateTextBox = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
            lblState = new Label { Text = stateText, AutoSize = true, ForeColor = Theme.HeaderText, Font = new Font("Segoe UI Semibold", 10f), Margin = new Padding(0) };
            lblStateSub = new Label { Text = stateSub, AutoSize = true, ForeColor = Theme.HeaderMuted, Font = new Font("Consolas", 9f), Margin = new Padding(0) };
            stateTextBox.Controls.Add(lblState); stateTextBox.Controls.Add(lblStateSub);
            state.Controls.Add(stateTextBox);
            header.Controls.Add(state, 8, 0);

            // ---------- barra strumenti ----------
            FlowLayoutPanel toolbar = toolBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.Surface, WrapContents = true, Padding = new Padding(Theme.S(10), Theme.S(8), Theme.S(10), Theme.S(8)) };
            toolButtons.Clear();
            FlowLayoutPanel gDisk = Group(toolbar, Lang.T("Disco"), Theme.SectionDisk);
            btnDir = ToolButton(gDisk, G_REFRESH, Lang.T("Directory"), null, OnDirectory, true);
            btnValidate = ToolButton(gDisk, G_CHECK, Lang.T("Valida"), null, OnValidate, true);
            btnFormat = ToolButton(gDisk, G_ERASE, Lang.T("Formatta…"), Lang.T("Formatta"), OnFormat, true);
            btnFormat.IconColor = Theme.Danger;
            FlowLayoutPanel gFile = Group(toolbar, Lang.T("File"), Theme.SectionFile);
            btnSave = ToolButton(gFile, G_DOWNLOAD, Lang.T("Salva sul PC…"), Lang.T("Salva"), OnSaveFiles, true);
            btnWrite = ToolButton(gFile, G_UPLOAD, Lang.T("Scrivi su disco…"), Lang.T("Scrivi"), OnWriteFiles, true);
            btnRename = ToolButton(gFile, G_RENAME, Lang.T("Rinomina…"), Lang.T("Rinomina"), OnRename, true);
            btnScratch = ToolButton(gFile, G_DELETE, Lang.T("Cancella"), null, OnScratch, true);
            btnScratch.IconColor = Theme.Danger;
            FlowLayoutPanel gImg = Group(toolbar, Lang.T("Immagini disco"), Theme.SectionImage);
            btnDiskToImg = ToolButton(gImg, G_SAVE, Lang.T("Crea immagine…"), Lang.T("Crea"), OnDiskToImage, true);
            btnImgToDisk = ToolButton(gImg, G_UPLOAD, Lang.T("Scrivi immagine…"), Lang.T("Scrivi"), OnImageToDisk, true);

            Panel toolbarLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            // ---------- schermo C64 con la directory ----------
            lst = new ListBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Theme.C64Back, ForeColor = Theme.C64Text, Font = ScreenFonts.Screen(),
                SelectionMode = SelectionMode.MultiExtended, IntegralHeight = false, AllowDrop = true
            };
            lst.ItemHeight = lst.Font.Height + Theme.S(ScreenFonts.Current.Spacing);
            lst.DrawItem += DrawEntry;
            lst.DoubleClick += OnSaveFiles;
            lst.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) OnScratch(s, e); else if (e.KeyCode == Keys.F5) OnDirectory(s, e); };
            lst.DragEnter += (s, e) =>
            {
                bool ok = e.Data.GetDataPresent(DataFormats.FileDrop) && drive != null && !Busy;
                e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None;
                if (ok) c64Frame.BackColor = Theme.C64FrameDrop;
            };
            lst.DragLeave += delegate { c64Frame.BackColor = Theme.C64Frame; };
            lst.DragDrop += (s, e) =>
            {
                c64Frame.BackColor = Theme.C64Frame;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null) WriteFiles(files);
            };

            FlowLayoutPanel headLine = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, BackColor = Theme.C64Back, Margin = new Padding(0), Padding = new Padding(0, 0, 0, Theme.S(3)) };
            lblDiskNo = C64Label("0 ", false);
            lblDiskHeader = C64Label("", true);
            headLine.Controls.Add(lblDiskNo); headLine.Controls.Add(lblDiskHeader);

            FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.C64Back, Padding = new Padding(0, Theme.S(3), 0, 0) };
            lblFree = C64Label("", false);
            lblReady = C64Label("READY.", false);
            lblCursor = C64Label("█", false);
            footer.Controls.Add(lblFree); footer.Controls.Add(lblReady); footer.Controls.Add(lblCursor);

            Panel screen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.C64Back, Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(14), Theme.S(10)) };
            screen.Controls.Add(lst);
            screen.Controls.Add(footer);
            screen.Controls.Add(headLine);
            c64Screen = screen;
            panelSaver = null;
            c64Frame = new Panel { Dock = DockStyle.Fill, BackColor = Theme.C64Frame, Padding = new Padding(Theme.S(22), Theme.S(18), Theme.S(22), Theme.S(18)) };
            c64Frame.Controls.Add(screen);

            // ---------- console: comando DOS + schede ----------
            cmdBar = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.Header, ColumnCount = 4, Padding = new Padding(Theme.S(10), Theme.S(6), Theme.S(10), Theme.S(6)) };
            cmdBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cmdBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cmdBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cmdBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cmdBar.Controls.Add(new Label { Text = Lang.T("Comando DOS"), AutoSize = true, ForeColor = Theme.HeaderMuted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(8), 0) }, 0, 0);
            txtCmd = new TextBox
            {
                Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper, Font = new Font("Consolas", 11f),
                BackColor = Theme.InputBack, ForeColor = Theme.InputText, BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, Theme.S(4), Theme.S(8), Theme.S(4))
            };
            txtCmd.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OnSendCommand(s, e); } };
            txtCmd.HandleCreated += delegate { SendMessage(txtCmd.Handle, EM_SETCUEBANNER, (IntPtr)1, Lang.T("scrivi un comando o sceglilo dalla scheda \"Comandi DOS\"")); };
            cmdBar.Controls.Add(txtCmd, 1, 0);
            btnSend = new IconButton(G_SEND, Lang.T("Invia"), ButtonKind.Header); btnSend.Click += OnSendCommand; driveButtons.Add(btnSend);
            btnStatus = new IconButton(G_INFO, Lang.T("Leggi stato"), ButtonKind.Header); btnStatus.Click += OnStatus; driveButtons.Add(btnStatus);
            cmdBar.Controls.Add(btnSend, 2, 0);
            cmdBar.Controls.Add(btnStatus, 3, 0);

            // barra delle schede
            tabBar = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.LogBack, ColumnCount = 7, Padding = new Padding(Theme.S(10), Theme.S(2), Theme.S(10), 0) };
            for (int i = 0; i < 7; i++) tabBar.ColumnStyles.Add(new ColumnStyle(i == 5 ? SizeType.Percent : SizeType.AutoSize, 100));
            tabs = new[] { new TabButton(Lang.T("Adattatore"), Theme.TabAdapter, G_USB), new TabButton(Lang.T("Registro"), Theme.TabLog, G_LIST),
                           new TabButton(Lang.T("Comandi DOS"), Theme.TabDos, G_PROMPT), new TabButton("VICE", Theme.TabVice, G_MONITOR),
                           new TabButton(Lang.T("Giochi"), Theme.TabGames, G_GAME) };
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                tabs[i].Click += delegate { SelectTab(idx); };
                tabBar.Controls.Add(tabs[i], i, 0);
            }
            tabBar.Controls.Add(new Panel { Width = 1, Height = 1, BackColor = Theme.LogBack }, 5, 0);
            btnClear = new IconButton(G_CLEAR, Lang.T("Pulisci registro"), ButtonKind.Header) { Anchor = AnchorStyles.Right };
            btnClear.Click += delegate { logEntries.Clear(); txtLog.Clear(); };
            tabBar.Controls.Add(btnClear, 6, 0);
            tabLine = new Panel { Dock = DockStyle.Top, Height = Theme.S(3), BackColor = Theme.TabLog };

            // scheda 0: registro
            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Theme.LogBack,
                ForeColor = Theme.LogText, Font = new Font("Consolas", 9.5f), DetectUrls = false, ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Panel pageLog = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack, Padding = new Padding(Theme.S(10), Theme.S(6), Theme.S(4), Theme.S(4)) };
            pageLog.Controls.Add(txtLog);

            // scheda 1: comandi DOS pronti
            lstDos = new ListBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Theme.LogBack, ForeColor = Theme.LogText, Font = new Font("Consolas", 10f), IntegralHeight = false, Cursor = Cursors.Hand
            };
            lstDos.ItemHeight = lstDos.Font.Height + Theme.S(8);
            foreach (DosCommand c in DosCommands) lstDos.Items.Add(c);
            lstDos.DrawItem += DrawDosCommand;
            lstDos.MouseClick += (s, e) =>
            {
                int i = lstDos.IndexFromPoint(e.Location);
                if (i >= 0) InsertDosCommand((DosCommand)lstDos.Items[i]);
            };
            lstDos.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && lstDos.SelectedItem != null) { e.SuppressKeyPress = true; InsertDosCommand((DosCommand)lstDos.SelectedItem); } };
            Panel pageDos = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack, Padding = new Padding(Theme.S(10), Theme.S(4), Theme.S(4), Theme.S(4)) };
            pageDos.Controls.Add(lstDos);
            pageDos.Controls.Add(new Label
            {
                Dock = DockStyle.Top, AutoSize = false, Height = Theme.S(24), ForeColor = Theme.HeaderMuted, TextAlign = ContentAlignment.MiddleLeft,
                Text = Lang.T("Clicca un comando per scriverlo nella riga \"Comando DOS\": completa le parti in maiuscolo (NOME, ID…) e premi Invio.")
            });

            // scheda 2: VICE
            Panel pageVice = BuildVicePage();

            Panel pageGames = BuildGamesPage();
            Panel pageAdapter = BuildAdapterPage();
            tabPages = new[] { pageAdapter, pageLog, pageDos, pageVice, pageGames };
            DarkScrollbars(pageAdapter);
            DarkScrollbars(pageGames);
            DarkScrollbars(txtLog); DarkScrollbars(lstDos); DarkScrollbars(pageVice); DarkScrollbars(lst);
            Panel tabContent = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack };
            foreach (Panel p in tabPages) tabContent.Controls.Add(p);

            Panel console = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack };
            // dall'alto: schede, linea colorata, riga comando DOS, contenuto della scheda
            console.Controls.Add(tabContent);
            console.Controls.Add(cmdBar);
            console.Controls.Add(tabLine);
            console.Controls.Add(tabBar);

            split = new SplitContainer
            {
                Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = Theme.Window,
                SplitterWidth = Theme.S(10), FixedPanel = FixedPanel.Panel2
            };
            split.Panel1.Controls.Add(c64Frame);
            split.Panel2.Controls.Add(console);

            Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.S(14), Theme.S(14), Theme.S(14), Theme.S(8)), BackColor = Theme.Window };
            body.Controls.Add(split);

            // ---------- barra di stato ----------
            Panel status = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(30), BackColor = Theme.Surface, Padding = new Padding(Theme.S(14), Theme.S(8), Theme.S(14), Theme.S(8)) };
            progress = new SlimProgress { Dock = DockStyle.Left, Width = Theme.S(260) };
            lblProgress = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted, Padding = new Padding(Theme.S(10), 0, 0, 0), AutoSize = false };
            status.Controls.Add(lblProgress);
            status.Controls.Add(progress);
            Panel statusLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };

            Controls.Add(body);
            Controls.Add(toolbarLine);
            Controls.Add(toolbar);
            Controls.Add(header);
            Controls.Add(statusLine);
            Controls.Add(status);

            SelectTab(currentTab);
            adaptedWidth = -1;
            AdaptLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdaptLayout();
        }

        Panel BuildVicePage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.LogBack, Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), Theme.S(6)), AutoScroll = true };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, BackColor = Theme.LogBack };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            t.Controls.Add(new Label { Text = Lang.T("Emulatore"), AutoSize = true, ForeColor = Theme.HeaderMuted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(10), 0) }, 0, 0);
            txtVice = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.InputBack, ForeColor = Theme.InputText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9.5f), Margin = new Padding(0, Theme.S(5), Theme.S(8), Theme.S(5)) };
            txtVice.Leave += delegate { SaveVicePath(txtVice.Text.Trim()); };
            t.Controls.Add(txtVice, 1, 0);
            IconButton browse = new IconButton(G_FOLDER, Lang.T("Sfoglia…"), ButtonKind.Header);
            browse.Click += OnViceBrowse;
            t.Controls.Add(browse, 2, 0);
            IconButton find = new IconButton(G_SEARCH, Lang.T("Cerca"), ButtonKind.Header);
            find.Click += delegate { Settings.Set("vice", ""); txtVice.Text = Vice.Find(true) ?? ""; SaveVicePath(txtVice.Text); };
            t.Controls.Add(find, 3, 0);

            t.Controls.Add(new Label { Text = Lang.T("Cartella file"), AutoSize = true, ForeColor = Theme.HeaderMuted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(10), 0) }, 0, 1);
            txtViceDir = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.InputBack, ForeColor = Theme.InputText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9.5f), Margin = new Padding(0, Theme.S(5), Theme.S(8), Theme.S(5)), ReadOnly = true };
            txtViceDir.Text = Settings.Get("vicedir", Vice.DefaultFilesDir());
            t.Controls.Add(txtViceDir, 1, 1);
            IconButton browseDir = new IconButton(G_FOLDER, Lang.T("Scegli…"), ButtonKind.Header);
            browseDir.Click += OnViceDirBrowse;
            t.Controls.Add(browseDir, 2, 1);
            IconButton openDir = new IconButton(G_OPEN, Lang.T("Apri"), ButtonKind.Header);
            openDir.Click += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "\"" + Vice.FilesDir() + "\""); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, Text); }
            };
            t.Controls.Add(openDir, 3, 1);

            t.Controls.Add(new Label { Text = Lang.T("Macchina"), AutoSize = true, ForeColor = Theme.HeaderMuted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Theme.S(10), 0) }, 0, 2);
            FlowLayoutPanel mrow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Theme.LogBack };
            cmbMachine = ThemedCombo(Theme.S(150));
            cmbMachine.Margin = new Padding(0, Theme.S(5), Theme.S(12), Theme.S(5));
            cmbMachine.Items.AddRange(Vice.Machines);
            int m; int.TryParse(Settings.Get("vicemachine", "0"), out m);
            cmbMachine.SelectedIndex = Math.Max(0, Math.Min(m, Vice.Machines.Length - 1));
            cmbMachine.SelectedIndexChanged += delegate { Settings.Set("vicemachine", cmbMachine.SelectedIndex.ToString()); };
            mrow.Controls.Add(cmbMachine);
            lblViceState = new Label { AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(9), 0, 0) };
            mrow.Controls.Add(lblViceState);
            t.Controls.Add(mrow, 1, 2);
            t.SetColumnSpan(mrow, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, Theme.S(6), 0, 0), BackColor = Theme.LogBack };
            btnViceFile = new IconButton(G_PLAY, Lang.T("Avvia il file selezionato"), ButtonKind.Accent);
            btnViceFile.Click += OnViceFile;
            btnViceImage = new IconButton(G_FOLDER, Lang.T("Avvia un'immagine dal PC…"), ButtonKind.Header);
            btnViceImage.Click += OnViceImage;
            btnViceDisk = new IconButton(G_DISK, Lang.T("Avvia tutto il disco del drive…"), ButtonKind.Header);
            btnViceDisk.Click += OnViceDisk;
            actions.Controls.Add(btnViceFile); actions.Controls.Add(btnViceImage); actions.Controls.Add(btnViceDisk);
            t.Controls.Add(actions, 0, 3);
            t.SetColumnSpan(actions, 4);

            Label hint = new Label
            {
                AutoSize = true, ForeColor = Theme.HeaderMuted, Margin = new Padding(0, Theme.S(6), 0, 0), MaximumSize = new Size(Theme.S(900), 0),
                Text = Lang.T("I file letti dal disco per VICE vengono salvati nella cartella qui sopra. \"Tutto il disco\" copia prima l'intero disco (circa 10 minuti).")
            };
            t.Controls.Add(hint, 0, 4);
            t.SetColumnSpan(hint, 4);

            page.Controls.Add(t);

            string path = Vice.Find();
            txtVice.Text = path ?? Settings.Get("vice", "");
            UpdateViceState();
            return page;
        }

        void SaveVicePath(string path)
        {
            if (path.Length > 0 && File.Exists(path)) Settings.Set("vice", path);
            UpdateViceState();
        }

        void UpdateViceState()
        {
            if (lblViceState == null) return;
            string p = txtVice.Text.Trim();
            if (p.Length > 0 && File.Exists(p))
            {
                string v = Vice.Version(p);
                lblViceState.Text = Lang.T("✓ VICE trovato") + (v.Length > 0 ? Lang.T(" (versione ") + v + ")" : "");
                lblViceState.ForeColor = Theme.LogReply;
            }
            else
            {
                lblViceState.Text = Lang.T("VICE non trovato: indica x64sc.exe con \"Sfoglia…\" (si scarica da vice-emu.sourceforge.io)");
                lblViceState.ForeColor = Theme.LogError;
            }
        }

        void SelectTab(int idx)
        {
            currentTab = idx;
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Selected = i == idx;
                tabPages[i].Visible = i == idx;
            }
            btnClear.Visible = idx == TAB_LOG;
            tabLine.BackColor = tabs[idx].Accent;
            cmdBar.Visible = idx == TAB_LOG || idx == TAB_DOS;   // nelle altre schede la riga del comando DOS non serve
            if (idx == TAB_GAMES) RefreshRecords();
        }

        void DrawDosCommand(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush b = new SolidBrush(sel ? Theme.HeaderButton : Theme.LogBack)) e.Graphics.FillRectangle(b, e.Bounds);
            DosCommand c = (DosCommand)lstDos.Items[e.Index];
            int colW = Theme.S(230);
            Rectangle r1 = new Rectangle(e.Bounds.X + Theme.S(6), e.Bounds.Y, colW, e.Bounds.Height);
            Rectangle r2 = new Rectangle(e.Bounds.X + Theme.S(6) + colW, e.Bounds.Y, e.Bounds.Width - colW - Theme.S(6), e.Bounds.Height);
            TextFormatFlags f = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(e.Graphics, c.Cmd, lstDos.Font, r1, Theme.LogSent, f);
            TextRenderer.DrawText(e.Graphics, c.Desc, Font, r2, Theme.LogText, f);
        }

        void InsertDosCommand(DosCommand c)
        {
            txtCmd.Text = c.Cmd;
            txtCmd.Focus();
            int colon = c.Cmd.IndexOf(':');
            if (colon >= 0)
            {
                // seleziona la prima parte da completare (es. NOME)
                int end = colon + 1;
                while (end < c.Cmd.Length && c.Cmd[end] != ',' && c.Cmd[end] != '=') end++;
                txtCmd.Select(colon + 1, end - colon - 1);
            }
            else txtCmd.Select(c.Cmd.Length, 0);
        }

        void DrawEntry(object sender, DrawItemEventArgs e)
        {
            Color back = Theme.C64Back, fore = Theme.C64Text;
            if (e.Index >= 0 && (e.State & DrawItemState.Selected) != 0) { back = Theme.C64Text; fore = Theme.C64Back; }
            using (SolidBrush b = new SolidBrush(back)) e.Graphics.FillRectangle(b, e.Bounds);
            if (e.Index < 0) return;
            DirEntry en = (DirEntry)lst.Items[e.Index];
            ScreenFonts.Draw(e.Graphics, FormatEntry(en), lst.Font, fore, e.Bounds);
        }

        static string FormatEntry(DirEntry e)
        {
            string name = "\"" + e.Name + "\"";
            return e.Blocks.ToString().PadRight(5) + name.PadRight(19) + (e.Splat ? "*" : " ") + e.Type + (e.Locked ? "<" : "");
        }

        void ShowEmptyScreen()
        {
            lst.Items.Clear();
            lblDiskNo.Text = "";
            lblDiskHeader.BackColor = Theme.C64Back; lblDiskHeader.ForeColor = Theme.C64Text;
            lblDiskHeader.Text = Lang.T("     **** XUMER TOOL ****");
            lblFree.Text = Lang.T("\nCOLLEGA L'ADATTATORE E PREMI DIRECTORY.\n\n");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetSplit();
        }

        void SetSplit()
        {
            split.SplitterDistance = Math.Max(Theme.S(200), split.Height - Theme.S(285));
        }

        // ================= temi =================

        void OnPreferences(object sender, EventArgs e)
        {
            using (PreferencesDialog d = new PreferencesDialog(Busy))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                bool rebuild = d.AutoConnect != AutoConnect;
                Settings.Set("saver", d.SaverOn ? "1" : "0");
                Settings.Set("savermode", d.SaverFull ? "full" : "dir");
                Settings.Set("autoconnect", d.AutoConnect ? "1" : "0");
                if (d.FontId != ScreenFonts.Current.Id) { Settings.Set("screenfont", d.FontId); rebuild = true; }
                if (d.ThemeName != Theme.Current)
                {
                    Settings.Set("theme", d.ThemeName);
                    if (!Busy) Theme.Apply(d.ThemeName);
                    rebuild = true;
                }
                if (rebuild)
                {
                    if (Busy) Log(Lang.T("Tema e carattere verranno applicati alla prossima apertura o quando l'operazione sarà finita."));
                    else RebuildUi();
                }
                if (d.TrySaver) BeginInvoke((Action)ShowSaver);
                bool langChanged = d.LangCode != Settings.Get("lang", Lang.Current);
                if (langChanged) Settings.Set("lang", d.LangCode);
                if (langChanged && d.LangCode != Lang.Current)
                {
                    // i testi si traducono all'avvio: serve riavviare il programma
                    string msg = Lang.T("La nuova lingua si applica riavviando il programma.");
                    if (Busy) Log(msg);
                    else if (MessageBox.Show(this, msg + "\n\n" + Lang.T("Riavviare ora?"), Lang.Current == "it" ? "Lingua · Language" : "Language · Lingua",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        BeginInvoke((Action)delegate { Application.Restart(); });
                }
            }
        }
        // ================= salvaschermo =================

        readonly IdleWatcher idle = new IdleWatcher();
        System.Windows.Forms.Timer idleTimer;
        ScreenSaverForm saverForm;

        static bool SaverEnabled { get { return Settings.Get("saver", "1") == "1"; } }
        static bool SaverFullScreen { get { return Settings.Get("savermode", "dir") == "full"; } }

        Panel c64Screen;
        PetsciiSaver panelSaver;
        DateTime panelSaverStarted;

        void StartIdleWatch()
        {
            Application.AddMessageFilter(idle);
            idle.Input += delegate
            {
                // nella directory: qualsiasi tasto o movimento del mouse riporta la directory
                if (panelSaver != null && (DateTime.Now - panelSaverStarted).TotalSeconds > 1) StopPanelSaver();
            };
            idleTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            idleTimer.Tick += delegate
            {
                if (!SaverEnabled || saverForm != null || panelSaver != null || WindowState == FormWindowState.Minimized) return;
                if ((DateTime.Now - idle.LastInput).TotalMinutes < 5) return;
                // a tutto schermo solo se l'app è in primo piano, per non coprire il lavoro in altre finestre
                if (SaverFullScreen && ActiveForm != this) return;
                ShowSaver();
            };
            idleTimer.Start();
        }

        void ShowSaver()
        {
            if (!SaverFullScreen) { ShowPanelSaver(); return; }
            if (saverForm != null) return;
            saverForm = new ScreenSaverForm(Screen.FromControl(this).Bounds);
            saverForm.FormClosed += delegate { saverForm = null; idle.LastInput = DateTime.Now; };
            saverForm.Show(this);
            saverForm.Activate();
        }

        /// <summary>Salvaschermo dentro lo schermo blu della directory.</summary>
        void ShowPanelSaver()
        {
            if (panelSaver != null || c64Screen == null) return;
            AttachPanelSaver();
            panelSaverStarted = DateTime.Now;
            panelSaver.Start();
        }

        /// <summary>Mette il salvaschermo al posto dello schermo della directory, dentro la cornice C64.</summary>
        void AttachPanelSaver()
        {
            panelSaver = new PetsciiSaver(10f, Theme.C64Back, Lang.T("PREMI UN TASTO O MUOVI IL MOUSE PER TORNARE ALLA DIRECTORY")) { Dock = DockStyle.Fill };
            c64Screen.Visible = false;
            c64Frame.Controls.Add(panelSaver);
            c64Frame.PerformLayout();
        }

        void StopPanelSaver()
        {
            if (panelSaver == null) return;
            PetsciiSaver p = panelSaver;
            panelSaver = null;
            p.Stop();
            if (p.Parent != null) p.Parent.Controls.Remove(p);
            p.Dispose();
            if (c64Screen != null) c64Screen.Visible = true;
        }

        /// <summary>Per le anteprime: il salvaschermo nella directory, fermo a un istante.</summary>
        public void DemoPanelSaver(float seconds)
        {
            AttachPanelSaver();
            panelSaver.SeekForPreview(seconds);
        }

        void ApplyTheme(string name)
        {
            if (Busy || name == Theme.Current) return;
            Theme.Apply(name);
            Settings.Set("theme", name);
            RebuildUi();
        }

        /// <summary>Ricostruisce l'interfaccia (dopo un cambio di tema o di carattere) mantenendo lo stato.</summary>
        void RebuildUi()
        {
            if (Busy) return;
            StopPanelSaver();
            string cmd = txtCmd.Text;
            int dev = cmbDev.SelectedIndex;
            string vice = txtVice.Text;
            SuspendLayout();
            List<Control> old = new List<Control>();
            foreach (Control c in Controls) old.Add(c);
            Controls.Clear();
            foreach (Control c in old) c.Dispose();
            driveButtons.Clear();
            lblViceState = null;
            BuildUi();
            txtCmd.Text = cmd;
            cmbDev.SelectedIndex = dev;
            txtVice.Text = vice;
            UpdateViceState();
            if (currentDir != null) ShowDirectory(currentDir); else ShowEmptyScreen();
            RenderLog();
            UpdateButtons();
            ResumeLayout(true);
            SetSplit();
        }

        int DevNumber { get { return 8 + cmbDev.SelectedIndex; } }
        bool Busy { get { return worker != null && worker.IsAlive; } }

        void UpdateButtons()
        {
            bool online = drive != null || demo;
            bool busy = Busy;
            foreach (Button b in driveButtons) b.Enabled = online && !busy;
            btnConnect.Enabled = !busy;
            btnConnect.Text = online ? Lang.T("Scollega") : Lang.T("Collega");
            btnConnect.Glyph = online ? G_CANCEL : G_CONNECT;
            btnConnect.Invalidate();
            btnCancel.Visible = busy;
            cmbDev.Enabled = !busy;
            led.Color = busy ? Theme.Busy : online ? Theme.Ok : Theme.Off;
            btnViceFile.Enabled = online && !busy;
            btnViceDisk.Enabled = online && !busy;
            btnViceImage.Enabled = !busy;
        }

        // ================= log / progresso (thread-safe) =================

        void Ui(Action a)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke(a); } catch (InvalidOperationException) { } }
            else a();
        }

        static Color LogColor(string s)
        {
            if (s.StartsWith("> ")) return Theme.LogSent;
            if (s.StartsWith("< "))
            {
                int code = CbmDrive.StatusCode(s.Substring(2));
                return code >= 20 && code != 73 ? Theme.LogError : Theme.LogReply;
            }
            if (s.StartsWith("ERROR") || s.StartsWith(Lang.T("Errore"))) return Theme.LogError;   // "ERRORE" e "ERROR"
            return Theme.LogText;
        }

        void AppendLog(string time, string s)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = Theme.LogTime;
            txtLog.AppendText(time + "  ");
            txtLog.SelectionColor = LogColor(s);
            txtLog.AppendText(s + "\n");
        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        const int WM_SETREDRAW = 0x000B;

        /// <summary>Ridisegna tutto il registro in un colpo solo (senza sfarfallio).</summary>
        void RenderLog()
        {
            SendMessage(txtLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                txtLog.Clear();
                foreach (KeyValuePair<string, string> kv in logEntries) AppendLog(kv.Key, kv.Value);
            }
            finally
            {
                SendMessage(txtLog.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                txtLog.Invalidate();
            }
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        void Log(string s)
        {
            Ui(delegate
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                logEntries.Add(new KeyValuePair<string, string>(time, s));
                if (logEntries.Count > 3000)
                {
                    // tiene il registro leggero: elimina le righe più vecchie anche dallo schermo
                    logEntries.RemoveRange(0, 1000);
                    RenderLog();
                }
                else AppendLog(time, s);
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
                if (s.StartsWith("ERROR") && currentTab != TAB_LOG) SelectTab(TAB_LOG);   // "ERRORE" e "ERROR"
            });
        }

        DateTime lastProgress;
        void SetProgress(int done, int total)
        {
            if (done < total && (DateTime.Now - lastProgress).TotalMilliseconds < 100) return;
            lastProgress = DateTime.Now;
            Ui(delegate
            {
                progress.Maximum = Math.Max(1, total);
                progress.Value = Math.Min(done, progress.Maximum);
                lblProgress.Text = done + " / " + total;
            });
        }

        void SetState(string text, string sub)
        {
            Ui(delegate
            {
                stateText = text; stateSub = sub; lblState.Text = text; lblStateSub.Text = sub;
                adaptedWidth = -1; AdaptLayout();   // il testo dello stato cambia la larghezza dell'intestazione
            });
        }

        // ================= adattamento alla larghezza =================
        // Quando la finestra si stringe, le barre si compattano a gradini invece di sovrapporsi.
        // Intestazione: 0 tutto · 1 Info e Preferenze solo icona · 2 senza sottotitolo · 3 senza dettaglio dello stato
        //               4 Reset/Annulla solo icona · 5 senza "Drive n°" · 6 solo LED (stato nel suggerimento) · 7 Collega solo icona
        // Barra strumenti: testi completi → testi brevi → solo icone.
        const int HEADER_LEVELS = 8;

        void AdaptLayout()
        {
            if (adapting || headerBar == null || toolBar == null || tabBar == null) return;
            int w = ClientSize.Width;
            if (w <= 0 || w == adaptedWidth) return;
            adapting = true;
            try
            {
                adaptedWidth = w;
                SuspendLayout();
                int level = 0;
                for (; level < HEADER_LEVELS - 1; level++)
                {
                    SetHeaderLevel(level);
                    if (headerBar.GetPreferredSize(Size.Empty).Width <= w) break;
                }
                if (level == HEADER_LEVELS - 1) SetHeaderLevel(level);

                for (int t = 0; t < 3; t++)
                {
                    SetToolbarLevel(t);
                    if (ToolbarWidth() <= w) break;
                }

                btnClear.Compact = false;
                if (tabBar.GetPreferredSize(Size.Empty).Width > w - Theme.S(28)) btnClear.Compact = true;
            }
            finally
            {
                ResumeLayout(true);
                adapting = false;
            }
        }

        void SetHeaderLevel(int l)
        {
            headerLevel = l;
            btnInfo.Compact = btnTheme.Compact = l >= 1;
            lblSubtitle.Visible = l < 2;
            lblStateSub.Visible = l < 3;
            btnReset.Compact = btnCancel.Compact = l >= 4;
            lblDevCaption.Visible = l < 5;
            stateTextBox.Visible = l < 6;
            btnConnect.Compact = l >= 7;
            UpdateStateTip();
        }

        void UpdateStateTip()
        {
            if (led == null) return;
            string tip = headerLevel >= 6 ? stateText + "\n" + stateSub : headerLevel >= 3 ? stateSub : null;
            IconButton.Tips.SetToolTip(led, tip);
            IconButton.Tips.SetToolTip(lblState, headerLevel >= 3 ? stateSub : null);
        }

        void SetToolbarLevel(int l)
        {
            foreach (IconButton b in toolButtons)
            {
                b.Text = l == 1 && b.ShortText != null ? b.ShortText : b.LongText;
                b.Compact = l >= 2;
            }
        }

        int ToolbarWidth()
        {
            int w = toolBar.Padding.Horizontal;
            foreach (Control c in toolBar.Controls)
                w += c.GetPreferredSize(Size.Empty).Width + c.Margin.Horizontal;
            return w;
        }

        // ================= esecuzione in background =================

        void Run(string title, Action job)
        {
            if (Busy) return;
            if (drive != null) drive.Cancel = false;
            progress.Value = 0; lblProgress.Text = title + "…";
            worker = new Thread(delegate ()
            {
                DateTime t0 = DateTime.Now;
                try
                {
                    job();
                    Ui(delegate { lblProgress.Text = title + Lang.T(": fatto (") + (DateTime.Now - t0).TotalSeconds.ToString("0.0") + " s)"; });
                }
                catch (OperationCanceledException)
                {
                    Log(title + Lang.T(": annullato."));
                    Ui(delegate { lblProgress.Text = title + Lang.T(": annullato"); });
                }
                catch (Exception ex)
                {
                    Log(Lang.T("ERRORE: ") + ex.Message);
                    Ui(delegate { lblProgress.Text = title + Lang.T(": errore"); });
                    if (!(ex is DriveException) && xum != null) { Log(Lang.T("Ripristino comunicazione con l'adattatore…")); xum.Recover(); }
                }
                finally { Ui(UpdateButtons); }
            });
            worker.IsBackground = true;
            worker.Start();
            UpdateButtons();
        }

        // ================= collegamento =================

        string OnlineTitle()
        {
            return Lang.T("ONLINE · drive ") + drive.Dev + (drive.Model != null && drive.Model != "?" ? " · " + drive.Model : "");
        }

        void OnConnect(object sender, EventArgs e)
        {
            if (drive != null || demo) { Disconnect(); return; }
            Run(Lang.T("Collegamento"), ConnectJob(DevNumber));
        }

        static bool AutoConnect { get { return Settings.Get("autoconnect", "1") == "1"; } }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (interactiveMode && AutoConnect)
            {
                Log(Lang.T("Collegamento automatico all'avvio (si disattiva nella scheda Adattatore)."));
                OnConnect(this, EventArgs.Empty);
            }
        }

        Action ConnectJob(int dev)
        {
            return delegate
            {
                Xum1541 x = Xum1541.Open();
                Log(string.Format(Lang.T("Adattatore xum1541 trovato: firmware v{0} (capacità 0x{1:X2})"), x.Version, x.Capabilities));
                CbmDrive d = new CbmDrive(x) { Dev = dev, Log = Log, Progress = SetProgress };
                xum = x; drive = d;
                string st;
                try { st = d.ReadStatus(); }
                catch (Exception)
                {
                    Log(Lang.T("Il drive non risponde: provo un reset del bus IEC…"));
                    x.Recover();
                    x.BusReset();
                    Thread.Sleep(2500);
                    try { st = d.ReadStatus(); }
                    catch
                    {
                        SetState(Lang.T("ADATTATORE COLLEGATO"), Lang.T("il drive ") + dev + Lang.T(" non risponde: accendilo e premi \"Leggi stato\""));
                        throw;
                    }
                }
                SetState(OnlineTitle(), st);
            };
        }

        void Disconnect()
        {
            if (xum != null) xum.Dispose();
            xum = null; drive = null; demo = false;
            SetState("OFFLINE", Lang.T("adattatore non collegato"));
            Log(Lang.T("Adattatore scollegato."));
            UpdateButtons();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Busy)
            {
                if (MessageBox.Show(this, Lang.T("C'è un'operazione in corso. Uscire comunque?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                { e.Cancel = true; return; }
                if (drive != null) drive.Cancel = true;
                worker.Join(3000);
            }
            if (xum != null) xum.Dispose();
            base.OnFormClosing(e);
        }

        void OnReset(object sender, EventArgs e)
        {
            Run(Lang.T("Reset"), delegate
            {
                Log(Lang.T("Reset bus IEC…"));
                xum.BusReset();
                Thread.Sleep(2500);
                string st = drive.ReadStatus();
                SetState(OnlineTitle(), st);
            });
        }

        void OnStatus(object sender, EventArgs e)
        {
            Run(Lang.T("Stato"), delegate { string st = drive.ReadStatus(); SetState(OnlineTitle(), st); });
        }

        void OnSendCommand(object sender, EventArgs e)
        {
            string c = txtCmd.Text.Trim();
            if (c.Length == 0 || drive == null || Busy) return;
            SelectTab(TAB_LOG);
            Run(Lang.T("Comando"), delegate { drive.Command(c); });
        }

        // ================= directory e file =================

        void ShowDirectory(DiskDirectory d)
        {
            Ui(delegate
            {
                currentDir = d;
                lst.BeginUpdate();
                lst.Items.Clear();
                foreach (DirEntry en in d.Entries) lst.Items.Add(en);
                lst.EndUpdate();
                lblDiskNo.Text = "0 ";
                lblDiskHeader.BackColor = Theme.C64Text; lblDiskHeader.ForeColor = Theme.C64Back;
                lblDiskHeader.Text = "\"" + d.DiskName.PadRight(16) + "\" " + d.DiskId;
                lblFree.Text = (d.BlocksFree >= 0 ? d.BlocksFree.ToString() : "?") + Lang.T(" BLOCKS FREE.");
            });
        }

        void OnDirectory(object sender, EventArgs e)
        {
            if (drive == null || Busy) return;
            Run(Lang.T("Directory"), delegate { ShowDirectory(drive.ReadDirectory()); });
        }

        List<DirEntry> SelectedEntries()
        {
            List<DirEntry> r = new List<DirEntry>();
            foreach (object o in lst.SelectedItems) r.Add((DirEntry)o);
            return r;
        }

        static string SafeFileName(string name, string type)
        {
            string n = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            if (n.Length == 0) n = "senza_nome";
            return n + "." + type.ToLowerInvariant();
        }

        void OnSaveFiles(object sender, EventArgs e)
        {
            if (drive == null || Busy) return;
            List<DirEntry> sel = SelectedEntries();
            if (sel.Count == 0) { MessageBox.Show(this, Lang.T("Seleziona uno o più file nella directory (premi prima \"Directory\")."), Text); return; }
            string folder = null, single = null;
            if (sel.Count == 1)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.FileName = SafeFileName(sel[0].Name, sel[0].Type);
                    d.Filter = Lang.T("Tutti i file|*.*");
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    single = d.FileName;
                }
            }
            else
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog { Description = Lang.T("Cartella dove salvare i file") })
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    folder = d.SelectedPath;
                }
            }
            Run(Lang.T("Lettura file"), delegate
            {
                foreach (DirEntry en in sel)
                {
                    byte[] data = drive.ReadFile(en);
                    string path = single ?? Path.Combine(folder, SafeFileName(en.Name, en.Type));
                    File.WriteAllBytes(path, data);
                    Log(Lang.T("Salvato: ") + path);
                }
            });
        }

        void OnWriteFiles(object sender, EventArgs e)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Multiselect = true;
                d.Filter = Lang.T("File Commodore (*.prg;*.seq;*.p00;*.s00;*.usr)|*.prg;*.seq;*.p00;*.s00;*.usr|Tutti i file|*.*");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                WriteFiles(d.FileNames);
            }
        }

        class PendingFile { public byte[] Name; public char Type; public byte[] Data; public string Source; }

        static PendingFile PrepareFile(string path)
        {
            PendingFile p = new PendingFile { Source = path };
            byte[] data = File.ReadAllBytes(path);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string baseName = Path.GetFileNameWithoutExtension(path);
            p.Type = 'P';
            if (ext == ".seq") p.Type = 'S';
            else if (ext == ".usr") p.Type = 'U';

            // formato PC64 (.P00/.S00/.U00): intestazione di 26 byte con il nome originale
            if (ext.Length == 4 && "psu".IndexOf(ext[1]) >= 0 && char.IsDigit(ext[2]) && char.IsDigit(ext[3])
                && data.Length >= 26 && System.Text.Encoding.ASCII.GetString(data, 0, 7) == "C64File")
            {
                int n = 0;
                while (n < 16 && data[8 + n] != 0) n++;
                p.Name = new byte[n];
                Array.Copy(data, 8, p.Name, 0, n);
                p.Type = char.ToUpperInvariant(ext[1]);
                byte[] rest = new byte[data.Length - 26];
                Array.Copy(data, 26, rest, 0, rest.Length);
                p.Data = rest;
                return p;
            }
            if (baseName.Length > 16) baseName = baseName.Substring(0, 16);
            p.Name = Petscii.FromText(baseName);
            p.Data = data;
            return p;
        }

        void WriteFiles(string[] paths)
        {
            if (drive == null || Busy) return;
            List<PendingFile> files = new List<PendingFile>();
            foreach (string path in paths)
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".d64" || ext == ".d71" || ext == ".d81")
                {
                    MessageBox.Show(this, Lang.T("Per scrivere un'immagine disco usa il pulsante \"Scrivi immagine…\"."), Text);
                    return;
                }
                try { files.Add(PrepareFile(path)); }
                catch (Exception ex) { MessageBox.Show(this, path + ": " + ex.Message, Text); return; }
            }
            if (files.Count == 1)
            {
                string[] r = InputDialog.Ask(this, Lang.T("Scrivi file su disco"), new[] { Lang.T("Nome sul disco:") }, new[] { Petscii.ToText(files[0].Name) }, new[] { 16 });
                if (r == null || r[0].Trim().Length == 0) return;
                files[0].Name = Petscii.FromText(r[0]);
            }
            Run(Lang.T("Scrittura file"), delegate
            {
                foreach (PendingFile f in files)
                    drive.WriteFile(f.Name, f.Type, f.Data);
                ShowDirectory(drive.ReadDirectory());
            });
        }

        void OnRename(object sender, EventArgs e)
        {
            List<DirEntry> sel = SelectedEntries();
            if (sel.Count != 1) { MessageBox.Show(this, Lang.T("Seleziona un solo file."), Text); return; }
            string[] r = InputDialog.Ask(this, Lang.T("Rinomina"), new[] { Lang.T("Nuovo nome:") }, new[] { sel[0].Name }, new[] { 16 });
            if (r == null || r[0].Trim().Length == 0) return;
            Run(Lang.T("Rinomina"), delegate { drive.Rename(sel[0], r[0]); ShowDirectory(drive.ReadDirectory()); });
        }

        void OnScratch(object sender, EventArgs e)
        {
            if (drive == null || Busy) return;
            List<DirEntry> sel = SelectedEntries();
            if (sel.Count == 0) { MessageBox.Show(this, Lang.T("Seleziona i file da cancellare."), Text); return; }
            if (MessageBox.Show(this, Lang.T("Cancellare definitivamente ") + sel.Count + Lang.T(" file dal disco?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Run(Lang.T("Cancella"), delegate
            {
                foreach (DirEntry en in sel) drive.Scratch(en);
                ShowDirectory(drive.ReadDirectory());
            });
        }

        void OnValidate(object sender, EventArgs e) { Run(Lang.T("Valida"), delegate { drive.Validate(); }); }

        void OnFormat(object sender, EventArgs e)
        {
            string[] r = InputDialog.Ask(this, Lang.T("Formatta disco"), new[] { Lang.T("Nome disco:"), Lang.T("ID (2 caratteri, vuoto = formattazione veloce):") }, new[] { "DISCO", "01" }, new[] { 16, 2 });
            if (r == null || r[0].Trim().Length == 0) return;
            if (MessageBox.Show(this, Lang.T("Tutti i dati sul disco verranno cancellati. Continuare?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Run(Lang.T("Formattazione"), delegate { drive.Format(r[0].Trim(), r[1].Trim()); ShowDirectory(drive.ReadDirectory()); });
        }

        // ================= immagini =================

        void OnDiskToImage(object sender, EventArgs e)
        {
            string path;
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = Lang.T("Immagine 1541 (*.d64)|*.d64|Immagine 1571 doppia faccia (*.d71)|*.d71|Immagine 1581 (*.d81)|*.d81");
                d.FileName = currentDir != null && currentDir.DiskName.Length > 0 ? SafeFileName(currentDir.DiskName, "d64") : "disco.d64";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                path = d.FileName;
            }
            ImageKind kind = ImageKind.D64;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".d71") kind = ImageKind.D71;
            else if (ext == ".d81") kind = ImageKind.D81;
            Run(Lang.T("Crea immagine"), delegate
            {
                byte[] img = drive.ReadImage(kind);
                File.WriteAllBytes(path, img);
                Log(Lang.T("Immagine salvata: ") + path);
            });
        }

        void OnImageToDisk(object sender, EventArgs e)
        {
            string path;
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = Lang.T("Immagini disco (*.d64;*.d71;*.d81)|*.d64;*.d71;*.d81|Tutti i file|*.*");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                path = d.FileName;
            }
            byte[] img = File.ReadAllBytes(path);
            ImageKind? k = CbmDrive.KindFromSize(img.Length);
            if (k == null) { MessageBox.Show(this, Lang.T("Dimensione immagine non riconosciuta (") + img.Length + Lang.T(" byte). Sono supportate D64 (35 tracce), D71 e D81."), Text); return; }
            ImageKind kind = k.Value;
            DialogResult ans = MessageBox.Show(this,
                Lang.T("Scrivere ") + Path.GetFileName(path) + " (" + kind + Lang.T(") sul disco nel drive ") + DevNumber + "?\n\n" +
                Lang.T("TUTTI i dati del disco verranno sovrascritti.\n\n") +
                Lang.T("Sì = formatta prima il disco (consigliato)\nNo = scrivi senza formattare (disco già formattato)"),
                Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (ans == DialogResult.Cancel) return;
            bool format = ans == DialogResult.Yes;
            Run(Lang.T("Scrivi immagine"), delegate
            {
                drive.WriteImage(kind, img, format);
                ShowDirectory(drive.ReadDirectory());
            });
        }

        // ================= VICE =================

        string ViceExe()
        {
            string p = txtVice.Text.Trim();
            if (p.Length == 0 || !File.Exists(p))
            {
                MessageBox.Show(this, Lang.T("Indica prima dove si trova VICE (x64sc.exe) nella scheda VICE."), Text);
                SelectTab(TAB_VICE);
                return null;
            }
            try { return Vice.ExeFor(p, cmbMachine.SelectedIndex); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text); return null; }
        }

        void LaunchVice(string exe, string target)
        {
            try
            {
                Vice.Autostart(exe, target);
                Log(Lang.T("VICE avviato: ") + Path.GetFileName(exe) + " " + Path.GetFileName(target));
            }
            catch (Exception ex) { Log(Lang.T("ERRORE: avvio di VICE non riuscito: ") + ex.Message); }
        }

        void OnViceDirBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = Lang.T("Cartella dove salvare i file letti dal disco per VICE");
                d.ShowNewFolderButton = true;
                try { d.SelectedPath = Vice.FilesDir(); } catch { }
                if (d.ShowDialog(this) != DialogResult.OK) return;
                Settings.Set("vicedir", d.SelectedPath);
                txtViceDir.Text = d.SelectedPath;
                Log(Lang.T("Cartella dei file per VICE: ") + d.SelectedPath);
            }
        }

        void OnViceBrowse(object sender, EventArgs e)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = Lang.T("VICE C64 (x64sc.exe)|x64sc.exe|Programmi (*.exe)|*.exe");
                d.Title = Lang.T("Dov'è x64sc.exe di VICE?");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                txtVice.Text = d.FileName;
                SaveVicePath(d.FileName);
            }
        }

        void OnViceFile(object sender, EventArgs e)
        {
            if (drive == null || Busy) return;
            List<DirEntry> sel = SelectedEntries();
            if (sel.Count != 1) { MessageBox.Show(this, Lang.T("Seleziona nella directory il programma da avviare."), Text); return; }
            DirEntry en = sel[0];
            if (en.Type != "PRG") { MessageBox.Show(this, Lang.T("Si possono avviare solo i file PRG."), Text); return; }
            string exe = ViceExe();
            if (exe == null) return;
            Run(Lang.T("Avvio in VICE"), delegate
            {
                byte[] data = drive.ReadFile(en);
                string path = Path.Combine(Vice.FilesDir(), SafeFileName(en.Name, "prg"));
                File.WriteAllBytes(path, data);
                Log(Lang.T("File salvato: ") + path);
                Ui(delegate { LaunchVice(exe, path); });
            });
        }

        void OnViceImage(object sender, EventArgs e)
        {
            string exe = ViceExe();
            if (exe == null) return;
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = Lang.T("Immagini e programmi C64 (*.d64;*.d71;*.d81;*.prg;*.t64;*.tap;*.crt)|*.d64;*.d71;*.d81;*.prg;*.t64;*.tap;*.crt|Tutti i file|*.*");
                d.Title = Lang.T("Scegli cosa avviare in VICE");
                string last = Settings.Get("vicelastdir", "");
                if (last.Length > 0 && Directory.Exists(last)) d.InitialDirectory = last;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                Settings.Set("vicelastdir", Path.GetDirectoryName(d.FileName));
                LaunchVice(exe, d.FileName);
            }
        }

        void OnViceDisk(object sender, EventArgs e)
        {
            if (drive == null || Busy) return;
            string exe = ViceExe();
            if (exe == null) return;
            if (MessageBox.Show(this, Lang.T("L'intero disco verrà copiato in un'immagine D64 temporanea e poi avviato in VICE.\n\nCon la copia normale servono circa 10 minuti. Continuare?"),
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            string name = currentDir != null && currentDir.DiskName.Length > 0 ? currentDir.DiskName : "disco";
            Run(Lang.T("Disco in VICE"), delegate
            {
                byte[] img = drive.ReadImage(ImageKind.D64);
                string path = Path.Combine(Vice.FilesDir(), SafeFileName(name, "d64"));
                File.WriteAllBytes(path, img);
                Log(Lang.T("Immagine salvata: ") + path);
                Ui(delegate { LaunchVice(exe, path); });
            });
        }

        // ================= anteprima (solo sviluppo) =================

        public void Demo(int tab)
        {
            demo = true;
            DiskDirectory d = new DiskDirectory { DiskName = Lang.T("GIOCHI 1"), DiskId = "01 2A", BlocksFree = 412 };
            string[][] files = {
                new[] { "34", "MENU", "PRG" }, new[] { "129", "AVVENTURA", "PRG" }, new[] { "58", "SCACCHI", "PRG" },
                new[] { "12", "PUNTEGGI", "SEQ" }, new[] { "21", Lang.T("MUSICA DEMO"), "PRG" }, new[] { "1", "----------------", "DEL" } };
            foreach (string[] f in files)
                d.Entries.Add(new DirEntry { Blocks = int.Parse(f[0]), Name = f[1], NameBytes = Petscii.FromText(f[1]), Type = f[2] });
            ShowDirectory(d);
            lst.SelectedIndex = 1;
            SetState(Lang.T("ONLINE · drive 8 · 1571"), Lang.T("00, OK,00,00"));
            Log(Lang.T("Adattatore xum1541 trovato: firmware v8 (capacità 0x03)"));
            Log(Lang.T("< 73,CBM DOS V3.0 1571,00,00"));
            UpdateButtons();
            SelectTab(tab);
            progress.Maximum = 683; progress.Value = 410;
            lblProgress.Text = "410 / 683";
        }
    }
}
