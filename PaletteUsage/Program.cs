using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace PaletteUsage
{
	class Program
	{
		static void Main(string[] args)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.RestoreDirectory = true;
			ofd.Filter = "PNG Image Cache (*.png)|*.png";

			if (DialogResult.OK != ofd.ShowDialog())
				return;

			Bitmap bitmap = new Bitmap(ofd.FileName);
			int[] f = new int[256];

			foreach (byte b in ImageBytes(bitmap))
				++f[b];

			for (int i = 0; i < 256; i++)
			{
				if (i % 8 == 0)
					Console.WriteLine();

				Console.Write("{0} -> {1}", i.ToString().PadLeft(3), f[i].ToString().PadRight(8));
			}
		}

		static IEnumerable<byte> ImageBytes(Bitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;

			for( int i = 0; i < width; i++ )
				for (int j = 0; j < height; j++)
				{
					Color c = bitmap.GetPixel(i, j);
					yield return (byte)c.R;
					yield return (byte)c.G;
					yield return (byte)c.B;
					yield return (byte)c.A;
				}
		}
	}
}
