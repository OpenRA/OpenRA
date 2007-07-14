using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace OpenRa.BlockCacheVisualizer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
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

				flowLayoutPanel1.Controls.Add(pb);
			}
		}

		int MaskColor(Color c, uint mask)
		{
			uint hax = (uint)c.ToArgb() & mask;

			hax = ( hax & 0xffff ) | (hax >> 16);
			hax = (hax & 0xff) | (hax >> 8);

			return (int)hax;
		}

		Bitmap ExtractChannelToBitmap(Bitmap src, Bitmap pal, uint mask)
		{
			Bitmap dest = new Bitmap(src.Width / 2, src.Height / 2);

			for( int i = 0; i < dest.Width; i++ )
				for (int j = 0; j < dest.Height; j++)
				{
					int index = MaskColor(src.GetPixel(2 * i, 2 * j), mask);
					dest.SetPixel(i, j, pal.GetPixel(index, 0));
				}

			return dest;
		}
	}
}