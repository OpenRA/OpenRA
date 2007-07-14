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

			unchecked
			{
				Color[] masks = { 
					Color.FromArgb( (int)0xff000000 ), 
					Color.FromArgb( (int)0x00ff0000 ), 
					Color.FromArgb( (int)0x0000ff00 ), 
					Color.FromArgb( (int)0x000000ff ) 
				};

				foreach (Color c in masks)
				{
					Bitmap b = ExtractChannelToBitmap(block, palette, c);
					PictureBox pb = new PictureBox();

					pb.SizeMode = PictureBoxSizeMode.AutoSize;
					pb.Image = b;

					flowLayoutPanel1.Controls.Add(pb);
				}

			}
		}

		int MaskColor(Color c, Color mask)
		{
			int result = 0;
			if (mask.R > 0) result += c.R;
			if (mask.G > 0) result += c.G;
			if (mask.B > 0) result += c.B;
			if (mask.A > 0) result += c.A;

			return result;
		}

		Bitmap ExtractChannelToBitmap(Bitmap src, Bitmap pal, Color mask)
		{
			Bitmap dest = new Bitmap(src.Width, src.Height);

			for( int i = 0; i < src.Width; i++ )
				for (int j = 0; j < src.Height; j++)
				{
					int index = MaskColor(src.GetPixel(i, j), mask);
					dest.SetPixel(i, j, pal.GetPixel(index, 0));
				}

			return dest;
		}
	}
}