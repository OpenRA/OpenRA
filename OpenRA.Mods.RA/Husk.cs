using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HuskInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new Husk( init ); }
	}

	class Husk : IOccupySpace
	{
		Actor self;
		[Sync]
		int2 location;

		public Husk(ActorInitializer init)
		{
			this.self = init.self;
			this.location = init.location;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);
		}

		public int2 TopLeft { get { return location; } }

		public IEnumerable<int2> OccupiedCells() { yield return TopLeft; }
	}
}
