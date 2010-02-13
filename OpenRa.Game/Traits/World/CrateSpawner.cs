using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	class CrateSpawnerInfo : ITraitInfo
	{
		public readonly int CrateMinimum = 1; // Minumum number of crates
		public readonly int CrateMaximum = 255; // Maximum number of crates
		public readonly int CrateRadius = 3; // Radius of crate effect TODO: This belongs on the crate effect itself
		public readonly int CrateRegen = 180; // Average time (seconds) between crate spawn
		public readonly float WaterCrateChance = .2f; // Chance of generating a water crate instead of a land crate

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
				ticks = info.CrateRegen * 25;		// todo: randomize
			
				crates.RemoveAll(c => !c.IsInWorld);

				while (crates.Count < info.CrateMinimum)
					SpawnCrate(self, info);
				if (crates.Count < info.CrateMaximum)
					SpawnCrate(self, info);
			}
		}

		void SpawnCrate(Actor self, CrateSpawnerInfo info)
		{
			var inWater = Game.SharedRandom.NextDouble() < info.WaterCrateChance;
			var umt = inWater ? UnitMovementType.Float : UnitMovementType.Wheel;

			for (; ; )
			{
				var p = new int2(Game.SharedRandom.Next(128), Game.SharedRandom.Next(128));
				if (self.World.IsCellBuildable(p, umt))
				{
					self.World.AddFrameEndTask(
						w => crates.Add(w.CreateActor("crate", p, self.Owner)));
					break;
				}
			}
		}
	}
}
