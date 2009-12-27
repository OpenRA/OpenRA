using System.Linq;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class CursorSheetBuilder
	{
		static Cache<string, Sprite[]> cursors = new Cache<string, Sprite[]>(LoadCursors);
		static readonly string[] exts = { ".shp" };

		static Sprite[] LoadCursors(string filename)
		{
			var shp = new Dune2ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => SheetBuilder.Add(a.Image, a.Size)).ToArray();
		}

		public static Sprite[] LoadAllSprites(string filename) { return cursors[filename]; }
	}
}
