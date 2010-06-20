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

using System;
using System.Collections.Generic;
using System.Linq;

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
		public bool PickAny;
		public Dictionary<int, TerrainType> TerrainType = new Dictionary<int, TerrainType>();
	}
	
	public class Walkability
	{
		Dictionary<string, TileTemplate> templates 
			= new Dictionary<string,TileTemplate>();

		public Walkability(string templatesFile)
		{
			var file = new IniFile( FileSystem.Open( templatesFile ) );

			foreach (var section in file.Sections)
			{
				var name = section.GetValue("Name", null).ToLowerInvariant();
				if (!section.Contains("width") || !section.Contains("height"))
					throw new InvalidOperationException("no width/height for template `{0}`".F(name));

				var tile = new TileTemplate
				{
					Name = name,
					Size = new int2(
						int.Parse(section.GetValue("width", "--")),
						int.Parse(section.GetValue("height", "--"))),
					TerrainType = section
						.Where(p => p.Key.StartsWith("tiletype"))
						.ToDictionary(
							p => int.Parse(p.Key.Substring(8)),
							p => (TerrainType)Enum.Parse(typeof(TerrainType),p.Value)),
					
					Bridge = section.GetValue("bridge", null),
					HP = float.Parse(section.GetValue("hp", "0")),
					PickAny = (bool)FieldLoader.GetValue(typeof(bool), section.GetValue("pickany", "no")),
				};
								
				tile.Index = -1;
				int.TryParse(section.Name.Substring(3), out tile.Index);

				templates[tile.Name] = tile;
			}
		}

		public TileTemplate GetTileTemplate(string terrainName)
		{
			return templates[terrainName];
		}
	}
}
