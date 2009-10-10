using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class SpriteSheetBuilder
	{
		static Dictionary<string, Sprite[]> sprites =
			new Dictionary<string, Sprite[]>();

		public static Sprite LoadSprite(string filename, params string[] exts )
		{
			return LoadAllSprites( filename, exts )[ 0 ];
		}

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
