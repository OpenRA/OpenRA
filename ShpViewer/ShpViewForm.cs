using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ImageDecode;
using System.IO;

namespace ShpViewer
{
	public partial class ShpViewForm : Form
	{
		ShpReader shpReader;
		List<Bitmap> bitmaps = new List<Bitmap>();

		public ShpViewForm( string filename )
		{
			shpReader = new ShpReader( File.OpenRead( filename ) );

			foreach( ImageHeader h in shpReader )
			{
				byte[] imageBytes = h.Image;

				Palette pal = new Palette(File.OpenRead("../../../temperat.pal"));

				Bitmap bitmap = new Bitmap( shpReader.Width, shpReader.Height );
				for( int x = 0 ; x < shpReader.Width ; x++ )
					for( int y = 0 ; y < shpReader.Height ; y++ )
						bitmap.SetPixel( x, y, pal.GetColor(imageBytes[ x + shpReader.Width * y ]) );
				bitmaps.Add( bitmap );
			}

			InitializeComponent();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );

			int y = 10;
			foreach( Bitmap b in bitmaps )
			{
				e.Graphics.DrawImage( b, 10, y );
				y += 10 + b.Height;
			}
		}
	}
}