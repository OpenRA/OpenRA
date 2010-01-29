using System.Linq;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	static class SpriteSheetBuilder
	{
		public static void Initialize( Map map )
		{
			exts = new[] {
				"." + map.Theater.Substring( 0, 3 ).ToLowerInvariant(),
				".shp",
				".tem",
				".sno",
				".int" };
			sprites = new Cache<string, Sprite[]>( LoadSprites );
		}

		static Cache<string, Sprite[]> sprites;
		static string[] exts;

		static Sprite[] LoadSprites(string filename)
		{
			var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => SheetBuilder.Add(a.Image, shp.Size)).ToArray();
		}

		public static Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
