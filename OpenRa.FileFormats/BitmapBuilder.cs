using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public static class BitmapBuilder
	{
		public static Bitmap FromBytes(byte[] imageBytes, Size size, Palette pal)
		{
			Bitmap bitmap = new Bitmap(size.Width, size.Height);
			for (int x = 0; x < size.Width; x++)
				for (int y = 0; y < size.Height; y++)
					bitmap.SetPixel(x, y, pal.GetColor(imageBytes[x + size.Width * y]));

			return bitmap;
		}
	}
}
