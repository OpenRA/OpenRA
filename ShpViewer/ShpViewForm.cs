using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using OpenRa.FileFormats;

namespace ShpViewer
{
	public partial class ShpViewForm : Form
	{
		List<Bitmap> bitmaps = new List<Bitmap>();

		public ShpViewForm( string filename )
		{
			InitializeComponent();

			string ext = Path.GetExtension( filename ).ToLowerInvariant();
			if( ext == ".shp" )
			{
				ShpReader shpReader = new ShpReader( File.OpenRead( filename ) );

				Palette pal = new Palette( File.OpenRead( "../../../temperat.pal" ) );

				foreach( ImageHeader h in shpReader )
					bitmaps.Add( BitmapBuilder.FromBytes( h.Image, shpReader.Width, shpReader.Height, pal ) );
			}
			else if( ext == ".tem" || ext == ".sno" || ext == ".int" )
			{
				Palette pal = new Palette( File.OpenRead( "../../../temperat.pal" ) );
				switch( ext )
				{
					case ".sno":
						pal = new Palette( File.OpenRead( "../../../snow.pal" ) );
						break;
					case ".int":
						pal = new Palette( File.OpenRead( "../../../interior.pal" ) );
						break;
				}

				Terrain t = new Terrain( File.OpenRead( filename ), pal );

				Bitmap bigTile = new Bitmap( 24 * t.XDim, 24 * t.YDim );
				using( Graphics g = Graphics.FromImage( bigTile ) )
				{
					for( int x = 0 ; x < t.XDim ; x++ )
						for( int y = 0 ; y < t.YDim ; y++ )
							g.DrawImageUnscaled( t.GetTile( x + y * t.XDim ) ?? new Bitmap( 24, 24 ), x * 24, y * 24 );
				}
				bitmaps.Add( bigTile );
			}
			else if( ext == ".ini" || ext == ".mpr" )
			{
				IniFile iniFile = new IniFile( File.OpenRead( filename ) );
				Map map = new Map( iniFile );
				TileSet tileSet = LoadTileSet( map );

				flowLayoutPanel1.Visible = false;
				flowLayoutPanel1.BackColor = Color.Blue;
				mapViewControl1.Visible = true;
				mapViewControl1.Map = map;
				mapViewControl1.TileSet = tileSet;
				mapViewControl1.Invalidate();

				int ux = 0, uy = 0;
				int vx = 0, vy = 0;

				mapViewControl1.MouseDown += delegate(object sender, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Right)
					{
						ux = e.X;
						uy = e.Y;

						vx = mapViewControl1.XScroll;
						vy = mapViewControl1.YScroll;

						mapViewControl1.Cursor = Cursors.NoMove2D;
					}
				};

				mapViewControl1.MouseMove += delegate(object sender, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Right)
					{
						int dx = ux - e.X;
						int dy = uy - e.Y;

						mapViewControl1.XScroll = vx + dx / 24;
						mapViewControl1.YScroll = vy + dy / 24;

						mapViewControl1.Invalidate();
					}
				};

				mapViewControl1.MouseUp += delegate { mapViewControl1.Cursor = Cursors.Default; };

				mapViewControl1.MouseClick += delegate( object sender, MouseEventArgs e )
				{
					if( e.Button == MouseButtons.Left )
					{
						mapViewControl1.Map = new Map( iniFile );
						mapViewControl1.TileSet = LoadTileSet( map );
					}
					mapViewControl1.Invalidate();
				};
			}

			foreach (Bitmap b in bitmaps)
			{
				PictureBox p = new PictureBox();
				p.Image = b;
				p.Size = b.Size;
				flowLayoutPanel1.Controls.Add(p);
			}

			Focus();
			BringToFront();
		}

		TileSet LoadTileSet( Map currentMap )
		{
			Palette pal;
			switch( currentMap.Theater.ToLowerInvariant() )
			{
				case "temperate":
					pal = new Palette( File.OpenRead( "../../../temperat.pal" ) );
					return new TileSet( new Package( "../../../temperat.mix" ), ".tem", pal );
				case "snow":
					pal = new Palette( File.OpenRead( "../../../snow.pal" ) );
					return new TileSet( new Package( "../../../snow.mix" ), ".sno", pal );
				case "interior":
					pal = new Palette( File.OpenRead( "../../../interior.pal" ) );
					return new TileSet( new Package( "../../../interior.mix" ), ".int", pal );
			}

			throw new NotImplementedException();
		}
	}
}
