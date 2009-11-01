using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
					harvester.traits.Get<Mobile>().facing = 64;
					w.Add(harvester);
				});
		}
	}
}
