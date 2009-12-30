using OpenRa.Game.Traits.Activities;
using System;

namespace OpenRa.Game.Traits
{
	class Helicopter : IOrder, IMovement
	{
		public IDisposable reservation;
		public Helicopter(Actor self) {}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
				return new Order("Move", self, null, xy, null);

			if (underCursor.Info == Rules.UnitInfo["HPAD"]
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor, int2.Zero, null);

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(order.TargetLocation)));
				self.QueueActivity(new Turn(self.Info.InitialFacing));
				self.QueueActivity(new HeliLand(true));
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.CenterLocation));
				self.QueueActivity(new Turn(self.Info.InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(new Rearm());
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
