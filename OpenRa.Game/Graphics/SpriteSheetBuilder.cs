using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class SpriteSheetBuilder
	{
		public static void Initialize()
		{
			sprites = new Dictionary<string, Sprite[]>();
		}

		static Dictionary<string, Sprite[]> sprites;

		public static Sprite[] LoadAllSprites( string filename, params string[] exts )
		{
			Sprite[] value;
			if( !sprites.TryGetValue( filename, out value ) )
			{
				ShpReader shp = new ShpReader( FileSystem.OpenWithExts( filename, exts ) );
				value = new Sprite[ shp.ImageCount ];
				for( int i = 0 ; i < shp.ImageCount ; i++ )
					value[ i ] = SheetBuilder.Add( shp[ i ].Image, shp.Size );
				sprites.Add( filename, value );
			}

			return value;
		}
	}
}
