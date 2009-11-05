using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Harvester : IOrder
	{
		public int oreCarried = 0;					/* sum of these must not exceed capacity */
		public int gemsCarried = 0;

		public bool IsFull { get { return oreCarried + gemsCarried == Rules.General.BailCount; } }
		public bool IsEmpty { get { return oreCarried == 0 && gemsCarried == 0; } }

		public void AcceptResource(bool isGem)
		{
			if (isGem) gemsCarried++;
			else oreCarried++;
		}

		public void Deliver(Actor self)
		{
			self.Owner.GiveCash(oreCarried * Rules.General.GoldValue);
			self.Owner.GiveCash(gemsCarried * Rules.General.GemValue);
			oreCarried = 0;
			gemsCarried = 0;
		}

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb) return null;

			if (underCursor != null 
				&& underCursor.Owner == self.Owner 
				&& underCursor.traits.Contains<AcceptsOre>() && !IsEmpty)
				return OpenRa.Game.Order.DeliverOre(self, underCursor);

			if (underCursor == null && Game.map.ContainsResource(xy))
				return OpenRa.Game.Order.Harvest(self, xy);

			return null;
		}

		public Harvester(Actor self) { }
	}
}
