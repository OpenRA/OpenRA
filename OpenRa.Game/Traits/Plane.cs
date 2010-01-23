using System;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class PlaneInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Plane(self); }
	}

	class Plane : IIssueOrder, IResolveOrder, IMovement
	{
		public IDisposable reservation;

		public Plane(Actor self) {}

		// todo: push into data!
		static bool PlaneCanEnter(Actor a)
		{
			if (a.Info.Name == "afld") return true;
			if (a.Info.Name == "fix") return true;
			return false;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor == null)
				return new Order("Move", self, xy);

			if (PlaneCanEnter(underCursor)
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
				self.QueueActivity(new ReturnToBase(self, null));
				self.QueueActivity(new Rearm());
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));
				self.QueueActivity(order.TargetActor.Info.Name == "afld"
					? (IActivity)new Rearm() : new Repair(true));
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
