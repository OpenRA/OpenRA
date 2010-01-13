using System.Linq;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class PassengerInfo : StatelessTraitInfo<Passenger> {}

	class Passenger : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) 
				return null;

			if (underCursor == null || underCursor.Owner != self.Owner)
				return null;

			var cargo = underCursor.traits.GetOrDefault<Cargo>();
			if (cargo == null || cargo.IsFull(underCursor))
				return null;

			var umt = self.traits.Get<IMovement>().GetMovementType();
			if (!underCursor.Info.Traits.Get<CargoInfo>().PassengerTypes.Contains(umt))
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
