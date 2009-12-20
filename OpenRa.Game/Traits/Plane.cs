using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Plane : IOrder
	{
		public Plane(Actor self)
		{
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor == null)
				return Order.Move(self, xy);
			if (underCursor.Info == Rules.UnitInfo["AFLD"])
				return Order.DeliverOre(self, underCursor);		/* brutal hack */
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Circle(order.TargetLocation));
			}

			if (order.OrderString == "DeliverOre")
			{
				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor.CenterLocation));
			}
		}
	}
}
