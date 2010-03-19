using System;
using System.Collections.Generic;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA_NG
{
	public class CrateDropInfo : ITraitInfo
	{
		public readonly int Minimum = 1; // Minumum number of crates
		public readonly int Maximum = 255; // Maximum number of crates
		public readonly int SpawnInterval = 180; // Average time (seconds) between crate spawn
		public readonly float WaterChance = .2f; // Chance of generating a water crate instead of a land crate

		public object Create(Actor self)
		{
			return new CrateDrop();
		}
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

				crates.RemoveAll(x => !x.IsInWorld);
				
				var toSpawn = Math.Max(0, info.Minimum - crates.Count)
					+ (crates.Count < info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self, info);
			}
		}

		void SpawnCrate(Actor self, CrateDropInfo info)
		{
			var inWater = self.World.SharedRandom.NextDouble() < info.WaterChance;
			var umt = inWater ? UnitMovementType.Float : UnitMovementType.Wheel;
			int count = 0, threshold = 100;
			for (; ; )
			{
				var p = new int2(self.World.SharedRandom.Next(0, 127), self.World.SharedRandom.Next(0, 127));
				if (self.World.IsCellBuildable(p, umt))
				{
					self.World.AddFrameEndTask(w =>
						{
							var crate = new Actor(w, "crate", new int2(0, 0), self.Owner);
							crates.Add(crate);
							var plane = w.CreateActor("BADR", w.ChooseRandomEdgeCell(), self.Owner);
							plane.traits.Get<ParaDrop>().SetLZ(p);
							plane.traits.Get<Cargo>().Load(plane, crate);
						});
					break;
				}
				if (count++ > threshold)
					break;
			}
		}
	}
}
