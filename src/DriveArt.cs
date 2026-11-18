// Elementi grafici "d'atmosfera": il piccolo drive con i LED nell'intestazione, la mappa del disco
// che si colora durante le copie, le icone pixel dei tipi di file. Disegni originali (nessun logo).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XumerTool
{
    /// <summary>
    /// Piccolo drive da 5,25" disegnato nell'intestazione: LED verde di alimentazione (collegato),
    /// LED rosso che sfarfalla durante il lavoro e lampeggia dopo un errore, come sul drive vero.
    /// </summary>
    class DriveLed : Control
    {
        bool powered, busy;
        int errorTicks;            // lampeggi d'errore rimasti
        bool redOn;
        readonly Random rnd = new Random();
        readonly Timer timer = new Timer { Interval = 90 };

        public bool Powered { get { return powered; } set { if (powered != value) { powered = value; Invalidate(); } } }

        public bool Busy
        {
            get { return busy; }
            set
            {
                if (busy == value) return;
                busy = value;
                if (busy) errorTicks = 0;
                UpdateTimer();
                Invalidate();
            }
        }

        /// <summary>Il LED rosso lampeggia per qualche secondo (errore del drive).</summary>
        public void ErrorBlink()
        {
            errorTicks = 36;   // circa 3 secondi a 90 ms (6 lampeggi)
            UpdateTimer();
        }

        public DriveLed()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(Theme.S(46), Theme.S(28));
            timer.Tick += delegate
            {
                if (busy) redOn = rnd.Next(100) < 72;            // attività: acceso quasi sempre, con brevi spegnimenti
                else if (errorTicks > 0) { errorTicks--; redOn = (errorTicks / 3) % 2 == 0; }
                else redOn = false;
                if (!busy && errorTicks == 0) timer.Stop();
                Invalidate();
            };
        }

        void UpdateTimer()
        {
            if ((busy || errorTicks > 0) && !DesignMode) timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Header);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool red = (busy || errorTicks > 0) && redOn;
            PaintDrive(g, new Rectangle(0, 0, Width, Height), powered, red);
        }

        /// <summary>Disegna il drive nel rettangolo indicato (usato anche per le anteprime).</summary>
        public static void PaintDrive(Graphics g, Rectangle r, bool powered, bool red)
        {
            float s = r.Height / 30f;
            // proporzioni del frontale: largo circa il doppio dell'altezza
            float w = Math.Min(r.Width, r.Height * 1.8f), h = r.Height - 3 * s;
            RectangleF body = new RectangleF(r.X + (r.Width - w) / 2, r.Y + 1 * s, w, h);

            Color beige = Color.FromArgb(0xDD, 0xD3, 0xBC), beigeDark = Color.FromArgb(0xB9, 0xAE, 0x93);
            Color edge = Color.FromArgb(0x6E, 0x66, 0x52), slot = Color.FromArgb(0x2A, 0x26, 0x20);

            // ombra
            using (GraphicsPath p = RoundRect(new RectangleF(body.X + 1.5f * s, body.Y + 2 * s, body.Width, body.Height), 3 * s))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillPath(b, p);
            // corpo con una leggera sfumatura e la fascia inferiore più scura
            using (GraphicsPath p = RoundRect(body, 3 * s))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(body, beige, beigeDark, 90f)) g.FillPath(b, p);
                using (Pen pen = new Pen(edge, Math.Max(1f, s))) g.DrawPath(pen, p);
            }
            float bandY = body.Y + body.Height * 0.68f;
            using (Pen pen = new Pen(Color.FromArgb(120, edge), Math.Max(1f, s * 0.8f)))
                g.DrawLine(pen, body.X + 2 * s, bandY, body.Right - 2 * s, bandY);

            // feritoia per il dischetto e leva di chiusura al centro
            float slotY = body.Y + body.Height * 0.36f;
            RectangleF sl = new RectangleF(body.X + body.Width * 0.12f, slotY - 1.6f * s, body.Width * 0.62f, 3.2f * s);
            using (GraphicsPath p = RoundRect(sl, 1.6f * s))
            using (SolidBrush b = new SolidBrush(slot)) g.FillPath(b, p);
            float cx = sl.X + sl.Width / 2;
            RectangleF hub = new RectangleF(cx - 3.2f * s, slotY - 3.2f * s, 6.4f * s, 6.4f * s);
            using (SolidBrush b = new SolidBrush(beigeDark)) g.FillEllipse(b, hub);
            using (Pen pen = new Pen(edge, Math.Max(1f, s * 0.8f))) g.DrawEllipse(pen, hub);
            RectangleF lever = new RectangleF(cx - 7.5f * s, slotY - 1.1f * s, 15f * s, 2.2f * s);
            using (GraphicsPath p = RoundRect(lever, 1.1f * s))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(0x4A, 0x44, 0x38))) g.FillPath(b, p);

            // LED: verde (alimentazione) e rosso (attività), in basso a destra
            float ledR = 2.4f * s, ledY = bandY + (body.Bottom - bandY) / 2;
            float x2 = body.Right - 6 * s, x1 = x2 - 7 * s;
            Led(g, x1, ledY, ledR, powered ? Color.FromArgb(0x3B, 0xE0, 0x5A) : Color.FromArgb(0x3A, 0x4A, 0x3C), powered);
            Led(g, x2, ledY, ledR, red ? Color.FromArgb(0xFF, 0x3B, 0x30) : Color.FromArgb(0x55, 0x30, 0x2C), red);
        }

        static void Led(Graphics g, float cx, float cy, float r, Color c, bool lit)
        {
            if (lit)
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(90, c)))
                    g.FillEllipse(glow, cx - r * 2.1f, cy - r * 2.1f, r * 4.2f, r * 4.2f);
            using (SolidBrush b = new SolidBrush(c)) g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
            using (Pen pen = new Pen(Color.FromArgb(110, 0, 0, 0), Math.Max(1f, r * 0.35f))) g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            if (lit)
                using (SolidBrush hi = new SolidBrush(Color.FromArgb(170, 255, 255, 255)))
                    g.FillEllipse(hi, cx - r * 0.5f, cy - r * 0.6f, r * 0.7f, r * 0.6f);
        }

        static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Max(0.5f, rad * 2);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    /// <summary>
    /// Mappa del disco: una cella per ogni settore, colonne = tracce. Si colora mentre l'immagine
    /// viene letta o scritta (in attesa, in corso, fatto, errore). Aggiornabile da qualunque thread.
    /// </summary>
    class DiskMap : Control
    {
        const byte Pending = BlockState.Pending, Working = BlockState.Working, Done = BlockState.Done, Error = BlockState.Error;

        ImageKind kind = ImageKind.D64;
        byte[][] state = new byte[0][];
        string title = "";
        volatile bool dirty, finished;
        volatile int curTrack, curSector;
        bool blink;
        readonly Timer timer = new Timer { Interval = 120 };

        public DiskMap()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.C64Back;
            Cursor = Cursors.Hand;
            timer.Tick += delegate { blink = !blink; if (dirty || !finished) { dirty = false; Invalidate(); } };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>Prepara una mappa vuota (thread dell'interfaccia).</summary>
        public void Start(ImageKind k, string what)
        {
            kind = k;
            title = what;
            int tracks = CbmDrive.Tracks(k);
            byte[][] s = new byte[tracks][];
            for (int t = 1; t <= tracks; t++) s[t - 1] = new byte[CbmDrive.SectorsPerTrack(k, t)];
            state = s;
            curTrack = 0; curSector = 0;
            finished = false;
            timer.Start();
            Invalidate();
        }

        /// <summary>Stato di un settore (chiamabile dal thread di lavoro).</summary>
        public void Set(int track, int sector, byte st)
        {
            byte[][] s = state;
            if (track < 1 || track > s.Length || sector < 0 || sector >= s[track - 1].Length) return;
            s[track - 1][sector] = st;
            if (st == Working) { curTrack = track; curSector = sector; }
            dirty = true;
        }

        /// <summary>Operazione terminata: il settore "in corso" smette di lampeggiare.</summary>
        public void Finish()
        {
            finished = true;
            curTrack = 0;
            dirty = true;
            Invalidate();
        }

        // gruppi di tracce disegnati come blocchi separati: D71 = due facce, D81 = due metà
        int[][] Groups()
        {
            int tracks = state.Length;
            if (kind == ImageKind.D64 || tracks <= 35) return new[] { new[] { 1, tracks } };
            int half = tracks / 2;
            return new[] { new[] { 1, half }, new[] { half + 1, tracks } };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = Theme.C64Back, text = Theme.C64Text;
            g.Clear(back);
            if (state.Length == 0) return;
            // area utile: dentro gli stessi margini dello schermo della directory
            g.TranslateTransform(Padding.Left, Padding.Top);
            int W = Width - Padding.Horizontal, H = Height - Padding.Vertical;

            Color pending = Theme.Blend(back, text, 0.16f), done = text;
            Color err = Color.FromArgb(0xF0, 0x70, 0x6A), work = Theme.Blend(text, Color.White, 0.6f);

            int ok = 0, bad = 0, total = 0;
            foreach (byte[] tr in state)
                foreach (byte b in tr) { total++; if (b == Done) ok++; else if (b == Error) bad++; }

            using (Font big = ScreenFonts.Screen())
            using (Font small = new Font("Consolas", 7.5f, GraphicsUnit.Point))
            {
                int pad = 0;
                int lineH = big.Height + Theme.S(4);
                // titolo e contatori (stile schermo C64, in maiuscolo)
                string head = title.ToUpperInvariant();
                string count = string.Format(Lang.T("{0} SU {1} SETTORI"), ok + bad, total) + (bad > 0 ? string.Format(Lang.T(" · {0} ERRORI"), bad) : "");
                ScreenFonts.Draw(g, head, big, text, new Rectangle(pad, 0, W - 2 * pad, lineH));
                SizeF cw = ScreenFonts.Measure(g, count, big);
                ScreenFonts.Draw(g, count, big, bad > 0 ? err : text, new Rectangle(W - pad - (int)Math.Ceiling(cw.Width), 0, (int)Math.Ceiling(cw.Width) + 2, lineH));

                // legenda in basso
                int legendH = lineH;
                string hint = finished ? Lang.T("CLIC PER TORNARE ALLA DIRECTORY") : (curTrack > 0 ? string.Format(Lang.T("TRACCIA {0} SETTORE {1}"), curTrack, curSector) : "");
                int lx = pad;
                int sw = Math.Max(6, big.Height / 2);
                int ly = H - legendH;
                foreach (KeyValuePair<Color, string> it in new[] {
                    new KeyValuePair<Color, string>(pending, Lang.T("DA FARE")), new KeyValuePair<Color, string>(done, Lang.T("FATTO")),
                    new KeyValuePair<Color, string>(err, Lang.T("ERRORE")) })
                {
                    using (SolidBrush b = new SolidBrush(it.Key)) g.FillRectangle(b, lx, ly + (legendH - sw) / 2, sw, sw);
                    lx += sw + Theme.S(6);
                    SizeF lw = ScreenFonts.Measure(g, it.Value, big);
                    ScreenFonts.Draw(g, it.Value, big, text, new Rectangle(lx, ly, (int)Math.Ceiling(lw.Width) + 2, legendH));
                    lx += (int)Math.Ceiling(lw.Width) + Theme.S(18);
                }
                if (hint.Length > 0)
                {
                    SizeF hw = ScreenFonts.Measure(g, hint, big);
                    int hx = Math.Max(lx, W - pad - (int)Math.Ceiling(hw.Width));
                    ScreenFonts.Draw(g, hint, big, text, new Rectangle(hx, ly, (int)Math.Ceiling(hw.Width) + 2, legendH));
                }

                // griglia: sceglie tra gruppi affiancati o uno sotto l'altro, quello che dà celle più grandi
                int[][] groups = Groups();
                int maxSectors = 0;
                foreach (byte[] tr in state) maxSectors = Math.Max(maxSectors, tr.Length);
                // tracce molto lunghe (D81: 40 settori) piegate su due colonne, così le celle restano visibili
                int fold = maxSectors > 24 ? 2 : 1;
                int rows = (maxSectors + fold - 1) / fold;
                int cols = (groups[0][1] - groups[0][0] + 1) * fold;
                int labelH = small.Height + Theme.S(2);
                int captionH = groups.Length > 1 ? small.Height + Theme.S(2) : 0;
                int areaX = pad, areaY = lineH + Theme.S(4), areaW = W - 2 * pad, areaH = ly - areaY - Theme.S(4);
                int gap = Theme.S(18);
                int n = groups.Length;
                int cellSide = Math.Min((areaW - gap * (n - 1)) / (cols * n), (areaH - labelH - captionH) / rows);
                int cellStack = Math.Min(areaW / cols, (areaH - n * (labelH + captionH) - gap * (n - 1)) / (rows * n));
                bool stacked = n > 1 && cellStack > cellSide;
                int cell = Math.Max(2, stacked ? cellStack : cellSide);
                int inner = cell >= 5 ? cell - 1 : cell;   // 1 px di spazio tra le celle, se c'è posto

                int groupW = cols * cell, groupH = captionH + rows * cell + labelH;
                int totalW = stacked ? groupW : groupW * n + gap * (n - 1);
                int totalH = stacked ? groupH * n + gap * (n - 1) : groupH;
                int ox = areaX + Math.Max(0, (areaW - totalW) / 2), oy = areaY + Math.Max(0, (areaH - totalH) / 2);

                using (SolidBrush bPending = new SolidBrush(pending), bDone = new SolidBrush(done), bErr = new SolidBrush(err), bWork = new SolidBrush(work))
                using (SolidBrush bLabel = new SolidBrush(Theme.Blend(back, text, 0.6f)))
                {
                    for (int gi = 0; gi < n; gi++)
                    {
                        int gx = stacked ? ox : ox + gi * (groupW + gap);
                        int gy = stacked ? oy + gi * (groupH + gap) : oy;
                        if (captionH > 0)
                        {
                            string cap = kind == ImageKind.D71
                                ? (gi == 0 ? Lang.T("LATO 1") : Lang.T("LATO 2"))
                                : string.Format(Lang.T("TRACCE {0}-{1}"), groups[gi][0], groups[gi][1]);
                            g.DrawString(cap, small, bLabel, gx, gy);
                        }
                        int top = gy + captionH;
                        // numeri di traccia: ogni 5, 10 o 20 tracce, secondo lo spazio (senza sovrapporsi)
                        int colW = cell * fold;
                        float labelW = g.MeasureString("80", small).Width + Theme.S(4);
                        int step = 5;
                        while (step < 20 && step * colW < labelW) step *= 2;
                        for (int t = groups[gi][0]; t <= groups[gi][1]; t++)
                        {
                            byte[] tr = state[t - 1];
                            int x = gx + (t - groups[gi][0]) * colW;
                            for (int s = 0; s < tr.Length; s++)
                            {
                                byte st = tr[s];
                                // il settore in lavorazione lampeggia; a operazione finita (o interrotta) torna "da fare"
                                SolidBrush b = st == Done ? bDone : st == Error ? bErr : st == Working && !finished ? (blink ? bWork : bPending) : bPending;
                                g.FillRectangle(b, x + (s / rows) * cell, top + (s % rows) * cell, inner, inner);
                            }
                            int local = t - groups[gi][0];
                            int toNext = (step - t % step) % step;   // tracce mancanti al prossimo numero
                            if (t % step == 0 || (local == 0 && toNext * colW >= labelW))
                            {
                                string num = t.ToString();
                                SizeF nw = g.MeasureString(num, small);
                                g.DrawString(num, small, bLabel, x + colW / 2f - nw.Width / 2, top + rows * cell + 1);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>Icone pixel 8×8 dei tipi di file (disegni originali), per lo schermo della directory.</summary>
    static class FileTypeIcons
    {
        static readonly Dictionary<string, string[]> Shapes = new Dictionary<string, string[]>
        {
            // programma: finestra con il triangolo "avvia"
            { "PRG", new[] { "#######.", "#.....#.", "#.#...#.", "#.##..#.", "#.###.#.", "#.##..#.", "#.#...#.", "#######." } },
            // dati sequenziali: foglio con le righe di testo
            { "SEQ", new[] { "#####...", "#...##..", "#.#..##.", "#.....#.", "#.###.#.", "#.....#.", "#.###.#.", "#######." } },
            // file utente: omino
            { "USR", new[] { "..###...", "..###...", "..###...", "........", ".#####..", "#######.", "#######.", "#######." } },
            // relativo: tabella di record
            { "REL", new[] { "#######.", "#..#..#.", "#######.", "#..#..#.", "#######.", "#..#..#.", "#######.", "........" } },
            // cancellato: cestino
            { "DEL", new[] { "..###...", "#######.", ".#.#.#..", ".#.#.#..", ".#.#.#..", ".#.#.#..", ".#####..", "........" } },
            // partizione / sottodirectory (1581): cartella
            { "DIR", new[] { "###.....", "#..####.", "#######.", "#.....#.", "#.....#.", "#.....#.", "#######.", "........" } },
        };

        // colori ispirati alla tavolozza del C64 (Colodore), leggibili sullo schermo blu
        static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "PRG", Color.FromArgb(0xA9, 0xFF, 0x9F) },
            { "SEQ", Color.FromArgb(0xED, 0xF1, 0x71) },
            { "USR", Color.FromArgb(0x75, 0xCE, 0xC8) },
            { "REL", Color.FromArgb(0xF0, 0xA0, 0x62) },
            { "DEL", Color.FromArgb(0xA8, 0xA8, 0xA8) },
            { "DIR", Color.FromArgb(0xE0, 0x9A, 0xF0) },
        };

        static string Key(string type)
        {
            if (type == "CBM") return "DIR";
            return Shapes.ContainsKey(type ?? "") ? type : "PRG";
        }

        public static Color ColorFor(string type) { return Colors[Key(type)]; }

        /// <summary>Disegna l'icona con pixel di lato px (color vuoto = colore del tipo).</summary>
        public static void Draw(Graphics g, string type, int x, int y, int px, Color color)
        {
            string[] rows = Shapes[Key(type)];
            if (color.IsEmpty) color = ColorFor(type);
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush b = new SolidBrush(color))
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                        if (rows[r][c] == '#') g.FillRectangle(b, x + c * px, y + r * px, px, px);
            g.SmoothingMode = old;
        }
    }
}
