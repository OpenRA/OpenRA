using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.Drawing;
using System.IO;

namespace ShpViewer
{
	public class MapViewControl : Control
	{
		public int XScroll, YScroll;

		Map map;
		public Map Map
		{
			get { return map; }
			set
			{
				map = value;
				TileSet = LoadTileSet( Map );
			}
		}

		Palette pal;
		TileSet TileSet;
		Package TileMix;
		string TileSuffix;
		Dictionary<string, Bitmap> TreeCache = new Dictionary<string, Bitmap>();

		public MapViewControl()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			UpdateStyles();
		}

		static Font font = new Font( FontFamily.GenericMonospace, 10 );
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (Map == null || TileSet == null)
				return;

			Graphics g = e.Graphics;

			for (int x = 55; x >= 0; x--)
			{
				int tX = x + Map.XOffset + XScroll;
				if (tX < Map.XOffset || tX >= Map.XOffset + Map.Width)
					continue;

				for (int y = 50; y >= 0; y--)
				{
					int tY = y + Map.YOffset + YScroll;
					if (tY < Map.YOffset || tY >= Map.YOffset + Map.Height)
						continue;

					Terrain t;
					if (TileSet.tiles.TryGetValue(Map.MapTiles[tX, tY].tile, out t))
					{
						Bitmap b = t.GetTile(Map.MapTiles[tX, tY].image);
						if (b == null)
						{
							g.FillRectangle(Brushes.Blue, x * 24, y * 24, 24, 24);
							g.DrawString(string.Format("{0:x}", Map.MapTiles[tX, tY].image),
								font, Brushes.White, x * 24, y * 24);
						}
						else
							g.DrawImage(b, x * 24, y * 24);
					}
					else
					{
						g.FillRectangle(Brushes.Red, x * 24, y * 24, 24, 24);
						g.DrawString(string.Format("{0:x}", Map.MapTiles[tX, tY].tile),
							font, Brushes.White, x * 24, y * 24);
					}
				}
			}

			foreach( TreeReference tr in Map.Trees )
			{
				int tX = tr.X - Map.XOffset - XScroll;
				int tY = tr.Y - Map.YOffset - YScroll;
				g.DrawImage( GetTree( tr.Image, TileMix ), tX * 24, tY * 24 );
			}
		}

		Bitmap GetTree( string name, Package mix )
		{
			Bitmap ret;
			if( !TreeCache.TryGetValue( name, out ret ) )
			{
				ShpReader shp = new ShpReader( TileSet.MixFile.GetContent( name + TileSuffix ) );
				ret = BitmapBuilder.FromBytes( shp[ 0 ].Image, shp.Width, shp.Height, pal ); ;
				TreeCache.Add( name, ret );
			}
			return ret;
		}

		TileSet LoadTileSet( Map currentMap )
		{
			switch( currentMap.Theater.ToLowerInvariant() )
			{
				case "temperate":
					pal = new Palette( File.OpenRead( "../../../temperat.pal" ) );
					TileMix = new Package( "../../../temperat.mix" );
					TileSuffix = ".tem";
					break;
				case "snow":
					pal = new Palette( File.OpenRead( "../../../snow.pal" ) );
					TileMix = new Package( "../../../snow.mix" );
					TileSuffix = ".sno";
					break;
				case "interior":
					pal = new Palette( File.OpenRead( "../../../interior.pal" ) );
					TileMix = new Package( "../../../interior.mix" );
					TileSuffix = ".int";
					break;
				default:
					throw new NotImplementedException();
			}
			return new TileSet( TileMix, TileSuffix, pal );
		}
	}
}
