using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class TreeCache
	{
		Dictionary<string, Sprite> trees = new Dictionary<string, Sprite>();

		public TreeCache(Map map)
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
