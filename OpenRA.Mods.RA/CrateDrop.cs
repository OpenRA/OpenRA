#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CrateDropInfo : TraitInfo<CrateDrop>
	{
		public readonly int Minimum = 1; // Minumum number of crates
		public readonly int Maximum = 255; // Maximum number of crates
		public readonly string[] ValidGround = {"Clear", "Rough", "Road", "Ore", "Beach"}; // Which terrain types can we drop on?
		public readonly string[] ValidWater = {"Water"};
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
				
				// Is this valid terrain?
				var terrainType = self.World.GetTerrainType(p);
				if (!(inWater ? info.ValidWater : info.ValidGround).Contains(terrainType)) continue;
				
				// Don't drop on any actors
				if (self.World.WorldActor.traits.Get<BuildingInfluence>().GetBuildingAt(p) != null) continue;
				if (self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(p).Any()) continue;

				self.World.AddFrameEndTask(w =>
					{
						var crate = w.CreateActor(false, "crate", new int2(0, 0), w.WorldActor.Owner);
						crates.Add(crate);

						var startPos = w.ChooseRandomEdgeCell();
						var plane = w.CreateActor("BADR", startPos, w.WorldActor.Owner);
						var aircraft = plane.traits.Get<Aircraft>();
						aircraft.Facing = Util.GetFacing(p - startPos, 0);
						plane.CancelActivity();
						plane.QueueActivity(new FlyCircle(p));
						plane.traits.Get<ParaDrop>().SetLZ(p, null);
						plane.traits.Get<Cargo>().Load(plane, crate);
					});
				return;
			}
		}
	}
}
