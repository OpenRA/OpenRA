using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Drawing;

namespace OpenRa.Game
{
	class TreeCache
	{
		Dictionary<string, SheetRectangle<Sheet>> trees = new Dictionary<string, SheetRectangle<Sheet>>();

		public readonly Sheet sh;

		public TreeCache(GraphicsDevice device, Map map, Package package)
		{
			Size pageSize = new Size(1024, 512);
			List<Sheet> sheets = new List<Sheet>();

			Provider<Sheet> sheetProvider = delegate
			{
				Sheet sheet = new Sheet(pageSize, device);
				sheets.Add(sheet);
				return sheet;
			};

			TileSheetBuilder<Sheet> builder = new TileSheetBuilder<Sheet>(pageSize, sheetProvider);

			foreach (TreeReference r in map.Trees)
			{
				if (trees.ContainsKey(r.Image))
					continue;

				string filename = r.Image + "." + map.Theater.Substring(0, 3);

				ShpReader reader = new ShpReader(package.GetContent(filename));
				SheetRectangle<Sheet> rect = builder.AddImage(reader.Size);
				Util.CopyIntoChannel(rect.sheet.bitmap, TextureChannel.Red, reader[0].Image, rect);
				trees.Add(r.Image, rect);
			}

			sh = sheets[0];
		}

		public SheetRectangle<Sheet> GetImage(string tree)
		{
			return trees[tree];
		}
	}
}
