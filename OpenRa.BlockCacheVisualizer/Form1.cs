using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace OpenRa.BlockCacheVisualizer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			ClientSize = new Size(525,525);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Text = "PNG Block Cache Visualizer";
			Visible = true;

			OpenFileDialog d = new OpenFileDialog();
			d.RestoreDirectory = true;
			d.Filter = "OpenRA PNG Block Cache (*.png)|*.png";

			if (DialogResult.OK != d.ShowDialog())
				return;

			

			string filename = d.FileName;
			string palname = Path.GetDirectoryName(filename) + "\\palette-cache.png";

			Bitmap palette = new Bitmap(palname);
			Bitmap block = new Bitmap(filename);

			uint[] masks = { 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000 };

			foreach (uint c in masks)
			{
				Bitmap b = ExtractChannelToBitmap(block, palette, c);
				PictureBox pb = new PictureBox();

				pb.SizeMode = PictureBoxSizeMode.AutoSize;
				pb.Image = b;
				pb.BackColor = Color.White;

				flowLayoutPanel1.Controls.Add(pb);
			}
		}

		uint MaskColor(uint c, uint mask)
		{
			uint hax = c & mask;

			hax = (hax & 0xffff) | (hax >> 16);
			return (hax & 0xff) | (hax >> 8);
		}

		static BitmapData Lock(Bitmap b, ImageLockMode mode)
		{
			return b.LockBits(new Rectangle(new Point(), b.Size), mode, b.PixelFormat);
		}

		Bitmap ExtractChannelToBitmap(Bitmap src, Bitmap pal, uint mask)
		{
			Bitmap dest = new Bitmap(src.Width / 2, src.Height / 2, pal.PixelFormat);

			BitmapData destData = Lock(dest, ImageLockMode.WriteOnly);
			BitmapData paletteData = Lock(pal, ImageLockMode.ReadOnly);
			BitmapData srcData = Lock(src, ImageLockMode.ReadOnly);

			int destStride = destData.Stride/4;
			int srcStride = srcData.Stride/4;

			unsafe
			{
				uint* pdest = (uint*)destData.Scan0.ToPointer();
				uint* ppal = (uint*)paletteData.Scan0.ToPointer();
				uint* psrc = (uint*)srcData.Scan0.ToPointer();

				int h = dest.Height; int w = dest.Width;

				for (int j = 0; j < h; j++)
					for (int i = 0; i < w; i++)
					{
						uint srcc = psrc[2 * j * srcStride + 2 * i];
						uint index = MaskColor(srcc, mask);
						uint data = ppal[index];
						pdest[j * destStride + i] = data;
					}
			}

			dest.UnlockBits(destData);
			pal.UnlockBits(paletteData);
			src.UnlockBits(srcData);

			return dest;
		}
	}
}