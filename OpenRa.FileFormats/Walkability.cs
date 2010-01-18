using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace OpenRa.FileFormats
{
	public class TileTemplate
	{
		public int Index;
		public string Name;
		public int2 Size;
		public string Bridge;
		public float HP;
		public Dictionary<int, int> TerrainType = new Dictionary<int, int>();
	}

	class Walkability
	{
		public Dictionary<string, TileTemplate> walkability 
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
					Index = int.Parse(section.Name.Substring(3)),
					Bridge = section.GetValue("bridge", null),
					HP = float.Parse(section.GetValue("hp", "0"))
				};

				walkability[tile.Name] = tile;
			}
		}

		public TileTemplate GetWalkability(string terrainName)
		{
			return walkability[terrainName];
		}
	}
}
