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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public static class Ore
	{
		static bool HasOverlay(this Map map, int i, int j)
		{
			return map.MapTiles[i, j].overlay < overlayIsOre.Length;
		}

		static bool ContainsOre(this Map map, int i, int j)
		{
			return map.HasOverlay(i, j) && overlayIsOre[map.MapTiles[i, j].overlay];
		}

		static bool ContainsGem(this Map map, int i, int j)
		{
			return map.HasOverlay(i, j) && overlayIsGems[map.MapTiles[i, j].overlay];
		}

		public static bool ContainsResource(this Map map, int2 p)
		{
			return map.ContainsGem(p.X, p.Y) || map.ContainsOre(p.X, p.Y);
		}

		static bool[] overlayIsOre =
			{
				false, false, false, false, false,
				true, true, true, true,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		static bool[] overlayIsGems =
			{
				false, false, false, false, false,
				false, false, false, false,
				true, true, true, true,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};
	}
}
