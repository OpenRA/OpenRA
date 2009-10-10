using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class OverlayRenderer
	{
		static string[] overlaySpriteNames = new string[]
			{
				"sbag", "cycl", "brik", "fenc", "wood",
				"gold01", "gold02", "gold03", "gold04",
				"gem01", "gem02", "gem03", "gem04",
				"v12", "v13", "v14", "v15", "v16", "v17", "v18",
				"fpls", "wcrate", "scrate", "barb", "sbag"
			};
		Sprite[][] overlaySprites;

		SpriteRenderer spriteRenderer;
		Map map;

		public OverlayRenderer( Renderer renderer, Map map )
		{
			this.spriteRenderer = new SpriteRenderer( renderer, true );
			this.map = map;

			overlaySprites = new Sprite[ overlaySpriteNames.Length ][];
			for( int i = 0 ; i < overlaySpriteNames.Length ; i++ )
				overlaySprites[ i ] = SpriteSheetBuilder.LoadAllSprites( overlaySpriteNames[ i ], ".shp", ".tem", ".sno" );
		}

		public void Draw()
		{
			for( int y = 0 ; y < 128 ; y++ )
			{
				for( int x = 0 ; x < 128 ; x++ )
				{
					if( map.MapTiles[ x, y ].overlay < overlaySprites.Length )
					{
						var location = new int2( x, y );
						var sprites = overlaySprites[ map.MapTiles[ x, y ].overlay ];
						spriteRenderer.DrawSprite( sprites[ sprites.Length / 2-1 ], 24 * (float2)( location - map.Offset ), 0 );
					}
				}
			}

			spriteRenderer.Flush();
		}
	}
}
