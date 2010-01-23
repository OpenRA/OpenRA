using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits.Activities;
using OpenRa.Traits;
using OpenRa.Mods.RA.Activities;

namespace OpenRa.Mods.RA
{
	class SpyInfo : StatelessTraitInfo<Spy> { }

	class Spy : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (underCursor.Owner == self.Owner) return null;
			if (!underCursor.traits.Contains<IAcceptSpy>()) return null;

			return new Order("Infiltrate", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new Infiltrate(order.TargetActor));
			}
		}
	}
}
