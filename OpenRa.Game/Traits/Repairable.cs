using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Repairable : IOrder
	{
		IDisposable reservation;
		public Repairable(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;

			if (underCursor.Info == Rules.UnitInfo["FIX"]
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor))
					return;

				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null) reservation = res.Reserve(self);

				self.CancelActivity();
				self.QueueActivity(new Move(((1 / 24f) * order.TargetActor.CenterLocation).ToInt2(), order.TargetActor));
				self.QueueActivity(new Rearm());
				self.QueueActivity(new Repair());
			}
		}
	}
}
