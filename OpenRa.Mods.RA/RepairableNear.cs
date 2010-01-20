using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
{
	class RepairableNearInfo : StatelessTraitInfo<RepairableNear> { }

	class RepairableNear : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			
			if (underCursor.Owner == self.Owner && 
				(underCursor.Info.Name == "spen" || underCursor.Info.Name == "syrd") && 
				self.Health < self.GetMaxHP())
				return new Order("Enter", self, underCursor, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Enter")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new Repair(false));
			}
		}
	}
}
