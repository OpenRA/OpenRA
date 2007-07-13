using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	static class CoreSheetBuilder
	{
		static TileSheetBuilder<Sheet> builder;
		static Size pageSize = new Size(512,512);

		public static void Initialize(GraphicsDevice device)
		{
			Provider<Sheet> sheetProvider = delegate { return new Sheet(pageSize, device); };
			builder = new TileSheetBuilder<Sheet>(pageSize, sheetProvider);
		}

		public static SheetRectangle<Sheet> Add(byte[] src, Size size)
		{
			SheetRectangle<Sheet> rect = builder.AddImage(size);
			Util.CopyIntoChannel(rect, src);
			return rect;
		}
	}

	static class SpriteSheetBuilder
	{
		static Dictionary<string, SheetRectangle<Sheet>> sprites = 
			new Dictionary<string, SheetRectangle<Sheet>>();

		public static SheetRectangle<Sheet> LoadSprite(Package package, string filename)
		{
			SheetRectangle<Sheet> value;
			if (!sprites.TryGetValue(filename, out value))
			{
				ShpReader shp = new ShpReader(package.GetContent(filename));
				sprites.Add(filename, value = CoreSheetBuilder.Add(shp[0].Image, shp.Size));
			}

			return value;
		}
	}
}
