using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.Drawing;

namespace ShpViewer
{
	public class MapViewControl : Control
	{
		public int XScroll, YScroll;

		public Map Map;
		public TileSet TileSet;

		public MapViewControl()
		{
		}

		static Font font = new Font( FontFamily.GenericMonospace, 10 );
		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );
			if( Map == null || TileSet == null )
				return;
			using( Graphics g = e.Graphics )
			{
				for( int x = 50 ; x >= 0 ; x-- )
				{
					int tX = x + Map.XOffset + XScroll;
					if( tX < Map.XOffset || tX >= Map.XOffset + Map.Width )
						continue;

					for( int y = 50 ; y >= 0 ; y-- )
					{
						int tY = y + Map.YOffset + YScroll;
						if( tY < Map.YOffset || tY >= Map.YOffset + Map.Height )
							continue;
						
						Terrain t;
						if( TileSet.tiles.TryGetValue( Map.MapTiles[ tX, tY ].tile, out t ) )
						{
							Bitmap b = t.GetTile( Map.MapTiles[ tX, tY ].image );
							if( b == null )
							{
								g.FillRectangle( Brushes.Blue, x * 24, y * 24, 24, 24 );
								g.DrawString( string.Format( "{0:x}", Map.MapTiles[ tX, tY ].image ),
									font, Brushes.White, x * 24, y * 24 );
							}
							else
								g.DrawImage( b, x * 24, y * 24 );
						}
						else
						{
							g.FillRectangle( Brushes.Red, x * 24, y * 24, 24, 24 );
							g.DrawString( string.Format( "{0:x}", Map.MapTiles[ tX, tY ].tile ),
								font, Brushes.White, x * 24, y * 24 );
						}
					}
				}
			}
		}
	}
}
