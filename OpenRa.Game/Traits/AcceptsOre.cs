using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AcceptsOre
	{
		public AcceptsOre(Actor self)
		{
			/* create the free harvester! */
			Game.world.AddFrameEndTask(
				w =>
				{
					var harvester = new Actor("harv", self.Location + new int2(1, 2), self.Owner);
					var mobile = harvester.traits.Get<Mobile>();
					mobile.facing = 64;
					mobile.InternalSetActivity( new Harvest() );
					w.Add(harvester);
				});
		}
	}
}
