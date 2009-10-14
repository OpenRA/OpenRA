using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class OverlayRenderer
	{
		static string[] overlaySpriteNames =
			{
				"sbag", "cycl", "brik", "fenc", "wood",
				"gold01", "gold02", "gold03", "gold04",
				"gem01", "gem02", "gem03", "gem04",
				"v12", "v13", "v14", "v15", "v16", "v17", "v18",
				"fpls", "wcrate", "scrate", "barb", "sbag",
			};
		static bool[] overlayIsFence =
			{
				true, true, true, true, true,
				false, false, false, false,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, true, true,
			};

		static bool[] overlayIsOre =
			{
				false, false, false, false, false,
				true, true, true, true,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		static bool[] overlayIsGems =
			{
				false, false, false, false, false,
				false, false, false, false,
				true, true, true, true,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
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
						var spriteIndex = 0;
						if( overlayIsFence[ map.MapTiles[ x, y ].overlay ] )
							spriteIndex = NearbyFences( x, y );
						else if( overlayIsOre[ map.MapTiles[ x, y ].overlay ] )
							spriteIndex = 11;
						else if( overlayIsGems[ map.MapTiles[ x, y ].overlay ] )
							spriteIndex = 2;
						spriteRenderer.DrawSprite( sprites[ spriteIndex ], Game.CellSize * (float2)( location - map.Offset ), 0 );
					}
				}
			}

			spriteRenderer.Flush();
		}

		bool IsFence( int x, int y )
		{
			var o = map.MapTiles[ x, y ].overlay;
			if( o < overlayIsFence.Length )
				return overlayIsFence[ o ];
			return false;
		}

		int NearbyFences( int x, int y )
		{
			int ret = 0;
			if( IsFence( x, y - 1 ) )
				ret |= 1;
			if( IsFence( x + 1, y ) )
				ret |= 2;
			if( IsFence( x, y + 1 ) )
				ret |= 4;
			if( IsFence( x - 1, y ) )
				ret |= 8;
			return ret;
		}
	}
}
