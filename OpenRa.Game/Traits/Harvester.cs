using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Harvester : IOrder
	{
		const int capacity = 28;
		int oreCarried = 0;					/* sum of these must not exceed capacity */
		int gemsCarried = 0;

		bool IsFull { get { return oreCarried + gemsCarried == capacity; } }
		bool IsEmpty { get { return oreCarried == 0 && gemsCarried == 0; } }

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (underCursor != null 
				&& underCursor.Owner == self.Owner 
				&& underCursor.traits.Contains<AcceptsOre>() && !IsEmpty)
				return OpenRa.Game.Order.DeliverOre(self, underCursor);

			if (underCursor == null && Game.map.ContainsResource(xy) && !IsFull)
				return OpenRa.Game.Order.Harvest(self, xy);

			return null;
		}

		public Harvester(Actor self) { }
	}
}
