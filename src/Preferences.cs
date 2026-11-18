// Finestra "Preferenze": tema, carattere della directory, salvaschermo e avvio.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XumerTool
{
    class PreferencesDialog : Form
    {
        // risultati
        public string ThemeName;
        public string LangCode;
        RadioButton[] langButtons;
        public string FontId;
        public bool SaverOn, SaverFull, AutoConnect, TrySaver;

        readonly ThemePicker picker;
        readonly Control themePreview, themeChips;
        readonly ListBox fonts;
        readonly Panel preview;
        readonly CheckBox chkSaver, chkAuto;
        readonly RadioButton rbDir, rbFull;

        static readonly Font NameFont = new Font("Segoe UI Semibold", 10f);
        static readonly Font NoteFont = new Font("Segoe UI", 8.5f);
        static readonly Font GroupFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);

        // ================= miniatura dell'app nei colori di un tema =================

        static void Fill(Graphics g, Color c, float x, float y, float w, float h)
        {
            using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, x, y, Math.Max(1f, w), Math.Max(1f, h));
        }

        static void Pill(Graphics g, Color c, float x, float y, float w, float h)
        {
            Rectangle r = Rectangle.Round(new RectangleF(x, y, Math.Max(2f, w), Math.Max(2f, h)));
            using (GraphicsPath p = Theme.Rounded(r, Math.Max(1, Math.Min(r.Height / 2, Theme.S(4)))))
            using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
        }

        /// <summary>Disegna la finestra principale in miniatura: intestazione, barra a sezioni, schermo C64, schede e registro.</summary>
        static void PaintMiniature(Graphics g, Rectangle r, Palette p, bool detailed)
        {
            SmoothingMode oldMode = g.SmoothingMode;
            Region oldClip = g.Clip;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = Theme.Rounded(r, detailed ? Theme.S(6) : Theme.S(3)))
            {
                g.SetClip(shape, CombineMode.Intersect);
                float W = r.Width, Hh = r.Height, X = r.X, Y = r.Y;
                float pad = Math.Max(2f, W * 0.035f);
                Fill(g, p.Window, X, Y, W, Hh);

                // intestazione
                float hh = Hh * 0.15f;
                Fill(g, p.Header, X, Y, W, hh);
                if (detailed)
                {
                    Pill(g, p.HeaderText, X + pad, Y + hh * 0.26f, W * 0.17f, hh * 0.22f);
                    Pill(g, p.HeaderMuted, X + pad, Y + hh * 0.60f, W * 0.23f, hh * 0.13f);
                    Pill(g, p.HeaderButton, X + W * 0.31f, Y + hh * 0.24f, W * 0.15f, hh * 0.52f);
                    Pill(g, p.Accent, X + W * 0.31f, Y + hh * 0.24f, W * 0.04f, hh * 0.52f);
                    Pill(g, p.Accent, X + W * 0.48f, Y + hh * 0.24f, W * 0.11f, hh * 0.52f);
                    Pill(g, p.HeaderButton, X + W * 0.61f, Y + hh * 0.24f, W * 0.09f, hh * 0.52f);
                    float d = hh * 0.26f;
                    using (SolidBrush b = new SolidBrush(p.Ok)) g.FillEllipse(b, X + W * 0.79f, Y + (hh - d) / 2, d, d);
                    Pill(g, p.HeaderText, X + W * 0.83f, Y + hh * 0.36f, W * 0.12f, hh * 0.2f);
                }
                else Pill(g, p.Accent, X + W * 0.62f, Y + hh * 0.25f, W * 0.18f, hh * 0.5f);

                // barra strumenti a sezioni
                float ty = Y + hh, th = Hh * 0.17f;
                Fill(g, p.Surface, X, ty, W, th);
                Fill(g, p.Border, X, ty + th, W, 1);
                Color[] sec = { p.SectionDisk, p.SectionFile, p.SectionImage };
                float[] gw = { 0.27f, 0.34f, 0.20f };
                int[] nb = { 3, 4, 2 };
                float gx = X + pad;
                for (int i = 0; i < 3; i++)
                {
                    float w = W * gw[i], gy = ty + th * 0.15f, gh = th * 0.70f;
                    Fill(g, Theme.Blend(sec[i], p.Surface, 0.88f), gx, gy, w, gh);
                    Fill(g, sec[i], gx, gy, w, gh * (detailed ? 0.28f : 0.45f));
                    if (detailed)
                    {
                        float by = gy + gh * 0.28f, bh = gh * 0.72f, slot = w / nb[i];
                        for (int k = 0; k < nb[i]; k++)
                        {
                            float sx = gx + k * slot + slot * 0.12f, s = bh * 0.32f;
                            Fill(g, sec[i], sx, by + (bh - s) / 2, s, s);
                            Fill(g, Theme.Blend(p.Text, p.Surface, 0.45f), sx + s * 1.6f, by + bh * 0.44f, slot * 0.42f, Math.Max(1f, bh * 0.12f));
                        }
                    }
                    gx += w + W * 0.02f;
                }

                // schermo C64
                float fy = Y + Hh * 0.36f, fh = Hh * 0.27f;
                Fill(g, p.C64Frame, X + pad, fy, W - 2 * pad, fh);
                float sx0 = X + pad + W * 0.045f, sy0 = fy + fh * 0.14f, sw = W - 2 * pad - W * 0.09f, sh = fh * 0.72f;
                Fill(g, p.C64Back, sx0, sy0, sw, sh);
                float lh = sh * (detailed ? 0.12f : 0.18f);
                Fill(g, p.C64Text, sx0 + sw * 0.04f, sy0 + sh * 0.12f, sw * 0.46f, lh);
                float[] lw = detailed ? new[] { 0.36f, 0.42f, 0.30f, 0.24f } : new[] { 0.36f, 0.28f };
                for (int i = 0; i < lw.Length; i++)
                    Fill(g, p.C64Text, sx0 + sw * 0.04f, sy0 + sh * 0.12f + (i + 1) * lh * 1.7f, sw * lw[i], lh * 0.7f);

                // console: schede e registro
                float cy = Y + Hh * 0.67f, chh = Hh * 0.29f;
                Fill(g, p.LogBack, X + pad, cy, W - 2 * pad, chh);
                Color[] tabs = { p.TabAdapter, p.TabLog, p.TabDos, p.TabVice, p.TabArchive, p.TabGames };
                float tabH = chh * 0.22f, tx = X + pad + W * 0.02f;
                for (int i = 0; i < tabs.Length; i++)
                {
                    float tw = W * (detailed ? 0.12f : 0.13f);
                    Fill(g, i == 1 ? tabs[i] : Theme.Blend(tabs[i], p.LogBack, 0.78f), tx, cy + chh * 0.06f, tw, tabH);
                    if (detailed && i != 1) Fill(g, tabs[i], tx + tw * 0.3f, cy + chh * 0.06f + tabH * 0.42f, tw * 0.5f, Math.Max(1f, tabH * 0.18f));
                    tx += tw + W * 0.01f;
                }
                Fill(g, p.TabLog, X + pad, cy + chh * 0.06f + tabH, W - 2 * pad, Math.Max(1f, chh * 0.03f));
                if (detailed)
                {
                    Color[] lc = { p.LogText, p.LogSent, p.LogReply, p.LogError };
                    float[] lww = { 0.40f, 0.28f, 0.34f, 0.46f };
                    float ly = cy + chh * 0.42f, lstep = chh * 0.13f;
                    for (int i = 0; i < 4; i++)
                    {
                        Fill(g, p.LogTime, X + pad + W * 0.03f, ly + i * lstep, W * 0.07f, lstep * 0.45f);
                        Fill(g, lc[i], X + pad + W * 0.12f, ly + i * lstep, W * lww[i], lstep * 0.45f);
                    }
                }
                else
                {
                    Fill(g, p.LogText, X + pad + W * 0.04f, cy + chh * 0.5f, W * 0.4f, Math.Max(1f, chh * 0.1f));
                    Fill(g, p.LogReply, X + pad + W * 0.04f, cy + chh * 0.72f, W * 0.3f, Math.Max(1f, chh * 0.1f));
                }

                // barra di stato
                Fill(g, p.Surface, X, Y + Hh * 0.975f, W, Hh * 0.03f);
                g.Clip = oldClip;
                using (Pen pen = new Pen(Theme.Blend(Theme.Border, Theme.Text, 0.15f))) g.DrawPath(pen, shape);
            }
            g.SmoothingMode = oldMode;
        }

        // ================= menu a tendina dei temi =================

        /// <summary>Elenco dei temi mostrato nella tendina: gruppi "chiari" e "scuri", miniatura, nome e descrizione.</summary>
        class ThemeList : Control
        {
            public event Action<string> Picked;
            readonly List<object> rows = new List<object>();   // string = intestazione del gruppo, Palette = tema
            readonly string selected;
            int hover = -1;

            static int RowHeight(object o) { return o is string ? Theme.S(28) : Theme.S(58); }

            public ThemeList(int width, string selected)
            {
                this.selected = selected;
                bool? group = null;
                foreach (string n in Theme.Names)
                {
                    Palette p = Theme.Get(n);
                    if (group != p.Dark) { rows.Add(p.Dark ? Lang.T("TEMI SCURI") : Lang.T("TEMI CHIARI")); group = p.Dark; }
                    rows.Add(p);
                }
                int h = Theme.S(4) * 2;
                foreach (object o in rows) h += RowHeight(o);
                Size = new Size(width, h);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Cursor = Cursors.Hand;
            }

            int RowAt(int y)
            {
                int top = Theme.S(4);
                for (int i = 0; i < rows.Count; i++)
                {
                    int h = RowHeight(rows[i]);
                    if (y >= top && y < top + h) return rows[i] is Palette ? i : -1;
                    top += h;
                }
                return -1;
            }

            protected override void OnMouseMove(MouseEventArgs e) { int i = RowAt(e.Y); if (i != hover) { hover = i; Invalidate(); } base.OnMouseMove(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                int i = RowAt(e.Y);
                if (i >= 0 && Picked != null) Picked(((Palette)rows[i]).Name);
                base.OnMouseClick(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Theme.Surface);
                int top = Theme.S(4);
                for (int i = 0; i < rows.Count; i++)
                {
                    int h = RowHeight(rows[i]);
                    Rectangle row = new Rectangle(0, top, Width, h);
                    string caption = rows[i] as string;
                    if (caption != null)
                    {
                        if (i > 0) Fill(g, Theme.Border, Theme.S(12), top + Theme.S(6), Width - Theme.S(24), 1);
                        TextRenderer.DrawText(g, caption, GroupFont, new Rectangle(Theme.S(14), top + Theme.S(8), Width, h - Theme.S(8)), Theme.Muted,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    }
                    else
                    {
                        Palette p = (Palette)rows[i];
                        bool sel = p.Name == selected;
                        if (i == hover || sel)
                        {
                            Rectangle hl = new Rectangle(Theme.S(4), top + Theme.S(1), Width - Theme.S(8), h - Theme.S(2));
                            using (GraphicsPath gp = Theme.Rounded(hl, Theme.S(5)))
                            using (SolidBrush b = new SolidBrush(Theme.Blend(Theme.Surface, Theme.Accent, i == hover ? 0.16f : 0.09f)))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.FillPath(b, gp);
                            }
                        }
                        PaintThemeRow(g, row, p, sel);
                    }
                    top += h;
                }
            }
        }

        /// <summary>Una riga tema: miniatura, nome, descrizione e segno di spunta se selezionato.</summary>
        static void PaintThemeRow(Graphics g, Rectangle row, Palette p, bool check)
        {
            Rectangle mini = new Rectangle(row.X + Theme.S(12), row.Y + (row.Height - Theme.S(40)) / 2, Theme.S(64), Theme.S(40));
            PaintMiniature(g, mini, p, false);
            int tx = mini.Right + Theme.S(12);
            int right = row.Right - Theme.S(check ? 36 : 12);
            TextRenderer.DrawText(g, Lang.T(p.Name), NameFont, new Rectangle(tx, row.Y + row.Height / 2 - Theme.S(20), right - tx, Theme.S(22)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, p.Note, NoteFont, new Rectangle(tx, row.Y + row.Height / 2 + Theme.S(2), right - tx, Theme.S(18)), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            if (check && Theme.IconFont != null)
                TextRenderer.DrawText(g, char.ConvertFromUtf32(0xE73E), Theme.IconFont, new Rectangle(row.Right - Theme.S(34), row.Y, Theme.S(22), row.Height), Theme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        /// <summary>Casella a tendina per scegliere il tema: mostra il tema scelto con la sua miniatura.</summary>
        class ThemePicker : Control
        {
            public event EventHandler SelectedChanged;
            string selected;
            bool hover, open;
            int ignoreClickUntil;

            public string Selected
            {
                get { return selected; }
                set { if (selected == value) return; selected = value; Invalidate(); if (SelectedChanged != null) SelectedChanged(this, EventArgs.Empty); }
            }

            public ThemePicker()
            {
                Size = new Size(Theme.S(360), Theme.S(60));
                Cursor = Cursors.Hand;
                TabStop = true;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
            protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                Focus();
                if (e.Button == MouseButtons.Left && !open && Environment.TickCount - ignoreClickUntil > 0) ShowList();
                base.OnMouseDown(e);
            }

            protected override bool IsInputKey(Keys k)
            {
                return k == Keys.Up || k == Keys.Down || base.IsInputKey(k);
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                int i = Array.IndexOf(Theme.Names, selected);
                if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down) || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { ShowList(); e.Handled = true; }
                else if (e.KeyCode == Keys.Down) { Selected = Theme.Names[Math.Min(Theme.Names.Length - 1, i + 1)]; e.Handled = true; }
                else if (e.KeyCode == Keys.Up) { Selected = Theme.Names[Math.Max(0, i - 1)]; e.Handled = true; }
                base.OnKeyDown(e);
            }

            void ShowList()
            {
                ThemeList list = new ThemeList(Width, selected);
                ToolStripDropDown drop = new ToolStripDropDown { Padding = Padding.Empty, Renderer = new ThemedRenderer(), DropShadowEnabled = true };
                ToolStripControlHost host = new ToolStripControlHost(list) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = list.Size };
                drop.Items.Add(host);
                list.Picked += name => { drop.Close(); Selected = name; };
                drop.Closed += (s, e) =>
                {
                    open = false; Invalidate();
                    // il clic sulla casella che chiude la tendina non deve riaprirla subito
                    if (e.CloseReason == ToolStripDropDownCloseReason.AppClicked) ignoreClickUntil = Environment.TickCount + 250;
                    BeginInvoke((MethodInvoker)delegate { drop.Dispose(); });
                };
                open = true; Invalidate();
                drop.Show(this, new Point(0, Height + Theme.S(3)));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath p = Theme.Rounded(r, Theme.S(6)))
                {
                    using (SolidBrush b = new SolidBrush(hover && !open ? Theme.Blend(Theme.FieldBack, Theme.Accent, 0.05f) : Theme.FieldBack)) g.FillPath(b, p);
                    Color border = open || Focused ? Theme.Accent : hover ? Theme.Blend(Theme.Border, Theme.Accent, 0.5f) : Theme.Blend(Theme.Border, Theme.Text, 0.12f);
                    using (Pen pen = new Pen(border, open || Focused ? Theme.S(2) : 1)) g.DrawPath(pen, p);
                }
                Palette pal = Theme.Get(selected);
                PaintThemeRow(g, new Rectangle(0, 0, Width - Theme.S(30), Height), pal, false);
                if (Theme.IconFont != null)
                    TextRenderer.DrawText(g, char.ConvertFromUtf32(open ? 0xE70E : 0xE70D), Theme.IconFont, new Rectangle(Width - Theme.S(36), 0, Theme.S(24), Height), Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        /// <summary>Modalità screenshot: salva la tendina dei temi come PNG.</summary>
        public static void ShotThemeList(string file)
        {
            using (ThemeList list = new ThemeList(Theme.S(360), Theme.Current))
            using (Bitmap bmp = new Bitmap(list.Width, list.Height))
            {
                list.DrawToBitmap(bmp, new Rectangle(Point.Empty, list.Size));
                bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        /// <summary>Pannello senza sfarfallio per le anteprime.</summary>
        class PreviewBox : Control
        {
            public PreviewBox() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
        }

        static Label Section(string text)
        {
            return new Label
            {
                Text = text.ToUpperInvariant(), AutoSize = true, ForeColor = Theme.Icon,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(0, Theme.S(16), 0, Theme.S(8))
            };
        }

        public PreferencesDialog(bool busy)
        {
            Text = Lang.T("Preferenze");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Theme.S(780), Theme.S(930));
            Icon = Theme.MakeAppIcon();

            ThemeName = Theme.Current;
            LangCode = Settings.Get("lang", Lang.Current);   // quella scelta, anche se non ancora applicata
            FontId = ScreenFonts.Current.Id;
            SaverOn = Settings.Get("saver", "1") == "1";
            SaverFull = Settings.Get("savermode", "dir") == "full";
            AutoConnect = Settings.Get("autoconnect", "1") == "1";

            Panel head = new Panel { Dock = DockStyle.Top, Height = Theme.S(64), BackColor = Theme.Header };
            head.Controls.Add(new Label { Text = Lang.T("Preferenze"), AutoSize = true, Font = new Font("Segoe UI Semibold", 15f), ForeColor = Theme.HeaderText, Location = new Point(Theme.S(20), Theme.S(14)) });

            FlowLayoutPanel body = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
                BackColor = Theme.Surface, Padding = new Padding(Theme.S(20), 0, Theme.S(16), Theme.S(10))
            };

            // ---------- Aspetto ----------
            body.Controls.Add(Section(Lang.T("Aspetto · tema")));
            TableLayoutPanel themeRow = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = new Padding(0), BackColor = Theme.Surface };
            FlowLayoutPanel themeLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, Theme.S(14), 0), BackColor = Theme.Surface };
            picker = new ThemePicker { Margin = new Padding(0) };
            picker.Selected = ThemeName;
            themeLeft.Controls.Add(picker);
            themeLeft.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(Theme.S(360), 0), ForeColor = Theme.Muted, Margin = new Padding(0, Theme.S(12), 0, Theme.S(8)),
                Text = Lang.T("Apri il menu per vedere tutti i temi, oppure usa le frecce ↑ ↓. Il tema viene applicato quando premi Salva.")
            });
            themeChips = new PreviewBox { Size = new Size(Theme.S(360), Theme.S(24)), Margin = new Padding(0) };
            themeChips.Paint += PaintChips;
            themeLeft.Controls.Add(themeChips);
            themeRow.Controls.Add(themeLeft, 0, 0);
            themePreview = new PreviewBox { Size = new Size(Theme.S(350), Theme.S(168)), Margin = new Padding(0) };
            themePreview.Paint += (s, e) => PaintMiniature(e.Graphics, new Rectangle(0, 0, themePreview.Width - 1, themePreview.Height - 1), Theme.Get(ThemeName), true);
            themeRow.Controls.Add(themePreview, 1, 0);
            picker.SelectedChanged += delegate { ThemeName = picker.Selected; themePreview.Invalidate(); themeChips.Invalidate(); };
            body.Controls.Add(themeRow);

            // ---------- Carattere ----------
            body.Controls.Add(Section(Lang.T("Carattere della directory")));
            TableLayoutPanel fontRow = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = new Padding(0), BackColor = Theme.Surface };
            fonts = new ListBox
            {
                DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = Theme.S(46), Width = Theme.S(360), Height = Theme.S(46) * 5 + 4,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface, ForeColor = Theme.Text, IntegralHeight = false, Margin = new Padding(0, 0, Theme.S(14), 0)
            };
            foreach (ScreenFontInfo f in ScreenFonts.All) if (ScreenFonts.Available(f)) fonts.Items.Add(f);
            fonts.DrawItem += DrawFontItem;
            fonts.SelectedIndexChanged += delegate
            {
                ScreenFontInfo f = fonts.SelectedItem as ScreenFontInfo;
                if (f != null) { FontId = f.Id; preview.Invalidate(); }
            };
            fontRow.Controls.Add(fonts, 0, 0);
            preview = new Panel { Width = Theme.S(350), Height = fonts.Height, BackColor = Theme.C64Frame, Margin = new Padding(0) };
            preview.Paint += PaintPreview;
            fontRow.Controls.Add(preview, 1, 0);
            body.Controls.Add(fontRow);
            for (int i = 0; i < fonts.Items.Count; i++)
                if (((ScreenFontInfo)fonts.Items[i]).Id == FontId) fonts.SelectedIndex = i;

            // ---------- Salvaschermo ----------
            body.Controls.Add(Section(Lang.T("Salvaschermo PETSCII")));
            chkSaver = new CheckBox { Text = Lang.T("Attiva dopo 5 minuti senza usare tastiera e mouse"), Checked = SaverOn, AutoSize = true, Margin = new Padding(0, 0, 0, Theme.S(4)) };
            body.Controls.Add(chkSaver);
            rbDir = new RadioButton { Text = Lang.T("Nella finestra della directory (non copre le altre finestre)"), Checked = !SaverFull, AutoSize = true, Margin = new Padding(Theme.S(20), 0, 0, 0) };
            rbFull = new RadioButton { Text = Lang.T("A tutto schermo (solo quando l'app è in primo piano)"), Checked = SaverFull, AutoSize = true, Margin = new Padding(Theme.S(20), 0, 0, Theme.S(4)) };
            body.Controls.Add(rbDir);
            body.Controls.Add(rbFull);
            chkSaver.CheckedChanged += delegate { rbDir.Enabled = rbFull.Enabled = chkSaver.Checked; };
            rbDir.Enabled = rbFull.Enabled = chkSaver.Checked;
            IconButton tryIt = new IconButton(char.ConvertFromUtf32(0xE768), Lang.T("Prova ora"), ButtonKind.Toolbar) { Margin = new Padding(Theme.S(16), Theme.S(4), 0, 0) };
            tryIt.Click += delegate { Collect(); TrySaver = true; DialogResult = DialogResult.OK; Close(); };
            body.Controls.Add(tryIt);

            // ---------- Avvio ----------
            body.Controls.Add(Section(Lang.T("Avvio")));
            chkAuto = new CheckBox { Text = Lang.T("Collega automaticamente l'adattatore all'avvio"), Checked = AutoConnect, AutoSize = true };
            body.Controls.Add(chkAuto);

            // ---------- Lingua ----------
            body.Controls.Add(Section(Lang.Current == "it" ? "Lingua · Language" : "Language · Lingua"));
            FlowLayoutPanel langRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Theme.Surface };
            langButtons = new RadioButton[Lang.Codes.Length];
            for (int i = 0; i < Lang.Codes.Length; i++)
            {
                langButtons[i] = new RadioButton { Text = Lang.Names[i], AutoSize = true, Checked = Lang.Codes[i] == LangCode, Margin = new Padding(0, 0, Theme.S(24), 0) };
                langRow.Controls.Add(langButtons[i]);
            }
            body.Controls.Add(langRow);
            body.Controls.Add(new Label
            {
                AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, Theme.S(6), 0, 0),
                Text = Lang.T("La nuova lingua si applica riavviando il programma.")
            });

            if (busy)
                body.Controls.Add(new Label
                {
                    AutoSize = true, ForeColor = Theme.Danger, Margin = new Padding(0, Theme.S(14), 0, 0),
                    Text = Lang.T("C'è un'operazione in corso: tema e carattere verranno applicati quando sarà finita.")
                });

            // ---------- pulsanti ----------
            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(58), BackColor = Theme.Surface, Padding = new Padding(Theme.S(20), Theme.S(11), Theme.S(20), Theme.S(11)) };
            IconButton save = new IconButton(null, Lang.T("Salva"), ButtonKind.Accent) { Dock = DockStyle.Right };
            save.Click += delegate { Collect(); DialogResult = DialogResult.OK; Close(); };
            IconButton cancel = new IconButton(null, Lang.T("Annulla"), ButtonKind.Toolbar) { Dock = DockStyle.Right, DialogResult = DialogResult.Cancel };
            foot.Controls.Add(cancel);
            foot.Controls.Add(save);
            Panel footLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };

            Controls.Add(body);
            Controls.Add(footLine);
            Controls.Add(foot);
            Controls.Add(head);
            CancelButton = cancel;
        }

        void Collect()
        {
            SaverOn = chkSaver.Checked;
            SaverFull = rbFull.Checked;
            AutoConnect = chkAuto.Checked;
            for (int i = 0; i < langButtons.Length; i++) if (langButtons[i].Checked) LangCode = Lang.Codes[i];
        }

        /// <summary>Tavolozza del tema scelto: pallini dei colori principali.</summary>
        void PaintChips(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Palette p = Theme.Get(ThemeName);
            Color[] c = { p.Header, p.Window, p.Surface, p.Accent, p.SectionDisk, p.SectionFile, p.SectionImage, p.C64Back, p.C64Text, p.Ok, p.Danger };
            int d = Theme.S(18), x = 0, y = (themeChips.Height - d) / 2;
            using (Pen pen = new Pen(Theme.Blend(Theme.Border, Theme.Text, 0.15f)))
                foreach (Color k in c)
                {
                    using (SolidBrush b = new SolidBrush(k)) g.FillEllipse(b, x, y, d, d);
                    g.DrawEllipse(pen, x, y, d, d);
                    x += d + Theme.S(6);
                }
            TextRenderer.DrawText(g, p.Dark ? Lang.T("tema scuro") : Lang.T("tema chiaro"), NoteFont, new Rectangle(x + Theme.S(4), 0, themeChips.Width - x - Theme.S(4), themeChips.Height), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        void DrawFontItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            ScreenFontInfo f = (ScreenFontInfo)fonts.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush b = new SolidBrush(sel ? Theme.Blend(Theme.Surface, Theme.Accent, 0.18f) : Theme.Surface))
                e.Graphics.FillRectangle(b, e.Bounds);
            if (sel)
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                    e.Graphics.FillRectangle(b, e.Bounds.X, e.Bounds.Y, Theme.S(4), e.Bounds.Height);
            using (Font ff = ScreenFonts.Create(f))
                ScreenFonts.Draw(e.Graphics, f.Name, ff, Theme.Text, new Rectangle(e.Bounds.X + Theme.S(12), e.Bounds.Y + Theme.S(3), e.Bounds.Width - Theme.S(14), Theme.S(24)));
            TextRenderer.DrawText(e.Graphics, f.Style, NoteFont, new Rectangle(e.Bounds.X + Theme.S(12), e.Bounds.Y + Theme.S(26), e.Bounds.Width - Theme.S(14), Theme.S(18)), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        void PaintPreview(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.C64Frame);
            Rectangle scr = new Rectangle(Theme.S(14), Theme.S(12), preview.Width - Theme.S(28), preview.Height - Theme.S(24));
            using (SolidBrush b = new SolidBrush(Theme.C64Back)) g.FillRectangle(b, scr);
            ScreenFontInfo info = fonts.SelectedItem as ScreenFontInfo;
            if (info == null) return;
            string[] lines = { Lang.T("0 \"GIOCHI 1        \" 01 2A"), Lang.T("34   \"MENU\"           PRG"), Lang.T("129  \"AVVENTURA\"      PRG"), Lang.T("12   \"PUNTEGGI\"       SEQ"), Lang.T("412 BLOCKS FREE."), "READY." };
            using (Font f = ScreenFonts.Create(info))
            {
                int lh = f.Height + Theme.S(info.Spacing);
                int y = scr.Y + Theme.S(8);
                for (int i = 0; i < lines.Length; i++)
                {
                    SizeF sz = ScreenFonts.Measure(g, lines[i], f);
                    if (i == 0)
                        using (SolidBrush b = new SolidBrush(Theme.C64Text)) g.FillRectangle(b, scr.X + Theme.S(8), y - 1, Math.Min((int)sz.Width, scr.Width - Theme.S(16)), lh - 2);
                    g.SetClip(scr);
                    ScreenFonts.Draw(g, lines[i], f, i == 0 ? Theme.C64Back : Theme.C64Text, new Rectangle(scr.X + Theme.S(8), y, scr.Width - Theme.S(16), lh));
                    g.ResetClip();
                    y += lh;
                }
            }
        }
    }
}
