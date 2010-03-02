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
		public static void DestroyOre(this Map map, int i, int j)
		{
			//if (map.ContainsResource(new int2(i, j)))
			//{
			//    map.MapTiles[i, j].density = 0;
			//    map.MapTiles[i, j].overlay = 0xff;
			//}
		}

		public static void SpreadOre(this World world, Random r, float chance)
		{
			var map = world.Map;

			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			/* phase 1: grow into neighboring regions */
			var newOverlay = new byte[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
				{
					newOverlay[i, j] = 0xff;
					if (!map.HasOverlay(i, j)
						&& r.NextDouble() < chance
						&& map.GetOreDensity(i, j) > 0
						&& world.IsCellBuildable(new int2(i,j), UnitMovementType.Wheel))
						newOverlay[i, j] = ChooseOre();
				}

			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (newOverlay[i, j] != 0xff)
						map.MapTiles[i, j].overlay = newOverlay[i, j];
		}

		public static void GrowOre(this World world)
		{
			var map = world.Map;
			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			/* phase 2: increase density of existing areas */
			var newDensity = new byte[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (map.ContainsOre(i, j)) newDensity[i, j] = map.GetOreDensity(i, j);

//			for (int j = minj; j < maxj; j++)
//				for (int i = mini; i < maxi; i++)
//					if (map.MapTiles[i, j].density < newDensity[i, j])
//						++map.MapTiles[i, j].density;
		}

		static byte GetOreDensity(this Map map, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (map.ContainsOre(i + u, j + v))
						++sum;
			sum = (sum * 4 + 2) / 3;
			return (byte)sum;
		}

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

		static byte ore = 5;
		static byte ChooseOre()
		{
			if (++ore > 8) ore = 5;
			return ore;
		}

		public static bool[] overlayIsOre =
			{
				false, false, false, false, false,
				true, true, true, true,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		public static bool[] overlayIsGems =
			{
				false, false, false, false, false,
				false, false, false, false,
				true, true, true, true,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};
	}
}
