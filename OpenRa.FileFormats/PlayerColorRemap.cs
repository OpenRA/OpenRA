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
using System.Drawing;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class PlayerColorRemap : IPaletteRemap
	{
		Dictionary<int, Color> remapColors;

		public PlayerColorRemap(Stream s)
		{
			var yaml = MiniYaml.FromStream(s);
			remapColors = yaml.ToDictionary(
				y => int.Parse(y.Key),
				y => ArrayToColor((int[])FieldLoader.GetValue(
					typeof(int[]), y.Value.Value.Trim())));
		}

		static Color ArrayToColor(int[] x) { return Color.FromArgb(x[0], x[1], x[2], x[3]); }

		public Color GetRemappedColor(Color original, int index)
		{
			Color c;
			return remapColors.TryGetValue(index, out c) 
				? c : original;
		}
	}
}
