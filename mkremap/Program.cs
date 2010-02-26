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
using System.Drawing;
using System.IO;
using System.Linq;

namespace mkremap
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 8)
			{
				Console.WriteLine("Usage: mkremap cnc|ra dest c1.r c1.g c1.b c2.r c2.g c2.b");
				return;
			}

			var c1 = Color.FromArgb(255, int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]));
			var c2 = Color.FromArgb(255, int.Parse(args[5]), int.Parse(args[6]), int.Parse(args[7]));

			var baseIndex = args[0] == "ra" ? 80 : 0xb0;
			var fracs = args[0] == "ra"
				? new[] { 0, 1 / 16f, 2 / 16f, 3 / 16f, 4 / 16f, 5 / 16f, 6 / 16f, 7 / 16f, 8 / 16f, 9 / 16f, 10 / 16f, 11 / 16f, 12 / 16f, 13 / 16f, 14 / 16f, 15 / 16f }
				: new[] { 0, 2 / 16f, 4 / 16f, 6 / 16f, 8 / 16f, 10 / 16f, 13 / 16f, 15 / 16f, 1 / 16f, 3 / 16f, 5 / 16f, 7 / 16f, 9 / 16f, 11 / 16f, 12 / 16f, 14 / 16f };

			File.WriteAllLines( args[1],
				fracs.Select(x => ColorLerp(x, c1, c2))
					.Select((x, i) => string.Format("{0}: 255,{1},{2},{3}", baseIndex + i, x.R, x.G, x.B))
					.ToArray());
		}

		static Color ColorLerp(float t, Color c1, Color c2)
		{
			return Color.FromArgb(255,
				(int)(t * c2.R + (1 - t) * c1.R),
				(int)(t * c2.G + (1 - t) * c1.G),
				(int)(t * c2.B + (1 - t) * c1.B));
		}
	}
}
