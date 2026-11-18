// Ridisegna l'icona del floppy 5¼" di darkwave3D in versione più leggibile, con le stesse proporzioni.
// Il floppy è vettoriale (coordinate misurate sul disegno originale da 242 px); la scritta "xumTools"
// e la firma a mano "Remember..." vengono estratte dal disegno originale e riapplicate così come sono.
// Uso: IconMaker.exe originale.png cartella_uscita
//   → icon-256.png, app.ico (misure 16…256 disegnate una per una), anteprima.png
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class IconMaker
{
    static Color H(int rgb) { return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF); }
    static Color A(int a, Color c) { return Color.FromArgb(a, c); }

    // ---------- geometria (unità = pixel del disegno originale, corpo da 0.5 a 240.5) ----------
    static readonly RectangleF Body = new RectangleF(0.5f, 0.5f, 240f, 240f);
    const float BodyRadius = 5f;
    static readonly RectangleF Label = new RectangleF(9f, 9.5f, 109.5f, 55f);
    const float LabelRadius = 3.5f;
    static readonly float[] BarTop = { 30f, 34.6f, 39.2f, 43.8f, 48.4f };
    const float BarHeight = 4.2f, BarLeft = 21.5f, BarRight = 104.5f;
    static readonly Color[] BarColor = { H(0xE5534B), H(0xF2A33A), H(0xF2D94B), H(0x5CB967), H(0x5B8ADD) };
    static readonly PointF Hub = new PointF(120.5f, 123.5f);
    const float HubOuter = 34.5f, HubHole = 28.5f;
    static readonly PointF Index = new PointF(162.5f, 141f);
    const float IndexOuter = 6.5f, IndexDot = 2.2f;
    static readonly RectangleF Slot = new RectangleF(109.5f, 168f, 22f, 60f);
    static readonly RectangleF Notch = new RectangleF(234.5f, 44f, 7f, 10f);

    // posizione del corpo nella tela da 256 (margine per l'ombra)
    const float CanvasX = 5f, CanvasY = 4f, CanvasScale = 246f / 240f;

    static GraphicsPath Rounded(RectangleF r, float rad)
    {
        GraphicsPath p = new GraphicsPath();
        rad = Math.Min(rad, Math.Min(r.Width, r.Height) / 2);
        if (rad < 0.3f) { p.AddRectangle(r); return p; }
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>Tacca di protezione dalla scrittura (lato destro) e le due piccole tacche in basso.</summary>
    static GraphicsPath Cuts()
    {
        GraphicsPath cut = new GraphicsPath(FillMode.Winding);
        cut.AddPath(Rounded(Notch, 1.2f), false);
        cut.AddEllipse(98.5f, 239f, 3f, 3f);
        cut.AddEllipse(138.5f, 239f, 3f, 3f);
        return cut;
    }

    static GraphicsPath Circle(PointF c, float r)
    {
        GraphicsPath p = new GraphicsPath();
        p.AddEllipse(c.X - r, c.Y - r, 2 * r, 2 * r);
        return p;
    }

    // ---------- inchiostro (scritte) estratto dall'originale ----------
    static float Lum(Color c) { return 0.299f * c.R + 0.587f * c.G + 0.114f * c.B; }

    static float Median(List<float> v) { v.Sort(); return v.Count == 0 ? 0 : v[v.Count / 2]; }

    /// <summary>Maschera alfa delle scritte (xumTools e Remember...) nell'etichetta del disegno originale.</summary>
    static Bitmap ExtractInk(Bitmap src)
    {
        int w = src.Width, h = src.Height;
        float[,] a = new float[w, h];
        const float inkLum = 38f;
        for (int y = 11; y <= 63; y++)
        {
            List<float> margin = new List<float>();
            for (int x = 10; x <= 20; x++) margin.Add(Lum(src.GetPixel(x, y)));
            float labelBg = Median(margin);
            List<float> bar = new List<float>();
            bool barRow = y >= 30 && y <= 53;
            if (barRow) for (int x = 24; x <= 101; x++) bar.Add(Lum(src.GetPixel(x, y)));
            float barBg = barRow ? Median(bar) : labelBg;
            for (int x = 10; x <= 117; x++)
            {
                Color c = src.GetPixel(x, y);
                bool inBar = barRow && x >= 24 && x <= 101;
                float bg = inBar ? barBg : labelBg;
                float v = (bg - Lum(c)) / Math.Max(20f, bg - inkLum);
                if (!inBar)
                {
                    // fuori dalle barre l'inchiostro è grigio neutro: i bordi colorati delle barre non contano
                    int sat = Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));
                    v *= 1f - Math.Max(0f, Math.Min(1f, (sat - 22) / 30f));
                }
                if (v < 0.16f) v = 0;
                a[x, y] = Math.Min(1f, v);
            }
        }
        // leggero ispessimento dei tratti sottili per la leggibilità
        Bitmap ink = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = a[x, y];
                if (x > 0 && x < w - 1 && y > 0 && y < h - 1)
                {
                    float n = Math.Max(Math.Max(a[x - 1, y], a[x + 1, y]), Math.Max(a[x, y - 1], a[x, y + 1]));
                    v = Math.Max(v, n * 0.42f);
                }
                v = Math.Min(1f, v * 1.25f);
                ink.SetPixel(x, y, Color.FromArgb((int)(v * 255), 0x16, 0x16, 0x18));
            }
        return ink;
    }

    static Bitmap Noise(int seed, int amp)
    {
        Random r = new Random(seed);
        Bitmap b = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int v = r.Next(-amp, amp + 1);
                b.SetPixel(x, y, v >= 0 ? Color.FromArgb(v, 255, 255, 255) : Color.FromArgb(-v, 0, 0, 0));
            }
        return b;
    }

    // ---------- disegno ----------
    /// <summary>Disegna l'icona alla misura n. detail = scritte e grana (misure grandi).</summary>
    static Bitmap Render(int n, Bitmap ink)
    {
        bool detail = n >= 64, grain = n >= 128;
        int ss = n <= 48 ? 8 : 4;
        int N = n * ss;
        Bitmap big = new Bitmap(N, N, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(big))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            float k = N / 256f;
            // linee: almeno ~1 pixel finale anche nelle misure piccole
            float px = 256f / n / CanvasScale; // 1 pixel finale in unità del disegno
            g.ScaleTransform(k, k);
            g.TranslateTransform(CanvasX, CanvasY);
            g.ScaleTransform(CanvasScale, CanvasScale);
            g.TranslateTransform(-0.5f, -0.5f);

            using (GraphicsPath body = Rounded(Body, BodyRadius))
            using (GraphicsPath cuts = Cuts())
            using (Region bodyRegion = new Region(body))
            {
                bodyRegion.Exclude(cuts);

                // ombra morbida sotto il disco
                for (int i = 4; i >= 1; i--)
                {
                    float o = i * 0.9f;
                    using (GraphicsPath sh = Rounded(new RectangleF(Body.X - o * 0.4f, Body.Y + o * 0.9f, Body.Width + o * 0.8f, Body.Height + o * 0.6f), BodyRadius + o))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(14, 0, 0, 0))) g.FillPath(b, sh);
                }

                g.SetClip(bodyRegion, CombineMode.Replace);
                // custodia: blu notte, un po' più chiara dell'originale per staccare dagli sfondi scuri
                using (LinearGradientBrush b = new LinearGradientBrush(new PointF(0, 0), new PointF(241, 241), H(0x394052), H(0x222734)))
                    g.FillRectangle(b, 0, 0, 242, 242);
                if (grain)
                    using (Bitmap nz = Noise(7, 9))
                    using (TextureBrush tb = new TextureBrush(nz))
                        g.FillRectangle(tb, 0, 0, 242, 242);
                // riflesso tenue in alto
                using (LinearGradientBrush b = new LinearGradientBrush(new PointF(0, 0), new PointF(0, 120), Color.FromArgb(22, 255, 255, 255), Color.FromArgb(0, 255, 255, 255)))
                    g.FillRectangle(b, 0, 0, 242, 120);

                // etichetta argentata (con ombra sottile)
                using (GraphicsPath lsh = Rounded(new RectangleF(Label.X, Label.Y + 0.9f, Label.Width, Label.Height), LabelRadius))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(b, lsh);
                using (GraphicsPath lab = Rounded(Label, LabelRadius))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new PointF(0, Label.Top), new PointF(0, Label.Bottom), H(0xDCDDE0), H(0xC4C6CA)))
                        g.FillPath(b, lab);
                    if (grain)
                    {
                        Region old = g.Clip;
                        g.SetClip(lab, CombineMode.Intersect);
                        using (Bitmap nz = Noise(11, 10))
                        using (TextureBrush tb = new TextureBrush(nz))
                            g.FillRectangle(tb, Label);
                        g.Clip = old;
                    }
                    // barre arcobaleno
                    for (int i = 0; i < 5; i++)
                        using (GraphicsPath bar = Rounded(new RectangleF(BarLeft, BarTop[i], BarRight - BarLeft, BarHeight), BarHeight / 2 - 0.01f))
                        using (SolidBrush b = new SolidBrush(BarColor[i])) g.FillPath(b, bar);
                    // scritte originali
                    if (detail && ink != null)
                        g.DrawImage(ink, new RectangleF(0, 0, ink.Width, ink.Height));
                    using (Pen pen = new Pen(H(0x8C9096), Math.Max(0.7f, px * 0.6f))) g.DrawPath(pen, lab);
                    using (Pen pen = new Pen(Color.FromArgb(150, 255, 255, 255), Math.Max(0.6f, px * 0.5f)))
                        g.DrawLine(pen, Label.X + LabelRadius, Label.Y + 0.6f, Label.Right - LabelRadius, Label.Y + 0.6f);
                }

                // mozzo: anello del disco magnetico e foro centrale trasparente
                Color edge = H(0x8A90A0);
                using (GraphicsPath hub = Circle(Hub, HubOuter))
                {
                    using (SolidBrush b = new SolidBrush(H(0x1B1B1F))) g.FillPath(b, hub);
                    using (GraphicsPath ring = Circle(Hub, (HubOuter + HubHole) / 2))
                    using (Pen pen = new Pen(H(0x2C2C32), Math.Max(0.8f, px * 0.7f))) g.DrawPath(pen, ring);
                    using (Pen pen = new Pen(edge, Math.Max(0.9f, px))) g.DrawPath(pen, hub);
                }
                using (GraphicsPath hole = Circle(Hub, HubHole))
                {
                    CompositingMode cm = g.CompositingMode;
                    g.CompositingMode = CompositingMode.SourceCopy;
                    using (SolidBrush b = new SolidBrush(Color.Transparent)) g.FillPath(b, hole);
                    g.CompositingMode = cm;
                    using (Pen pen = new Pen(H(0x55585F), Math.Max(0.8f, px * 0.8f))) g.DrawPath(pen, hole);
                }

                // foro indice
                using (GraphicsPath ih = Circle(Index, IndexOuter))
                {
                    using (SolidBrush b = new SolidBrush(H(0x18181B))) g.FillPath(b, ih);
                    using (Pen pen = new Pen(edge, Math.Max(0.8f, px * 0.8f))) g.DrawPath(pen, ih);
                }
                using (GraphicsPath dot = Circle(Index, Math.Max(IndexDot, px * 0.8f)))
                using (SolidBrush b = new SolidBrush(H(0xF4F4F4))) g.FillPath(b, dot);

                // finestra della testina
                using (GraphicsPath slot = Rounded(Slot, Slot.Width / 2 - 0.01f))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new PointF(0, Slot.Top), new PointF(0, Slot.Bottom), H(0x26262A), H(0x1C1C20)))
                        g.FillPath(b, slot);
                    using (Pen pen = new Pen(edge, Math.Max(0.8f, px * 0.8f))) g.DrawPath(pen, slot);
                }

                // bordo: luce in alto a sinistra (stacca dagli sfondi scuri), filo scuro all'esterno (sfondi chiari)
                g.ResetClip();
                float rim = Math.Max(1.4f, px * 1.1f);
                using (GraphicsPath inner = Rounded(new RectangleF(Body.X + rim / 2, Body.Y + rim / 2, Body.Width - rim, Body.Height - rim), BodyRadius - rim / 2))
                using (LinearGradientBrush lb = new LinearGradientBrush(new PointF(0, 0), new PointF(241, 241), H(0x7C859A), H(0x454C5C)))
                using (Pen pen = new Pen(lb, rim))
                {
                    g.SetClip(bodyRegion, CombineMode.Replace);
                    g.DrawPath(pen, inner);
                    g.ResetClip();
                }
                using (Pen pen = new Pen(Color.FromArgb(200, 0x0C, 0x0E, 0x14), Math.Max(0.8f, px * 0.7f)))
                {
                    Region outside = new Region(new RectangleF(-10, -10, 262, 262));
                    outside.Exclude(cuts);
                    g.SetClip(outside, CombineMode.Replace);
                    g.DrawPath(pen, body);
                    g.ResetClip();
                    outside.Dispose();
                }
                // bordi delle tacche
                using (Pen pen = new Pen(H(0x6E7688), Math.Max(0.8f, px * 0.7f)))
                {
                    g.SetClip(bodyRegion, CombineMode.Replace);
                    g.DrawPath(pen, cuts);
                    g.ResetClip();
                }
            }
        }
        Bitmap res = new Bitmap(n, n, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(res))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(big, new Rectangle(0, 0, n, n), 0, 0, N, N, GraphicsUnit.Pixel, ia);
            }
        }
        big.Dispose();
        return res;
    }

    static int Main(string[] args)
    {
        string outDir = args[1];
        Directory.CreateDirectory(outDir);
        Bitmap original = new Bitmap(args[0]);
        Bitmap ink = ExtractInk(original);
        ink.Save(Path.Combine(outDir, "ink.png"), ImageFormat.Png);

        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
        Dictionary<int, Bitmap> renders = new Dictionary<int, Bitmap>();
        foreach (int s in sizes) renders[s] = Render(s, ink);
        renders[256].Save(Path.Combine(outDir, "icon-256.png"), ImageFormat.Png);

        List<byte[]> pngs = new List<byte[]>();
        foreach (int s in sizes)
            using (MemoryStream ms = new MemoryStream()) { renders[s].Save(ms, ImageFormat.Png); pngs.Add(ms.ToArray()); }
        using (BinaryWriter w = new BinaryWriter(File.Create(Path.Combine(outDir, "app.ico"))))
        {
            w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
                w.Write(pngs[i].Length); w.Write(offset);
                offset += pngs[i].Length;
            }
            foreach (byte[] p in pngs) w.Write(p);
        }

        // anteprima: originale e nuova, su sfondo chiaro, scuro e molto scuro (barra di Windows)
        Color[] backs = { H(0xF3F4F6), H(0x1F2937), H(0x101014) };
        using (Bitmap prev = new Bitmap(1100, 3 * 300))
        using (Graphics g = Graphics.FromImage(prev))
        {
            for (int r = 0; r < 3; r++)
            {
                using (SolidBrush b = new SolidBrush(backs[r])) g.FillRectangle(b, 0, r * 300, 1100, 300);
                g.DrawImage(original, 14, r * 300 + 22, original.Width, original.Height);
                g.DrawImageUnscaled(renders[256], 270, r * 300 + 18);
                int x = 545;
                foreach (int s in new[] { 16, 24, 32, 48, 64, 128 })
                {
                    g.DrawImageUnscaled(renders[s], x, r * 300 + 150 - s / 2);
                    x += s + 22;
                }
            }
            prev.Save(Path.Combine(outDir, "anteprima.png"), ImageFormat.Png);
        }
        return 0;
    }
}
