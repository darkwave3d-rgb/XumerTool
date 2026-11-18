// Crea un'icona Windows multi-misura (.ico) da un'immagine PNG.
// Uso: IcoFromPng.exe immagine.png app.ico [anteprima.png]
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class IcoFromPng
{
    static Bitmap Resize(Image src, int size)
    {
        Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetWrapMode(WrapMode.TileFlipXY); // niente aloni sui bordi
                g.DrawImage(src, new Rectangle(0, 0, size, size), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
            }
        }
        return b;
    }

    static int Main(string[] args)
    {
        using (Image original = Image.FromFile(args[0]))
        // tela quadrata da 256: l'immagine resta alla sua misura, centrata (nessun ingrandimento sfocato)
        using (Bitmap canvas = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                int w = Math.Min(256, original.Width), h = Math.Min(256, original.Height);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, new Rectangle((256 - w) / 2, (256 - h) / 2, w, h));
            }

            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
            List<byte[]> pngs = new List<byte[]>();
            foreach (int s in sizes)
                using (Bitmap b = s == 256 ? (Bitmap)canvas.Clone() : Resize(canvas, s))
                using (MemoryStream ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                }

            using (BinaryWriter w = new BinaryWriter(File.Create(args[1])))
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

            if (args.Length > 2)
                using (Bitmap prev = new Bitmap(760, 300))
                using (Graphics g = Graphics.FromImage(prev))
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0xF3, 0xF4, 0xF6)), 0, 0, 760, 150);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0x1F, 0x29, 0x37)), 0, 150, 760, 150);
                    int x = 10;
                    foreach (int s in new[] { 16, 24, 32, 48, 64, 128 })
                    {
                        using (Bitmap a = Resize(canvas, s)) { g.DrawImageUnscaled(a, x, 75 - s / 2); g.DrawImageUnscaled(a, x, 225 - s / 2); }
                        x += s + 24;
                    }
                    g.DrawImageUnscaled(canvas, 480, 22);
                    prev.Save(args[2], ImageFormat.Png);
                }
        }
        return 0;
    }
}
