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

		public TreeCache(GraphicsDevice device, Map map, Package package, Palette pal)
		{
			Size pageSize = new Size(1024, 512);
			List<Sheet> sheets = new List<Sheet>();

			Provider<Sheet> sheetProvider = delegate
			{
				Sheet sheet = new Sheet(new Bitmap(pageSize.Width, pageSize.Height));
				sheets.Add(sheet);
				return sheet;
			};

			TileSheetBuilder<Sheet> builder = new TileSheetBuilder<Sheet>(pageSize, sheetProvider);

			foreach (TreeReference r in map.Trees)
			{
				if (trees.ContainsKey(r.Image))
					continue;

				ShpReader reader = new ShpReader(package.GetContent(r.Image + "." + map.Theater.Substring(0, 3)));
				Bitmap bitmap = BitmapBuilder.FromBytes(reader[0].Image, reader.Width, reader.Height, pal);

				SheetRectangle<Sheet> rect = builder.AddImage(bitmap.Size);
				using (Graphics g = Graphics.FromImage(rect.sheet.bitmap))
					g.DrawImage(bitmap, rect.origin);

				trees.Add(r.Image, rect);
			}

			foreach (Sheet sheet in sheets)
				sheet.LoadTexture(device);

			sh = sheets[0];
		}

		public SheetRectangle<Sheet> GetImage(string tree)
		{
			return trees[tree];
		}
	}
}
