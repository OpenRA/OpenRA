using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class CursorSheetBuilder
	{
		static Dictionary<string, Sprite[]> cursors =
			new Dictionary<string, Sprite[]>();

		public static Sprite LoadSprite(string filename, params string[] exts)
		{
			return LoadAllSprites(filename, exts)[0];
		}

		public static Sprite[] LoadAllSprites(string filename, params string[] exts)
		{
			Sprite[] value;
			if (!cursors.TryGetValue(filename, out value))
			{
				Dune2ShpReader shp = new Dune2ShpReader(FileSystem.OpenWithExts(filename, exts));
				value = new Sprite[shp.ImageCount];
				for (int i = 0; i < shp.ImageCount; i++)
					value[i] = SheetBuilder.Add(shp[i].Image, shp[i].Size);
				cursors.Add(filename, value);
			}

			return value;
		}
	}
}
