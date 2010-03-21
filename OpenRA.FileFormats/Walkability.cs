#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System;
namespace OpenRA.FileFormats
{
	public enum TerrainType : byte
	{
		Clear = 0,
		Water = 1,
		Road = 2,
		Rock = 3,
		Tree = 4,
		River = 5,
		Rough = 6,
		Wall = 7,
		Beach = 8,
		Ore = 9,
		Special = 10,
	}
	
	public class TileTemplate
	{
		public int Index; // not valid for `interior` stuff. only used for bridges.
		public string Name;
		public int2 Size;
		public string Bridge;
		public float HP;
		public Dictionary<int, TerrainType> TerrainType = new Dictionary<int, TerrainType>();
	}
	
	public class Walkability
	{
		Dictionary<string, TileTemplate> walkability 
			= new Dictionary<string,TileTemplate>();

		public Walkability(string templatesFile)
		{
			var file = new IniFile( FileSystem.Open( templatesFile ) );

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
							p => (TerrainType)Enum.Parse(typeof(TerrainType),p.Value)),
					Name = section.GetValue("Name", null).ToLowerInvariant(),
					Bridge = section.GetValue("bridge", null),
					HP = float.Parse(section.GetValue("hp", "0"))
				};
								
				tile.Index = -1;
				int.TryParse(section.Name.Substring(3), out tile.Index);

				walkability[tile.Name] = tile;
			}
		}

		public TileTemplate GetTerrainType(string terrainName)
		{
			return walkability[terrainName];
		}
	}
}
