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
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	public class CrateDropInfo : TraitInfo<CrateDrop>
	{
		public readonly int Minimum = 1; // Minumum number of crates
		public readonly int Maximum = 255; // Maximum number of crates
		public readonly int SpawnInterval = 180; // Average time (seconds) between crate spawn
		public readonly float WaterChance = .2f; // Chance of generating a water crate instead of a land crate
	}

	public class CrateDrop : ITick
	{
		List<Actor> crates = new List<Actor>();
		int ticks = 0;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<CrateDropInfo>();
				ticks = info.SpawnInterval * 25;		// todo: randomize

				crates.RemoveAll(x => !x.IsInWorld);	// BUG: this removes crates that are cargo of a BADR!
				
				var toSpawn = Math.Max(0, info.Minimum - crates.Count)
					+ (crates.Count < info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self, info);
			}
		}

		void SpawnCrate(Actor self, CrateDropInfo info)
		{
			var threshold = 100;
			var inWater = self.World.SharedRandom.NextDouble() < info.WaterChance;

			for (var n = 0; n < threshold; n++ )
			{
				var p = self.World.ChooseRandomCell(self.World.SharedRandom);
				if (self.World.IsPathableCell(p, inWater ? UnitMovementType.Float : UnitMovementType.Wheel))
				{
					self.World.AddFrameEndTask(w =>
						{
							var crate = new Actor(w, "crate", new int2(0, 0), w.NeutralPlayer);
							crates.Add(crate);
							self.World.WorldActor.traits.Get<UnitInfluence>().Remove(crate, crate.traits.Get<IOccupySpace>());

							var startPos = w.ChooseRandomEdgeCell();
							var plane = w.CreateActor("BADR", startPos, w.NeutralPlayer);
							plane.traits.Get<Unit>().Facing = Util.GetFacing(p - startPos, 0);
							plane.CancelActivity();
							plane.QueueActivity(new FlyCircle(p));
							plane.traits.Get<ParaDrop>().SetLZ(p, null, inWater);
							plane.traits.Get<Cargo>().Load(plane, crate);
						});
					return;
				}
			}
		}
	}
}
