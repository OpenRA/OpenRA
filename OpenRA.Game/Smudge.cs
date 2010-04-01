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

using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA
{
	static class Smudge
	{
		const int firstScorch = 19;
		const int firstCrater = 25;
		const int framesPerCrater = 5;

		public static void AddSmudge(this Map map, bool isCrater, int x, int y)
		{
			var smudge = map.MapTiles[x, y].smudge;
			if (smudge == 0)
				map.MapTiles[x, y].smudge = (byte) (isCrater
					? (firstCrater + framesPerCrater * ChooseSmudge())
					: (firstScorch + ChooseSmudge()));

			if (smudge < firstCrater || !isCrater) return; /* bib or scorch; don't change */
			
			/* deepen the crater */
			var amount = (smudge - firstCrater) % framesPerCrater;
			if (amount < framesPerCrater - 1)
				map.MapTiles[x, y].smudge++;
		}

		public static void AddSmudge(this Map map, int2 targetTile, WarheadInfo warhead)
		{
			if (warhead.SmudgeType == SmudgeType.None) return;
			if (warhead.Size[0] == 0 && warhead.Size[1] == 0)
				map.AddSmudge(warhead.SmudgeType == SmudgeType.Crater, targetTile.X, targetTile.Y);
			else
				foreach (var t in Game.world.FindTilesInCircle(targetTile, warhead.Size[0]))
					if ((t - targetTile).LengthSquared >= warhead.Size[1] * warhead.Size[1])
						if (Game.world.GetTerrainType(t) != TerrainType.Water)
							map.AddSmudge(warhead.SmudgeType == SmudgeType.Crater, t.X, t.Y);
		}

		static int lastSmudge = 0;
		static int ChooseSmudge() { lastSmudge = (lastSmudge + 1) % 6; return lastSmudge; }
	}
}
