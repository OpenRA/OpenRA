using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class SpriteSheetBuilder
	{
		static Dictionary<string, Sprite> sprites =
			new Dictionary<string, Sprite>();

		public static Sprite LoadSprite(string filename)
		{
			Sprite value;
			if (!sprites.TryGetValue(filename, out value))
			{
				ShpReader shp = new ShpReader(FileSystem.Open(filename));
				sprites.Add(filename, value = SheetBuilder.Add(shp[0].Image, shp.Size));
			}

			return value;
		}
	}
}
