// Finestra "Info": natura del progetto, crediti e licenze dei progetti su cui si basa XUMER TOOL.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XumerTool
{
    class AboutDialog : Form
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);

        public const string AppVersion = "1.2.1b";   // "b" = beta

        /// <summary>L'utente ha premuto "Giochi": la finestra principale apre la scheda Giochi.</summary>
        public bool OpenGames;

        class Credit
        {
            public string Name, Authors, Role, License, Url;
            public Credit(string name, string authors, string role, string license, string url)
            { Name = name; Authors = authors; Role = role; License = license; Url = url; }
        }

        class Section
        {
            public string Title;
            public Credit[] Items;
            public Section(string title, params Credit[] items) { Title = title; Items = items; }
        }

        // Licenze verificate sui sorgenti e sui repository ufficiali.
        static readonly Section[] Sections =
        {
            new Section(Lang.T("Adattatore e protocollo di comunicazione"),
                new Credit("OpenCBM", Lang.T("Michael Klein, Spiro Trikaliotis, Wolfgang Moser e collaboratori"),
                    Lang.T("Progetto di riferimento per i drive Commodore su PC: il protocollo USB dell'adattatore xum1541 è implementato secondo le sue specifiche."),
                    Lang.T("GPL-2.0 o successiva"), "https://github.com/OpenCBM/OpenCBM"),
                new Credit(Lang.T("Firmware xum1541"), Lang.T("Nate Lawson, sulla base di xu1541 di Till Harbaum"),
                    Lang.T("Firmware che trasforma il microcontrollore in un adattatore USB per il bus seriale IEC."),
                    Lang.T("GPL-2.0 o successiva"), "https://github.com/OpenCBM/OpenCBM/tree/master/xum1541"),
                new Credit("LUFA", Lang.T("Dean Camera"),
                    Lang.T("Libreria USB per microcontrollori AVR, inclusa nel firmware xum1541."),
                    Lang.T("Permissiva (stile MIT)"), "http://www.fourwalledcubicle.com/LUFA.php"),
                new Credit(Lang.T("Routine 6502 per la copia veloce"), Lang.T("Michael Klein (protocollo S2), Joe Forster/STA (turbo read/write)"),
                    Lang.T("Codice eseguito all'interno del drive, tratto da OpenCBM/d64copy e incluso nell'applicazione."),
                    "BSD-3-Clause", "https://github.com/OpenCBM/OpenCBM/tree/master/opencbm/libd64copy")),
            new Section(Lang.T("Hardware e progetti di riferimento"),
                new Credit("C64-XUM1541", "tebl",
                    Lang.T("Progetto della scheda con Arduino Pro Micro e connettore DIN per il bus IEC."),
                    Lang.T("Non indicata nel repository"), "https://github.com/tebl/C64-XUM1541"),
                new Credit("RetroFixer", "retrofixer.it",
                    Lang.T("Programma web per l'adattatore xum1541, da cui è nata l'idea di questa applicazione, e firmware RFIEC."),
                    Lang.T("RFIEC: GPL-3.0"), "https://retrofixer.it/xum1541/"),
                new Credit("IECDevice", Lang.T("David Hansel"),
                    Lang.T("Libreria per Arduino alla base del firmware RFIEC."),
                    "GPL-3.0", "https://github.com/dhansel/IECDevice")),
            new Section(Lang.T("Strumenti esterni (non inclusi, scaricati dalle fonti ufficiali)"),
                new Credit("VICE", Lang.T("The VICE Team"),
                    Lang.T("Emulatore Commodore utilizzato per avviare programmi e immagini disco."),
                    Lang.T("GPL-2.0 o successiva"), "https://vice-emu.sourceforge.io/"),
                new Credit(Lang.T("Zadig / libwdi"), Lang.T("Pete Batard (Akeo)"),
                    Lang.T("Strumento per l'installazione del driver WinUSB dell'adattatore."),
                    "GPL-3.0", "https://zadig.akeo.ie/"),
                new Credit("AVRDUDE", Lang.T("Progetto AVRDUDE"),
                    Lang.T("Programmatore di riferimento per microcontrollori AVR; l'applicazione ne implementa lo stesso protocollo AVR109."),
                    "GPL-2.0", "https://github.com/avrdudes/avrdude")),
            new Section(Lang.T("Caratteri e colori"),
                new Credit("Sixtyfour", Lang.T("Jens Kutilek"),
                    Lang.T("Carattere ispirato al Commodore 64, predefinito nello schermo della directory."),
                    "OFL-1.1", "https://fonts.google.com/specimen/Sixtyfour"),
                new Credit(Lang.T("Altri caratteri della directory"),
                    Lang.T("Press Start 2P: CodeMan38 · VT323: Peter Hull · Share Tech Mono: Carrois Apostrophe · JetBrains Mono: JetBrains · Fira Code: Nikita Prokopov · IBM Plex Mono: IBM"),
                    Lang.T("Caratteri selezionabili nelle Preferenze, inclusi nell'applicazione (testi delle licenze nella cartella Licenze)."),
                    "OFL-1.1", "https://fonts.google.com/"),
                new Credit("Tokyo Night", "enkia",
                    Lang.T("Tavolozza di colori del tema omonimo."),
                    "MIT", "https://github.com/enkia/tokyo-night-vscode-theme"),
                new Credit(Lang.T("Catppuccin · Everforest · Gruvbox · Nord · Rosé Pine"),
                    Lang.T("Catppuccin · sainnhe · Pavel \"morhetz\" Pertsev · Sven Greb (Arctic Ice Studio) · Rosé Pine"),
                    Lang.T("Tavolozze di colori dei temi omonimi, scelte tra le più usate anche in Omarchy."),
                    "MIT", "https://github.com/catppuccin/catppuccin"),
                new Credit(Lang.T("Lunar Tear UI"), "Raze-Demon",
                    Lang.T("Tema per Blender che ha ispirato i colori del tema \"Lunar Tear\" (nessun file del tema è incluso)."),
                    Lang.T("GPL-3.0 (solo ispirazione)"), "https://extensions.blender.org/themes/lunar-tear-ui/"),
                new Credit(Lang.T("Colodore"), Lang.T("Philip \"Pepto\" Timmermann"),
                    Lang.T("Tavolozza C64 utilizzata per i colori dello schermo della directory."),
                    Lang.T("Tavolozza pubblica"), "https://www.colodore.com/")),
        };

        static readonly Font TitleFont = new Font("Segoe UI Semibold", 10.5f);
        static readonly Font BadgeFont = new Font("Segoe UI Semibold", 8f);
        static readonly Font SectionFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        /// <summary>Sfondo uniforme delle schede: leggermente colorato rispetto alla finestra.</summary>
        static Color CardBack { get { return Theme.Blend(Theme.Surface, Theme.Icon, 0.07f); } }

        public AboutDialog()
        {
            Text = Lang.T("Informazioni su XUMER TOOL");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Theme.S(700), Theme.S(680));
            Icon = Theme.MakeAppIcon();

            // ---------- intestazione ----------
            Panel head = new Panel { Dock = DockStyle.Top, Height = Theme.S(92), BackColor = Theme.Header };
            PictureBox logo = new PictureBox { Size = new Size(Theme.S(60), Theme.S(60)), Location = new Point(Theme.S(20), Theme.S(16)), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.Header };
            logo.Image = Theme.AppLogo();
            head.Controls.Add(logo);
            head.Controls.Add(new Label { Text = "XUMER TOOL", AutoSize = true, Font = new Font("Segoe UI Semibold", 16f), ForeColor = Theme.HeaderText, Location = new Point(Theme.S(94), Theme.S(14)), BackColor = Theme.Header });
            head.Controls.Add(new Label
            {
                Text = Lang.T("Versione ") + AppVersion + Lang.T(" · interfaccia integrata per drive Commodore e adattatore xum1541"),
                AutoSize = true, ForeColor = Theme.HeaderMuted, Location = new Point(Theme.S(96), Theme.S(54)), BackColor = Theme.Header
            });

            // ---------- contenuto ----------
            FlowLayoutPanel list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
                BackColor = Theme.Surface, Padding = new Padding(Theme.S(20), Theme.S(6), Theme.S(10), Theme.S(12))
            };
            int cardW = Theme.S(636);

            list.Controls.Add(SectionLabel(Lang.T("Il progetto")));
            list.Controls.Add(ProjectCard(cardW));

            foreach (Section s in Sections)
            {
                list.Controls.Add(SectionLabel(s.Title));
                foreach (Credit c in s.Items) list.Controls.Add(CreditCard(c, cardW));
            }

            list.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(cardW, 0), ForeColor = Theme.Muted, Margin = new Padding(0, Theme.S(14), 0, Theme.S(4)),
                Text = Lang.T("Commodore, C64, VIC-20, Amiga e i nomi dei prodotti citati sono marchi dei rispettivi proprietari. ") +
                       Lang.T("Il testo della licenza MIT è nel file LICENSE.txt; i testi delle licenze di terze parti sono nella cartella \"Licenze\" della cartella di installazione.")
            });

            // ---------- pulsanti ----------
            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(56), BackColor = Theme.Surface, Padding = new Padding(Theme.S(20), Theme.S(10), Theme.S(20), Theme.S(10)) };
            IconButton close = new IconButton(null, Lang.T("Chiudi"), ButtonKind.Accent) { DialogResult = DialogResult.OK, Dock = DockStyle.Right };
            foot.Controls.Add(close);
            IconButton play = new IconButton(char.ConvertFromUtf32(0xE7FC), Lang.T("Giochi"), ButtonKind.Toolbar) { Dock = DockStyle.Left };
            play.Click += delegate { OpenGames = true; Close(); };
            foot.Controls.Add(play);
            Panel footLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };

            if (Theme.Dark)
                list.HandleCreated += delegate { try { SetWindowTheme(list.Handle, "DarkMode_Explorer", null); } catch { } };

            Controls.Add(list);
            Controls.Add(footLine);
            Controls.Add(foot);
            Controls.Add(head);
            AcceptButton = close; CancelButton = close;
        }

        static Label SectionLabel(string text)
        {
            return new Label
            {
                Text = text.ToUpperInvariant(), AutoSize = true, ForeColor = Theme.Icon, Font = SectionFont,
                Margin = new Padding(0, Theme.S(14), 0, Theme.S(6))
            };
        }

        static TableLayoutPanel NewCard(int width)
        {
            TableLayoutPanel card = new TableLayoutPanel
            {
                ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(width, 0), MaximumSize = new Size(width, 0),
                BackColor = CardBack, Margin = new Padding(0, 0, 0, Theme.S(6)),
                Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(14), Theme.S(10))
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return card;
        }

        static Label Text_(string text, Color color, int width, Font font)
        {
            Label l = new Label { Text = text, AutoSize = true, MaximumSize = new Size(width, 0), ForeColor = color, Margin = new Padding(0, Theme.S(2), 0, 0), UseMnemonic = false };
            if (font != null) l.Font = font;
            return l;
        }

        /// <summary>La scheda dell'autore: cos'è XUMER TOOL e chi l'ha realizzato.</summary>
        Control ProjectCard(int width)
        {
            TableLayoutPanel card = NewCard(width);
            int inner = width - Theme.S(28);
            card.Controls.Add(Text_(Lang.T("XUMER TOOL · darkwave3D"), Theme.Text, inner, TitleFont), 0, 0);
            card.Controls.Add(new Badge("MIT"), 1, 0);
            Label body = Text_(
                Lang.T("XUMER TOOL è un'interfaccia grafica integrata. Riunisce in un'unica applicazione gli strumenti, i protocolli ") +
                Lang.T("e le risorse sviluppati dai progetti elencati di seguito, per renderli utilizzabili insieme in modo semplice e coerente.\n\n") +
                Lang.T("L'applicazione non sostituisce tali progetti né ne rivendica la paternità: il merito del lavoro originale appartiene ") +
                Lang.T("ai rispettivi autori, nel rispetto delle relative licenze.\n\n") +
                Lang.T("Ideazione, progettazione dell'interfaccia e collaudo sull'hardware reale (adattatore xum1541 su Arduino Pro Micro e ") +
                Lang.T("Commodore 1571): darkwave3D. Sviluppo del codice con il supporto di Claude (Anthropic).\n\n") +
                Lang.T("XUMER TOOL è distribuito con licenza MIT. I componenti di terze parti inclusi (routine 6502 di OpenCBM e caratteri) ") +
                Lang.T("mantengono le rispettive licenze, riportate qui sotto e nella cartella \"Licenze\"."),
                Theme.Muted, inner, null);
            body.Margin = new Padding(0, Theme.S(6), 0, 0);
            card.Controls.Add(body, 0, 1);
            card.SetColumnSpan(body, 2);
            return card;
        }

        Control CreditCard(Credit c, int width)
        {
            TableLayoutPanel card = NewCard(width);
            int inner = width - Theme.S(28);
            card.Controls.Add(Text_(c.Name, Theme.Text, inner - Theme.S(180), TitleFont), 0, 0);
            card.Controls.Add(new Badge(c.License), 1, 0);
            int row = 1;
            Label authors = Text_(c.Authors, Theme.Icon, inner, null);
            card.Controls.Add(authors, 0, row); card.SetColumnSpan(authors, 2); row++;
            Label role = Text_(c.Role, Theme.Muted, inner, null);
            role.Margin = new Padding(0, Theme.S(4), 0, 0);
            card.Controls.Add(role, 0, row); card.SetColumnSpan(role, 2); row++;
            if (c.Url != null)
            {
                LinkLabel link = new LinkLabel
                {
                    Text = c.Url, AutoSize = true, Margin = new Padding(0, Theme.S(4), 0, 0),
                    LinkColor = Theme.Accent, ActiveLinkColor = Theme.AccentHover, VisitedLinkColor = Theme.Accent, LinkBehavior = LinkBehavior.HoverUnderline
                };
                string url = c.Url;
                link.LinkClicked += delegate
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show(this, Lang.T("Non riesco ad aprire il link: ") + ex.Message, Text); }
                };
                card.Controls.Add(link, 0, row); card.SetColumnSpan(link, 2);
            }
            return card;
        }

        /// <summary>Etichetta arrotondata con il tipo di licenza.</summary>
        class Badge : Control
        {
            public Badge(string text)
            {
                Text = text;
                Font = BadgeFont;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Size sz = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
                Size = new Size(sz.Width + Theme.S(18), sz.Height + Theme.S(8));
                Margin = new Padding(Theme.S(8), 0, 0, 0);
                Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
                using (GraphicsPath p = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
                {
                    using (SolidBrush b = new SolidBrush(Theme.Blend(Theme.Surface, Theme.Icon, 0.16f))) g.FillPath(b, p);
                    using (Pen pen = new Pen(Theme.Blend(Theme.Surface, Theme.Icon, 0.45f))) g.DrawPath(pen, p);
                }
                TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), Theme.Icon,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
