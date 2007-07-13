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

		public TreeCache(GraphicsDevice device, Map map, Package package)
		{
			foreach (TreeReference r in map.Trees)
			{
				if (trees.ContainsKey(r.Image))
					continue;

				string filename = r.Image + "." + map.Theater.Substring(0, 3);

				ShpReader reader = new ShpReader(package.GetContent(filename));
				trees.Add(r.Image, CoreSheetBuilder.Add(reader[0].Image, reader.Size));
			}
		}

		public SheetRectangle<Sheet> GetImage(string tree) { return trees[tree]; }
	}
}
