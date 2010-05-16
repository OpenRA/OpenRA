using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HuskInfo : ITraitInfo { public object Create(Actor self) { return new Husk(self); } }

	class Husk : IOccupySpace
	{
		Actor self;
		public Husk(Actor self) 
		{
			this.self = self;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);
		}

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
	}
}
