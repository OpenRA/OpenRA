using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public static class BitmapBuilder
	{
		public static Bitmap FromBytes( byte[] imageBytes, int width, int height, Palette pal )
		{
			Bitmap bitmap = new Bitmap( width, height );
			for( int x = 0 ; x < width ; x++ )
				for( int y = 0 ; y < height ; y++ )
					bitmap.SetPixel( x, y, pal.GetColor( imageBytes[ x + width * y ] ) );

			return bitmap;
		}
	}
}
