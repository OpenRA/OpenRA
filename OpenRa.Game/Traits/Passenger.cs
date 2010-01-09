using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Passenger : IIssueOrder, IResolveOrder
	{
		public Passenger(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) 
				return null;

			if (underCursor == null || underCursor.Owner != self.Owner)
				return null;

			var cargo = underCursor.traits.GetOrDefault<Cargo>();
			if (cargo == null || cargo.IsFull(underCursor))
				return null;

			var umt = self.traits.WithInterface<IMovement>().First().GetMovementType();
			if (!underCursor.Info.PassengerTypes.Contains(umt))
				return null;

			return new Order("EnterTransport", self, underCursor, int2.Zero, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, 1));
				self.QueueActivity(new EnterTransport(self, order.TargetActor));
			}
		}
	}
}
