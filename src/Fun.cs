// Svago: i minigiochi "Floppy Runner" e "Assalto PETSCII" e il salvaschermo PETSCII.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace XumerTool
{
    // =====================================================================
    //  Floppy Runner: un floppy corre e salta calamite e torri di blocchi (genere "endless runner")
    // =====================================================================
    class GameForm : Form
    {
        class Obstacle { public float X; public int W, H, Kind; }

        readonly Timer timer = new Timer { Interval = 15 };
        readonly Stopwatch clock = new Stopwatch();
        readonly Random rnd = new Random();
        readonly List<Obstacle> obstacles = new List<Obstacle>();
        readonly List<PointF> stars = new List<PointF>();
        readonly Font hud = new Font("Consolas", 13f, FontStyle.Bold);
        readonly Font big = new Font("Consolas", 16f, FontStyle.Bold);
        readonly Color red = Color.FromArgb(0xE8, 0x4B, 0x4B);

        float playerY, vy, speed, score, groundScroll, nextGap;
        int hi;
        bool started, over;
        long lastTicks;

        int GroundY { get { return ClientSize.Height - Theme.S(56); } }
        int PlayerX { get { return Theme.S(90); } }
        int PlayerSize { get { return Theme.S(34); } }

        public GameForm()
        {
            Text = Lang.T("Floppy Runner");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ClientSize = new Size(Theme.S(900), Theme.S(320));
            BackColor = Theme.C64Back;
            DoubleBuffered = true;
            KeyPreview = true;
            int.TryParse(Settings.Get("gamehi", "0"), out hi);
            for (int i = 0; i < 40; i++) stars.Add(new PointF(rnd.Next(ClientSize.Width), rnd.Next(GroundY - Theme.S(40))));
            Reset();
            timer.Tick += delegate { Step(); Invalidate(); };
            clock.Start();
            timer.Start();
        }

        /// <summary>Per le anteprime: una partita in corso, ferma.</summary>
        public void PreviewState()
        {
            timer.Stop();
            started = true; score = 137; hi = Math.Max(hi, 452); playerY = Theme.S(46);
            obstacles.Add(new Obstacle { X = Theme.S(330), Kind = 0, W = Theme.S(26), H = Theme.S(26) });
            obstacles.Add(new Obstacle { X = Theme.S(560), Kind = 2, W = Theme.S(16), H = Theme.S(44) });
            obstacles.Add(new Obstacle { X = Theme.S(780), Kind = 1, W = Theme.S(26), H = Theme.S(48) });
        }

        void Reset()
        {
            obstacles.Clear();
            playerY = 0; vy = 0; speed = 7f; score = 0; nextGap = Theme.S(500);
            over = false;
        }

        void Jump()
        {
            if (over) { Reset(); started = true; return; }
            started = true;
            if (playerY <= 0.01f) vy = Theme.S(15);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up || e.KeyCode == Keys.W || e.KeyCode == Keys.Enter) { Jump(); e.Handled = true; }
            else if (e.KeyCode == Keys.Down && playerY > 0) vy = -Theme.S(18);
            else if (e.KeyCode == Keys.Escape) Close();
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e) { Jump(); base.OnMouseDown(e); }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Stop();
            base.OnFormClosed(e);
        }

        void Step()
        {
            long now = clock.ElapsedTicks;
            float dt = lastTicks == 0 ? 1f : (float)((now - lastTicks) / (double)Stopwatch.Frequency * 60.0);
            lastTicks = now;
            dt = Math.Min(dt, 3f);
            for (int i = 0; i < stars.Count; i++)
            {
                PointF p = stars[i];
                p.X -= 0.3f * dt * (started && !over ? speed / 7f : 0.3f);
                if (p.X < 0) p = new PointF(ClientSize.Width, rnd.Next(GroundY - Theme.S(40)));
                stars[i] = p;
            }
            if (!started || over) return;

            float s = speed * Theme.Scale * dt;
            groundScroll = (groundScroll + s) % Theme.S(24);
            score += speed * dt * 0.05f;
            speed = Math.Min(16f, speed + 0.0022f * dt);

            // salto
            vy -= 0.85f * Theme.Scale * dt;
            playerY = Math.Max(0, playerY + vy * dt);
            if (playerY <= 0) vy = 0;

            // ostacoli
            foreach (Obstacle o in obstacles) o.X -= s;
            obstacles.RemoveAll(o => o.X + o.W < 0);
            nextGap -= s;
            if (nextGap <= 0)
            {
                Obstacle o = new Obstacle { X = ClientSize.Width + Theme.S(10), Kind = rnd.Next(3) };
                if (o.Kind == 0) { o.W = Theme.S(26); o.H = Theme.S(28); }                 // calamita
                else if (o.Kind == 1) { o.W = Theme.S(30); o.H = Theme.S(46); }            // calamita grande
                else { o.W = Theme.S(16); o.H = Theme.S(30 + rnd.Next(22)); }              // torre di blocchi PETSCII
                obstacles.Add(o);
                if (rnd.Next(5) == 0 && score > 150)
                    obstacles.Add(new Obstacle { X = o.X + o.W + Theme.S(4), Kind = 0, W = Theme.S(26), H = Theme.S(28) });
                nextGap = Theme.S(260) + (float)rnd.NextDouble() * Theme.S(320) + speed * Theme.S(14);
            }

            // collisioni
            Rectangle pr = new Rectangle(PlayerX + Theme.S(5), GroundY - PlayerSize - (int)playerY + Theme.S(5), PlayerSize - Theme.S(10), PlayerSize - Theme.S(8));
            foreach (Obstacle o in obstacles)
            {
                Rectangle or = new Rectangle((int)o.X + Theme.S(3), GroundY - o.H + Theme.S(3), o.W - Theme.S(6), o.H - Theme.S(3));
                if (pr.IntersectsWith(or))
                {
                    over = true;
                    if ((int)score > hi) { hi = (int)score; Settings.Set("gamehi", hi.ToString()); }
                    break;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fg = Theme.C64Text;
            using (SolidBrush dim = new SolidBrush(Theme.Blend(Theme.C64Back, fg, 0.35f)))
                foreach (PointF p in stars) g.FillRectangle(dim, p.X, p.Y, Theme.S(2), Theme.S(2));

            // terreno
            using (Pen pen = new Pen(fg, Theme.S(2))) g.DrawLine(pen, 0, GroundY, ClientSize.Width, GroundY);
            using (SolidBrush b = new SolidBrush(Theme.Blend(Theme.C64Back, fg, 0.55f)))
                for (float x = -groundScroll; x < ClientSize.Width; x += Theme.S(24))
                {
                    g.FillRectangle(b, x, GroundY + Theme.S(8), Theme.S(8), Theme.S(2));
                    g.FillRectangle(b, x + Theme.S(13), GroundY + Theme.S(17), Theme.S(4), Theme.S(2));
                }

            foreach (Obstacle o in obstacles) DrawObstacle(g, o, fg);

            float bob = started && !over && playerY <= 0 ? (float)Math.Abs(Math.Sin(clock.ElapsedMilliseconds / 70.0)) * Theme.S(2) : 0;
            DrawFloppy(g, PlayerX, GroundY - PlayerSize - (int)playerY - (int)bob, PlayerSize);

            string sc = Lang.T("HI ") + hi.ToString("D5") + "   " + ((int)score).ToString("D5");
            Size ss = TextRenderer.MeasureText(sc, hud);
            TextRenderer.DrawText(g, sc, hud, new Point(ClientSize.Width - ss.Width - Theme.S(16), Theme.S(12)), fg);

            if (!started) CenterText(g, Lang.T("PREMI SPAZIO (O CLICCA) PER SALTARE"), fg, -Theme.S(40));
            if (over)
            {
                CenterText(g, Lang.T("?GAME OVER  ERROR"), fg, -Theme.S(60));
                CenterText(g, Lang.T("FLOPPY SMAGNETIZZATO!  PREMI SPAZIO PER RIPROVARE"), fg, -Theme.S(28));
            }
        }

        void CenterText(Graphics g, string s, Color c, int dy)
        {
            Size sz = TextRenderer.MeasureText(s, big);
            TextRenderer.DrawText(g, s, big, new Point((ClientSize.Width - sz.Width) / 2, ClientSize.Height / 2 + dy - sz.Height / 2), c);
        }

        void DrawObstacle(Graphics g, Obstacle o, Color fg)
        {
            int x = (int)o.X, top = GroundY - o.H;
            using (SolidBrush b = new SolidBrush(fg))
            using (SolidBrush back = new SolidBrush(Theme.C64Back))
            {
                if (o.Kind == 2)
                {
                    // torre di blocchi a scacchi, stile PETSCII
                    int cell = Theme.S(8);
                    for (int yy = 0; yy * cell < o.H; yy++)
                        for (int xx = 0; xx * cell < o.W; xx++)
                            if (((xx + yy) & 1) == 0)
                                g.FillRectangle(b, x + xx * cell, GroundY - (yy + 1) * cell, cell, cell);
                    g.FillRectangle(b, x, top, o.W, Theme.S(3));
                    return;
                }
            }
            DrawMagnet(g, x, top, o.W, o.H);
        }

        /// <summary>Calamita a ferro di cavallo, rossa con le punte d'argento: il nemico dei floppy.</summary>
        void DrawMagnet(Graphics g, int x, int top, int w, int h)
        {
            float t = w * 0.34f;                      // spessore dei bracci
            float arcSize = w - t;                    // diametro dell'arco (sull'asse dei bracci)
            float legH = h - w / 2f;                  // altezza dei bracci dritti
            using (SolidBrush rb = new SolidBrush(red))
            using (SolidBrush silver = new SolidBrush(Color.FromArgb(0xD8, 0xDC, 0xE6)))
            using (Pen arc = new Pen(red, t))
            {
                g.FillRectangle(rb, x, top, t, legH);
                g.FillRectangle(rb, x + w - t, top, t, legH);
                g.DrawArc(arc, x + t / 2f, top + legH - arcSize / 2f, arcSize, arcSize, 0, 180);
                g.FillRectangle(silver, x, top, t, t * 0.9f);
                g.FillRectangle(silver, x + w - t, top, t, t * 0.9f);
            }
        }

        /// <summary>Il protagonista: un floppy da 5¼" (busta scura, etichetta con le righe colorate). In salto si inclina.</summary>
        void DrawFloppy(Graphics g, int x, int y, int s)
        {
            GraphicsState state = g.Save();
            float angle = playerY > 0 ? Math.Max(-22f, Math.Min(22f, -vy * 1.4f)) : 0f;
            g.TranslateTransform(x + s / 2f, y + s / 2f);
            g.RotateTransform(angle);
            g.TranslateTransform(-s / 2f, -s / 2f);

            RectangleF body = new RectangleF(0, 0, s, s);
            using (GraphicsPath p = Theme.Rounded(Rectangle.Round(body), Math.Max(2, s / 10)))
            {
                using (SolidBrush jacket = new SolidBrush(Color.FromArgb(0x22, 0x26, 0x32))) g.FillPath(jacket, p);
                using (Pen edge = new Pen(Theme.C64Text, Math.Max(1f, s / 18f))) g.DrawPath(edge, p);
            }
            // etichetta
            RectangleF label = new RectangleF(s * 0.16f, s * 0.09f, s * 0.68f, s * 0.26f);
            using (SolidBrush paper = new SolidBrush(Color.FromArgb(0xF2, 0xEE, 0xDF))) g.FillRectangle(paper, label);
            int[] stripes = { 0xE5534B, 0xF2A33A, 0xF2D94B, 0x5FBF6A, 0x5B86E5 };
            float sh = label.Height * 0.12f;
            for (int i = 0; i < stripes.Length; i++)
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(0xFF << 24 | stripes[i])))
                    g.FillRectangle(sb, label.X + label.Width * 0.08f, label.Bottom - label.Height * 0.12f - (stripes.Length - i) * sh, label.Width * 0.84f, sh + 0.4f);
            // foro centrale con anello
            float cx = s * 0.5f, cy = s * 0.6f, ro = s * 0.17f, ri = s * 0.09f;
            using (SolidBrush ring = new SolidBrush(Color.FromArgb(0xC9, 0xCE, 0xD8))) g.FillEllipse(ring, cx - ro, cy - ro, 2 * ro, 2 * ro);
            using (SolidBrush hole = new SolidBrush(Theme.C64Back)) g.FillEllipse(hole, cx - ri, cy - ri, 2 * ri, 2 * ri);
            // finestra di lettura e tacca di protezione
            using (SolidBrush media = new SolidBrush(Color.FromArgb(0x6A, 0x4A, 0x2C)))
                g.FillRectangle(media, s * 0.45f, s * 0.8f, s * 0.1f, s * 0.13f);
            using (SolidBrush back = new SolidBrush(Theme.C64Back))
                g.FillRectangle(back, s - s * 0.07f, s * 0.2f, s * 0.08f, s * 0.09f);
            g.Restore(state);
        }
    }

    // =====================================================================
    //  Assalto PETSCII: sparatutto con alieni in formazione (gioco originale)
    // =====================================================================
    class AssaultForm : Form
    {
        // sprite originali 8x6 (due fotogrammi per tipo)
        static readonly string[][][] AlienSprites =
        {
            new[] { new[] { "..####..", ".######.", "##.##.##", "########", ".#.##.#.", "#......#" },
                    new[] { "..####..", ".######.", "##.##.##", "########", "..#..#..", ".#.##.#." } },
            new[] { new[] { "#.####.#", "########", "##.##.##", ".######.", "..#..#..", ".##..##." },
                    new[] { "..####..", "########", "##.##.##", ".######.", ".#.##.#.", "#......#" } },
            new[] { new[] { "...##...", "..####..", ".##..##.", "########", "..#..#..", ".#....#." },
                    new[] { "...##...", "..####..", ".##..##.", "########", ".#.##.#.", "#......#" } },
        };
        // l'astronave è un drive 1541 stilizzato
        static readonly string[] Ship = { "....###....", "....###....", "###########", "#.........#", "#.#######.#", "###########" };
        static readonly Color[] RowColors =
        {
            Color.FromArgb(0xB4, 0x8F, 0xF0), Color.FromArgb(0x6A, 0xBF, 0xC6), Color.FromArgb(0x6A, 0xBF, 0xC6),
            Color.FromArgb(0xA9, 0xE2, 0x86), Color.FromArgb(0xED, 0xF1, 0x71),
        };

        const int Rows = 5, Cols = 10;
        readonly Timer timer = new Timer { Interval = 15 };
        readonly Stopwatch clock = new Stopwatch();
        readonly Random rnd = new Random();
        readonly Font hud = new Font("Consolas", 13f, FontStyle.Bold);
        readonly Font big = new Font("Consolas", 18f, FontStyle.Bold);
        readonly bool[,] alive = new bool[Rows, Cols];
        readonly List<RectangleF> enemyShots = new List<RectangleF>();
        float shipX, ox, oy, stepTimer, fireTimer, ufoX = -1, deadTimer;
        int dir = 1, frame, score, hi, lives, wave, aliveCount;
        RectangleF? shot;
        bool started, over, left, right;
        long lastTicks;

        int Px { get { return Theme.S(4); } }
        int CellW { get { return Theme.S(52); } }
        int CellH { get { return Theme.S(40); } }
        int ShipY { get { return ClientSize.Height - Theme.S(70); } }

        public AssaultForm()
        {
            Text = Lang.T("Assalto PETSCII");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(820), Theme.S(620));
            BackColor = Theme.C64Back;
            DoubleBuffered = true;
            KeyPreview = true;
            int.TryParse(Settings.Get("assaulthi", "0"), out hi);
            NewGame();
            timer.Tick += delegate { Step(); Invalidate(); };
            clock.Start();
            timer.Start();
        }

        void NewGame() { score = 0; lives = 3; wave = 0; over = false; NewWave(); }

        void NewWave()
        {
            wave++;
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++) alive[r, c] = true;
            aliveCount = Rows * Cols;
            ox = Theme.S(60); oy = Theme.S(70) + Math.Min(wave - 1, 5) * Theme.S(14);
            dir = 1; shot = null; enemyShots.Clear(); ufoX = -1;
            shipX = ClientSize.Width / 2f;
        }

        /// <summary>Per le anteprime: una partita in corso, ferma.</summary>
        public void PreviewState()
        {
            timer.Stop();
            started = true; score = 740; hi = Math.Max(hi, 2310);
            alive[4, 2] = alive[4, 3] = alive[3, 7] = alive[0, 0] = false; aliveCount -= 4;
            ox = Theme.S(110); shot = new RectangleF(shipX + Theme.S(60), ShipY - Theme.S(150), Theme.S(3), Theme.S(14));
            enemyShots.Add(new RectangleF(Theme.S(300), Theme.S(420), Theme.S(3), Theme.S(12)));
            ufoX = Theme.S(560);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) left = true;
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) right = true;
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up || e.KeyCode == Keys.Enter)
            {
                if (!started || over) { if (over) NewGame(); started = true; }
                else if (shot == null && deadTimer <= 0)
                    shot = new RectangleF(shipX - Theme.S(1), ShipY - Theme.S(14), Theme.S(3), Theme.S(14));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape) Close();
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) left = false;
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) right = false;
            base.OnKeyUp(e);
        }

        protected override bool IsInputKey(Keys k) { return k == Keys.Left || k == Keys.Right || k == Keys.Up || base.IsInputKey(k); }

        protected override void OnFormClosed(FormClosedEventArgs e) { timer.Stop(); base.OnFormClosed(e); }

        RectangleF AlienRect(int r, int c)
        {
            return new RectangleF(ox + c * CellW, oy + r * CellH, 8 * Px, 6 * Px);
        }

        void Step()
        {
            long now = clock.ElapsedTicks;
            float dt = lastTicks == 0 ? 0.016f : (float)((now - lastTicks) / (double)Stopwatch.Frequency);
            lastTicks = now;
            dt = Math.Min(dt, 0.05f);
            if (!started || over) return;

            if (deadTimer > 0) { deadTimer -= dt; if (deadTimer <= 0 && lives <= 0) EndGame(); return; }

            float sp = Theme.S(330) * dt;
            if (left) shipX -= sp;
            if (right) shipX += sp;
            float half = Ship[0].Length * Px / 2f;
            shipX = Math.Max(half + Theme.S(10), Math.Min(ClientSize.Width - half - Theme.S(10), shipX));

            // formazione: avanza a scatti, più veloce quando restano pochi alieni
            stepTimer -= dt;
            if (stepTimer <= 0)
            {
                stepTimer = 0.04f + 0.6f * aliveCount / (Rows * Cols) / (1f + (wave - 1) * 0.15f);
                frame ^= 1;
                float minX = float.MaxValue, maxX = float.MinValue;
                for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++)
                        if (alive[r, c]) { RectangleF a = AlienRect(r, c); minX = Math.Min(minX, a.Left); maxX = Math.Max(maxX, a.Right); }
                float dx = Theme.S(10) * dir;
                if (maxX + dx > ClientSize.Width - Theme.S(14) || minX + dx < Theme.S(14)) { dir = -dir; oy += Theme.S(16); }
                else ox += dx;
            }

            // colpo del giocatore
            if (shot.HasValue)
            {
                RectangleF s = shot.Value;
                s.Y -= Theme.S(720) * dt;
                shot = s.Bottom < 0 ? (RectangleF?)null : s;
                if (shot.HasValue)
                {
                    for (int r = 0; r < Rows && shot.HasValue; r++)
                        for (int c = 0; c < Cols; c++)
                            if (alive[r, c] && AlienRect(r, c).IntersectsWith(shot.Value))
                            {
                                alive[r, c] = false; aliveCount--; shot = null;
                                score += (Rows - r) * 10;
                                break;
                            }
                    if (shot.HasValue && ufoX >= 0 && new RectangleF(ufoX, Theme.S(40), Theme.S(28), Theme.S(22)).IntersectsWith(shot.Value))
                    {
                        score += 100 * (1 + rnd.Next(3)); ufoX = -1; shot = null;
                    }
                }
            }
            if (aliveCount == 0) { NewWave(); return; }

            // floppy bonus
            if (ufoX < 0 && rnd.NextDouble() < 0.0015) ufoX = 0.01f;
            if (ufoX >= 0) { ufoX += Theme.S(140) * dt; if (ufoX > ClientSize.Width) ufoX = -1; }

            // colpi degli alieni, dal più basso di una colonna a caso
            fireTimer -= dt;
            if (fireTimer <= 0)
            {
                fireTimer = Math.Max(0.25f, 1.1f - wave * 0.1f) * (0.5f + (float)rnd.NextDouble());
                int c = rnd.Next(Cols);
                for (int r = Rows - 1; r >= 0; r--)
                    if (alive[r, c])
                    {
                        RectangleF a = AlienRect(r, c);
                        enemyShots.Add(new RectangleF(a.X + a.Width / 2, a.Bottom, Theme.S(3), Theme.S(12)));
                        break;
                    }
            }
            RectangleF shipRect = new RectangleF(shipX - half, ShipY, Ship[0].Length * Px, Ship.Length * Px);
            for (int i = enemyShots.Count - 1; i >= 0; i--)
            {
                RectangleF s = enemyShots[i];
                s.Y += Theme.S(300) * dt;
                if (s.Top > ClientSize.Height) { enemyShots.RemoveAt(i); continue; }
                enemyShots[i] = s;
                if (s.IntersectsWith(shipRect)) { enemyShots.Clear(); lives--; deadTimer = 1.2f; return; }
            }

            // gli alieni arrivano in fondo: fine partita
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++)
                    if (alive[r, c] && AlienRect(r, c).Bottom >= ShipY) { lives = 0; EndGame(); return; }
        }

        void EndGame()
        {
            over = true;
            if (score > hi) { hi = score; Settings.Set("assaulthi", hi.ToString()); }
        }

        void DrawSprite(Graphics g, string[] rows, float x, float y, Color color)
        {
            using (SolidBrush b = new SolidBrush(color))
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                        if (rows[r][c] == '#') g.FillRectangle(b, x + c * Px, y + r * Px, Px, Px);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color fg = Theme.C64Text;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (alive[r, c])
                    {
                        RectangleF a = AlienRect(r, c);
                        DrawSprite(g, AlienSprites[r == 0 ? 2 : r <= 2 ? 1 : 0][frame], a.X, a.Y, RowColors[r]);
                    }

            if (ufoX >= 0)
                using (SolidBrush b = new SolidBrush(Color.FromArgb(0xE8, 0x4B, 0x4B)))
                using (SolidBrush back = new SolidBrush(Theme.C64Back))
                {
                    g.FillRectangle(b, ufoX, Theme.S(40), Theme.S(28), Theme.S(22));
                    g.FillRectangle(back, ufoX + Theme.S(11), Theme.S(48), Theme.S(6), Theme.S(6));
                }

            using (SolidBrush b = new SolidBrush(fg))
            {
                if (shot.HasValue) g.FillRectangle(b, shot.Value);
                foreach (RectangleF s in enemyShots) g.FillRectangle(b, s);
            }

            float half = Ship[0].Length * Px / 2f;
            bool blink = deadTimer > 0 && ((int)(deadTimer * 10)) % 2 == 0;
            if (!blink) DrawSprite(g, Ship, shipX - half, ShipY, fg);

            using (Pen pen = new Pen(fg, Theme.S(2)))
                g.DrawLine(pen, 0, ShipY + Ship.Length * Px + Theme.S(10), ClientSize.Width, ShipY + Ship.Length * Px + Theme.S(10));

            string h = Lang.T("PUNTI ") + score.ToString("D5") + Lang.T("   HI ") + hi.ToString("D5") + Lang.T("   ONDATA ") + wave + Lang.T("   VITE ") + new string('♥', Math.Max(0, lives));
            TextRenderer.DrawText(g, h, hud, new Point(Theme.S(16), Theme.S(10)), fg, TextFormatFlags.NoPrefix);

            if (!started) Center(g, Lang.T("PREMI SPAZIO PER INIZIARE   ← → MUOVI   SPAZIO SPARA"), fg, 0);
            if (over)
            {
                Center(g, Lang.T("?GAME OVER  ERROR"), fg, -Theme.S(20));
                Center(g, Lang.T("READY.  PREMI SPAZIO PER RIGIOCARE"), fg, Theme.S(18));
            }
        }

        void Center(Graphics g, string s, Color c, int dy)
        {
            Size sz = TextRenderer.MeasureText(s, big);
            TextRenderer.DrawText(g, s, big, new Point((ClientSize.Width - sz.Width) / 2, ClientSize.Height / 2 + dy), c, TextFormatFlags.NoPrefix);
        }
    }

    // =====================================================================
    //  Salvaschermo PETSCII: nomi Commodore in grandi caratteri a blocchi
    // =====================================================================

    /// <summary>L'animazione del salvaschermo, usabile a tutto schermo o dentro lo schermo della directory.</summary>
    class PetsciiSaver : Control
    {
        static readonly string[][] Items =
        {
            new[] { Lang.T("COMMODORE 64"), Lang.T("1982 - il computer più venduto di sempre") },
            new[] { "VIC-20", Lang.T("1980 - il primo computer a superare il milione di pezzi") },
            new[] { Lang.T("PET 2001"), Lang.T("1977 - il primo computer di Commodore") },
            new[] { "C128", Lang.T("1985 - tre modalità: C128, C64 e CP/M") },
            new[] { Lang.T("AMIGA 500"), Lang.T("1987 - grafica e suono da sala giochi in casa") },
            new[] { "PLUS/4", Lang.T("1984 - con il software integrato in ROM") },
            new[] { "SX-64", Lang.T("1984 - il primo computer portatile a colori") },
            new[] { "1541", Lang.T("il drive da 5,25\" del C64: 170 KB per facciata") },
            new[] { "1571", Lang.T("il drive doppia faccia del C128: 340 KB") },
            new[] { "1581", Lang.T("il drive da 3,5\": 800 KB") },
            new[] { Lang.T("SID 6581"), Lang.T("il chip sonoro progettato da Bob Yannes") },
            new[] { "VIC-II", Lang.T("il chip video progettato da Al Charpentier") },
            new[] { "6502", Lang.T("la CPU progettata da Chuck Peddle e dal suo team alla MOS") },
            new[] { Lang.T("JACK TRAMIEL"), Lang.T("il fondatore di Commodore") },
            new[] { Lang.T("CHUCK PEDDLE"), Lang.T("il padre del 6502 e del PET") },
            new[] { Lang.T("BOB YANNES"), Lang.T("il creatore del SID") },
            new[] { Lang.T("AL CHARPENTIER"), Lang.T("il progettista del VIC-II") },
            new[] { Lang.T("BIL HERD"), Lang.T("il progettista del C128 e del Plus/4") },
            new[] { Lang.T("JAY MINER"), Lang.T("il padre dei chip dell'Amiga") },
            new[] { Lang.T("DAVE HAYNIE"), Lang.T("ingegnere di Commodore, dal C128 all'Amiga") },
        };

        static readonly string Noise = "█▓▒░▀▄▌▐■●♥♠♣♦┼╬═║╔╗╚╝▲▼◄►";

        static readonly Color[] Palette =
        {
            Color.FromArgb(0x6A, 0xBF, 0xC6), Color.FromArgb(0xA9, 0xE2, 0x86), Color.FromArgb(0xED, 0xF1, 0x71),
            Color.FromArgb(0xB4, 0x8F, 0xF0), Color.FromArgb(0xE8, 0x9E, 0x5E), Color.FromArgb(0xFF, 0xFF, 0xFF),
            Color.FromArgb(0xE0, 0x7A, 0x7A), Color.FromArgb(0x9A, 0x97, 0xF7),
        };

        class Cell { public int Col, Row; public char Ch; public float T0, V; public float X0, Y0, Speed; }

        readonly Timer timer = new Timer { Interval = 33 };
        readonly Stopwatch clock = new Stopwatch();
        readonly Random rnd = new Random();
        readonly Font font, captionFont, footFont;
        readonly int cellW, cellH;
        readonly string footer;
        readonly Color captionColor, footColor;
        int cols, rows, item = -1, effect;
        List<Cell> cells = new List<Cell>();
        char[,] grid;
        byte[,] layer;
        Color color;
        float t, introEnd;
        int bigBottom;

        public PetsciiSaver(float fontSize, Color back, string footerText)
        {
            BackColor = back;
            footer = footerText;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            font = new Font("Consolas", fontSize);
            captionFont = new Font("Consolas", fontSize + 1.5f);
            footFont = new Font("Consolas", Math.Max(8f, fontSize - 2f));
            Size m = TextRenderer.MeasureText("MM", font, Size.Empty, TextFormatFlags.NoPadding);
            Size m1 = TextRenderer.MeasureText("M", font, Size.Empty, TextFormatFlags.NoPadding);
            cellW = Math.Max(4, m.Width - m1.Width);
            cellH = m1.Height;
            bool darkBack = back.GetBrightness() < 0.25f;
            captionColor = darkBack ? Color.FromArgb(0xD0, 0xD0, 0xD0) : Color.White;
            footColor = Theme.Blend(back, Color.White, 0.35f);
            timer.Tick += delegate { Advance(); Invalidate(); };
        }

        public void Start()
        {
            Layout_();
            clock.Restart();
            timer.Start();
        }

        public void Stop() { timer.Stop(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (grid != null) Layout_();
        }

        void Layout_()
        {
            cols = Math.Max(10, ClientSize.Width / cellW);
            rows = Math.Max(6, ClientSize.Height / cellH);
            grid = new char[rows, cols];
            layer = new byte[rows, cols];
            NextItem();
            if (previewAt.HasValue) { clock.Stop(); t = previewAt.Value; Compute(); Invalidate(); }
        }

        /// <summary>Disegna il nome in una bitmap e la converte in celle con mezzi blocchi (▀ ▄ █).</summary>
        void NextItem()
        {
            int prev = item;
            do item = rnd.Next(Items.Length); while (item == prev && Items.Length > 1);
            effect = rnd.Next(4);
            Color c;
            do c = Palette[rnd.Next(Palette.Length)]; while (Math.Abs(c.GetBrightness() - BackColor.GetBrightness()) < 0.2f);
            color = c;
            string word = Items[item][0];

            int pw = cols, ph = rows * 2;
            cells = new List<Cell>();
            using (Bitmap bmp = new Bitmap(pw, ph))
            {
                float size = ph * 0.5f;
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                    Font f = null;
                    SizeF sz = SizeF.Empty;
                    for (; size > 4; size -= 1f)
                    {
                        if (f != null) f.Dispose();
                        f = new Font("Arial Black", size, FontStyle.Regular, GraphicsUnit.Pixel);
                        sz = g.MeasureString(word, f);
                        if (sz.Width <= pw * 0.88f && sz.Height <= ph * 0.46f) break;
                    }
                    g.Clear(Color.Black);
                    float x = (pw - sz.Width) / 2f, y = ph * 0.38f - sz.Height / 2f;
                    g.DrawString(word, f, Brushes.White, x, y);
                    f.Dispose();
                }
                bigBottom = 0;
                for (int r = 0; r < rows; r++)
                    for (int col = 0; col < cols; col++)
                    {
                        bool top = bmp.GetPixel(col, r * 2).R > 128, bot = bmp.GetPixel(col, r * 2 + 1).R > 128;
                        if (!top && !bot) continue;
                        char ch = top && bot ? '█' : top ? '▀' : '▄';
                        cells.Add(new Cell
                        {
                            Col = col, Row = r, Ch = ch,
                            T0 = (float)rnd.NextDouble() * 1.6f,
                            V = (float)rnd.NextDouble() * 0.9f,
                            X0 = rnd.Next(cols), Y0 = -rnd.Next(rows) - 1,
                            Speed = 18f + (float)rnd.NextDouble() * 30f
                        });
                        bigBottom = Math.Max(bigBottom, r);
                    }
            }
            introEnd = effect == 2 ? cols / 55f + 0.4f : effect == 1 ? 2.6f : 2.0f;
            t = 0;
            clock.Restart();
        }

        char NoiseChar() { return Noise[rnd.Next(Noise.Length)]; }

        void Put(int r, int c, char ch, byte l)
        {
            if (r < 0 || r >= rows || c < 0 || c >= cols) return;
            grid[r, c] = ch; layer[r, c] = l;
        }

        void Advance()
        {
            if (previewAt.HasValue) return;
            t = (float)clock.Elapsed.TotalSeconds;
            Compute();
        }

        void Compute()
        {
            float hold = 4.5f, outro = 1.2f;
            if (t > introEnd + hold + outro && !previewAt.HasValue) { NextItem(); return; }
            Array.Clear(grid, 0, grid.Length);
            Array.Clear(layer, 0, layer.Length);

            bool outroPhase = t > introEnd + hold;
            float to = t - introEnd - hold;
            foreach (Cell c in cells)
            {
                if (outroPhase)
                {
                    if (to < c.V) Put(c.Row, c.Col, c.Ch, 2);
                    else if (to < c.V + 0.18f) Put(c.Row, c.Col, NoiseChar(), 1);
                    continue;
                }
                switch (effect)
                {
                    case 0: // decrittazione
                        if (t < c.T0) { if (t > c.T0 * 0.3f) Put(c.Row, c.Col, NoiseChar(), 1); }
                        else Put(c.Row, c.Col, c.Ch, 2);
                        break;
                    case 1: // pioggia
                        {
                            float row = c.Y0 + c.Speed * t;
                            if (row >= c.Row) Put(c.Row, c.Col, c.Ch, 2);
                            else Put((int)row, c.Col, NoiseChar(), 1);
                        }
                        break;
                    case 2: // scansione da sinistra
                        {
                            float beam = t * 55f;
                            if (c.Col < beam - 3) Put(c.Row, c.Col, c.Ch, 2);
                            else if (c.Col < beam) Put(c.Row, c.Col, NoiseChar(), 1);
                        }
                        break;
                    default: // dispersione: i caratteri arrivano da punti casuali
                        {
                            float k = Math.Max(0f, Math.Min(1f, (t - c.T0 * 0.4f) / 1.1f));
                            k = 1f - (1f - k) * (1f - k);
                            float startRow = (c.Y0 + rows) % rows;
                            int col = (int)Math.Round(c.X0 + (c.Col - c.X0) * k);
                            int row = (int)Math.Round(startRow + (c.Row - startRow) * k);
                            Put(row, col, k >= 1f ? c.Ch : NoiseChar(), (byte)(k >= 1f ? 2 : 1));
                        }
                        break;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            if (grid == null) return;
            Color dim = Theme.Blend(BackColor, color, 0.55f);
            char[] line = new char[cols];
            for (int r = 0; r < rows; r++)
            {
                for (byte l = 1; l <= 2; l++)
                {
                    bool any = false;
                    for (int c = 0; c < cols; c++)
                    {
                        bool on = layer[r, c] == l;
                        line[c] = on ? grid[r, c] : ' ';
                        any |= on;
                    }
                    if (any)
                        TextRenderer.DrawText(g, new string(line), font, new Point(0, r * cellH), l == 2 ? color : dim,
                            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }
            }

            // didascalia scritta a macchina, con cursore lampeggiante
            float tc = t - introEnd * 0.8f;
            Size fs = TextRenderer.MeasureText(footer, footFont);
            if (tc > 0 && t < introEnd + 4.5f)
            {
                string cap = Items[item][1].ToUpperInvariant();
                int n = Math.Min(cap.Length, (int)(tc * 28));
                string shown = cap.Substring(0, n) + (((int)(t * 2.5f)) % 2 == 0 ? "█" : " ");
                Size sz = TextRenderer.MeasureText(cap + "█", captionFont);
                int y = Math.Min(ClientSize.Height - fs.Height - sz.Height - Theme.S(14), (bigBottom + 2) * cellH);
                TextRenderer.DrawText(g, shown, captionFont, new Point(Math.Max(0, (ClientSize.Width - sz.Width) / 2), y), captionColor, TextFormatFlags.NoPrefix);
            }
            TextRenderer.DrawText(g, footer, footFont, new Point((ClientSize.Width - fs.Width) / 2, ClientSize.Height - fs.Height - Theme.S(6)), footColor, TextFormatFlags.NoPrefix);
        }

        /// <summary>Per le anteprime: mostra l'animazione a un istante preciso.</summary>
        public void SeekForPreview(float seconds)
        {
            previewAt = seconds;
            Layout_();
        }

        float? previewAt;
    }

    /// <summary>Il salvaschermo a tutto schermo.</summary>
    class ScreenSaverForm : Form
    {
        readonly PetsciiSaver saver;
        Point mouseStart;
        bool mouseKnown;

        public ScreenSaverForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            KeyPreview = true;
            saver = new PetsciiSaver(13f, Color.Black, Lang.T("XUMER TOOL  ·  PREMI UN TASTO PER TORNARE")) { Dock = DockStyle.Fill };
            saver.MouseMove += (s, e) => OnSaverMouse(e.Location);
            saver.MouseDown += delegate { Close(); };
            Controls.Add(saver);
        }

        void OnSaverMouse(Point p)
        {
            if (!mouseKnown) { mouseStart = p; mouseKnown = true; return; }
            if (Math.Abs(p.X - mouseStart.X) + Math.Abs(p.Y - mouseStart.Y) > 12) Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Cursor.Hide();
            saver.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            saver.Stop();
            Cursor.Show();
            base.OnFormClosed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e) { Close(); }

        public void SeekForPreview(float seconds) { saver.SeekForPreview(seconds); }
    }

    // =====================================================================

    /// <summary>Tiene traccia dell'ultima azione dell'utente nell'app (tastiera e mouse).</summary>
    class IdleWatcher : IMessageFilter
    {
        public DateTime LastInput = DateTime.Now;
        /// <summary>Scatta a ogni tasto, clic o movimento del mouse nell'app.</summary>
        public event Action Input;
        Point lastMouse;

        void Touch()
        {
            LastInput = DateTime.Now;
            if (Input != null) Input();
        }

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0100: case 0x0104: // WM_KEYDOWN, WM_SYSKEYDOWN
                case 0x0201: case 0x0204: case 0x0207: case 0x020A: // tasti del mouse e rotella
                    Touch();
                    break;
                case 0x0200: case 0x00A0: // WM_MOUSEMOVE, WM_NCMOUSEMOVE
                    Point p = Cursor.Position;
                    if (Math.Abs(p.X - lastMouse.X) + Math.Abs(p.Y - lastMouse.Y) > 3) { lastMouse = p; Touch(); }
                    break;
            }
            return false;
        }
    }
}
