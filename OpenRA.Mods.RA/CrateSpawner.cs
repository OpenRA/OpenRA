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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CrateSpawnerInfo : ITraitInfo
	{
		public readonly int Minimum = 1; // Minumum number of crates
		public readonly int Maximum = 255; // Maximum number of crates
		public readonly int SpawnInterval = 180; // Average time (seconds) between crate spawn
		public readonly float WaterChance = .2f; // Chance of generating a water crate instead of a land crate

		public object Create(Actor self) { return new CrateSpawner(); }
	}
	
	// assumption: there is always at least one free water cell, and one free land cell.

	class CrateSpawner : ITick
	{
		List<Actor> crates = new List<Actor>();
		int ticks = 0;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<CrateSpawnerInfo>();
				ticks = info.SpawnInterval * 25;		// todo: randomize
			
				crates.RemoveAll(c => !c.IsInWorld);

				var toSpawn = Math.Max(0, info.Minimum - crates.Count)
					+ (crates.Count < info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self, info);
			}
		}

		const int ChooseCrateLocationAttempts = 100;

		void SpawnCrate(Actor self, CrateSpawnerInfo info)
		{
			var inWater = self.World.SharedRandom.NextDouble() < info.WaterChance;

			for (var n = 0; n < ChooseCrateLocationAttempts; n++)
			{
				var p = self.World.ChooseRandomCell(self.World.SharedRandom);

				if (self.World.IsCellBuildable(p, inWater))
				{
					self.World.AddFrameEndTask(
						w => crates.Add(w.CreateActor("crate", p, self.World.WorldActor.Owner)));
					break;
				}
			}
		}
	}
}
