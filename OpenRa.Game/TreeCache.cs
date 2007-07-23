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
		Dictionary<string, Sprite> trees = new Dictionary<string, Sprite>();

		public TreeCache(GraphicsDevice device, Map map)
		{
			foreach (TreeReference r in map.Trees)
			{
				if (trees.ContainsKey(r.Image))
					continue;

				string filename = r.Image + "." + map.Theater.Substring(0, 3);

				ShpReader reader = new ShpReader( FileSystem.Open( filename ) );
				trees.Add(r.Image, SheetBuilder.Add(reader[0].Image, reader.Size));
			}
		}

		public Sprite GetImage(string tree) { return trees[tree]; }
	}
}
