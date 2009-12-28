using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class C4Demolition : IOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (underCursor == null) return null;
			if (underCursor.Owner == self.Owner && !mi.Modifiers.HasModifier(Modifiers.Ctrl)) return null;
			if (!underCursor.traits.Contains<Building>()) return null;

			return new Order("C4", self, underCursor, int2.Zero, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "C4")
			{
				self.CancelActivity();
				self.QueueActivity(new Demolish(order.TargetActor));
			}
		}
	}
}
