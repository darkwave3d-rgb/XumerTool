// Genera l'icona dell'applicazione (floppy da 5,25" in stile retro) in tutte le misure di Windows.
// Uso: IconGen.exe app.ico anteprima.png
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

static class IconGen
{
    static GraphicsPath Rounded(RectangleF r, float rad)
    {
        GraphicsPath p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Color H(int rgb) { return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF); }

    static Bitmap Draw(int s)
    {
        Bitmap b = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            float m = s <= 16 ? 0.5f : s * 0.05f;
            RectangleF jacket = new RectangleF(m, m, s - 2 * m - (s <= 16 ? 0 : 0.5f), s - 2 * m - (s <= 16 ? 0 : 0.5f));

            // busta del dischetto
            using (GraphicsPath p = Rounded(jacket, Math.Max(1f, s * 0.07f)))
            {
                using (LinearGradientBrush lg = new LinearGradientBrush(jacket, H(0x343A4A), H(0x14161D), 60f))
                    g.FillPath(lg, p);
                using (Pen pen = new Pen(H(0x6B7591), Math.Max(1f, s / 32f)))
                    g.DrawPath(pen, p);
            }

            // tacca di protezione dalla scrittura (lato destro)
            if (s >= 32)
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clear = new SolidBrush(Color.Transparent))
                    g.FillRectangle(clear, jacket.Right - s * 0.07f, s * 0.22f, s * 0.08f, s * 0.08f);
                g.CompositingMode = CompositingMode.SourceOver;
            }

            // etichetta con le righe colorate
            if (s >= 20)
            {
                RectangleF label = new RectangleF(s * 0.17f, s * 0.10f, s * 0.66f, s * 0.25f);
                using (GraphicsPath lp = Rounded(label, Math.Max(0.5f, s * 0.02f)))
                using (SolidBrush paper = new SolidBrush(H(0xF3EEDC)))
                    g.FillPath(paper, lp);
                int[] stripes = { 0xE5534B, 0xF2A33A, 0xF2D94B, 0x5FBF6A, 0x5B86E5 };
                float sh = label.Height * 0.11f;
                float y0 = label.Bottom - sh * stripes.Length - label.Height * 0.08f;
                for (int i = 0; i < stripes.Length; i++)
                    using (SolidBrush sb = new SolidBrush(H(stripes[i])))
                        g.FillRectangle(sb, label.X + label.Width * 0.06f, y0 + i * sh, label.Width * 0.88f, sh + 0.3f);
                if (s >= 64)
                {
                    using (Font f = new Font("Consolas", label.Height * 0.30f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush ink = new SolidBrush(H(0x2E2C9B)))
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        g.DrawString("1571", f, ink, label.X + label.Width * 0.05f, label.Y + label.Height * 0.03f);
                    }
                }
            }

            // foro centrale con anello di rinforzo
            float cx = s * 0.5f, cy = s * 0.58f;
            float ro = s * (s <= 16 ? 0.20f : 0.165f), ri = s * (s <= 16 ? 0.10f : 0.085f);
            using (SolidBrush ring = new SolidBrush(H(0xB9C0CE)))
                g.FillEllipse(ring, cx - ro, cy - ro, 2 * ro, 2 * ro);
            using (SolidBrush hole = new SolidBrush(H(0x0C0D12)))
                g.FillEllipse(hole, cx - ri, cy - ri, 2 * ri, 2 * ri);

            // foro indice
            if (s >= 40)
                using (SolidBrush idx = new SolidBrush(H(0x8D6E4A)))
                    g.FillEllipse(idx, s * 0.30f - s * 0.022f, cy - s * 0.022f, s * 0.044f, s * 0.044f);

            // finestra di lettura con il disco magnetico visibile
            if (s >= 20)
            {
                RectangleF slot = new RectangleF(s * 0.44f, s * 0.79f, s * 0.12f, s * 0.13f);
                using (GraphicsPath sp = Rounded(slot, slot.Width * 0.45f))
                {
                    using (SolidBrush media = new SolidBrush(H(0x6A4A2C)))
                        g.FillPath(media, sp);
                    using (Pen pen = new Pen(H(0x0C0D12), Math.Max(1f, s / 64f)))
                        g.DrawPath(pen, sp);
                }
            }
            else
            {
                using (SolidBrush media = new SolidBrush(H(0x8A6A44)))
                    g.FillRectangle(media, 7, 13, 2, 2);
            }
        }
        return b;
    }

    static int Main(string[] args)
    {
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        List<byte[]> pngs = new List<byte[]>();
        foreach (int s in sizes)
            using (Bitmap bmp = Draw(s))
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                pngs.Add(ms.ToArray());
            }

        using (BinaryWriter w = new BinaryWriter(File.Create(args[0])))
        {
            w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0); w.Write((byte)0);
                w.Write((short)1); w.Write((short)32);
                w.Write(pngs[i].Length); w.Write(offset);
                offset += pngs[i].Length;
            }
            foreach (byte[] p in pngs) w.Write(p);
        }

        if (args.Length > 1)
        {
            // anteprima: tutte le misure su sfondo chiaro e scuro
            using (Bitmap prev = new Bitmap(760, 300))
            using (Graphics g = Graphics.FromImage(prev))
            {
                g.FillRectangle(new SolidBrush(H(0xF3F4F6)), 0, 0, 760, 150);
                g.FillRectangle(new SolidBrush(H(0x1F2937)), 0, 150, 760, 150);
                int x = 10;
                foreach (int s in new[] { 16, 24, 32, 48, 64, 128 })
                {
                    using (Bitmap a = Draw(s)) { g.DrawImage(a, x, 75 - s / 2); g.DrawImage(a, x, 225 - s / 2); }
                    x += s + 24;
                }
                using (Bitmap big = Draw(256)) g.DrawImage(big, 470, 22, 256, 256);
                prev.Save(args[1], ImageFormat.Png);
            }
        }
        return 0;
    }
}
