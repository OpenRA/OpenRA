using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace OpenRa.FileFormats
{
	public class TileTemplate
	{
		public int Index; // not valid for `interior` stuff. only used for bridges.
		public string Name;
		public int2 Size;
		public string Bridge;
		public float HP;
		public Dictionary<int, int> TerrainType = new Dictionary<int, int>();
	}

	public class Walkability
	{
		Dictionary<string, TileTemplate> walkability 
			= new Dictionary<string,TileTemplate>();

		public Walkability()
		{
			var file = new IniFile( FileSystem.Open( "templates.ini" ) );

			foreach (var section in file.Sections)
			{
				var tile = new TileTemplate
				{
					Size = new int2(
						int.Parse(section.GetValue("width", "0")),
						int.Parse(section.GetValue("height", "0"))),
					TerrainType = section
						.Where(p => p.Key.StartsWith("tiletype"))
						.ToDictionary(
							p => int.Parse(p.Key.Substring(8)),
							p => int.Parse(p.Value)),
					Name = section.GetValue("Name", null).ToLowerInvariant(),
					Bridge = section.GetValue("bridge", null),
					HP = float.Parse(section.GetValue("hp", "0"))
				};
				tile.Index = -1;
				int.TryParse(section.Name.Substring(3), out tile.Index);

				walkability[tile.Name] = tile;
			}
		}

		public TileTemplate GetWalkability(string terrainName)
		{
			return walkability[terrainName];
		}
	}
}
