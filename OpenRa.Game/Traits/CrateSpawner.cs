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
		public readonly float WaterCrateChance = 0.2f; // Change of generating a water crate instead of a land crate
		
		public object Create(Actor self) { return new CrateSpawner(self); }
	}
	
	class CrateSpawner
	{
		Actor self;
		public CrateSpawner(Actor self)
		{
			this.self = self;
		}
	}
}
