using System.Linq;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	static class SpriteSheetBuilder
	{
		public static void Initialize()
		{
			sprites = new Cache<string, Sprite[]>( LoadSprites );
		}

		static Cache<string, Sprite[]> sprites;
		static readonly string[] exts = { ".tem", ".sno", ".int", ".shp" };

		static Sprite[] LoadSprites(string filename)
		{
			var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => SheetBuilder.Add(a.Image, shp.Size)).ToArray();
		}

		public static Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
