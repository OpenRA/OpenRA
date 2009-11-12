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
		//	if (!Game.skipMakeAnims)
				Game.world.AddFrameEndTask(
					w =>
					{		/* create the free harvester! */
						var harvester = new Actor("harv", self.Location + new int2(1, 2), self.Owner);
						var mobile = harvester.traits.Get<Mobile>();
						mobile.facing = 64;
						mobile.QueueActivity(new Harvest());
						w.Add(harvester);
					});
		}
	}
}
