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
using System.Drawing;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class TerrainColorSet
	{
		readonly Dictionary<TerrainType, Color> colors;

		public TerrainColorSet(string colorFile)
		{
			var lines = FileSystem.Open(colorFile).ReadAllLines()
				.Select(l => l.Trim())
				.Where(l => !l.StartsWith(";") && l.Length > 0);

			colors = lines.Select(l => l.Split('=')).ToDictionary(
				kv => (TerrainType)Enum.Parse(typeof(TerrainType), kv[0]),
				kv => ColorFromRgbString(kv[1]));
		}

		static Color ColorFromRgbString(string s)
		{
			var parts = s.Split(',');
			return Color.FromArgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
		}

		public Color ColorForTerrainType(TerrainType type) { return colors[type]; }
	}
}
