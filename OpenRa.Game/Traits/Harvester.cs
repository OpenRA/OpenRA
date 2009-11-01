using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Harvester : IOrder
	{
		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (underCursor != null 
				&& underCursor.Owner == self.Owner 
				&& underCursor.traits.Contains<AcceptsOre>())
				return OpenRa.Game.Order.DeliverOre(self, underCursor);

			/* todo: harvest order when on ore */

			return null;
		}

		public Harvester(Actor self) { }
	}
}
