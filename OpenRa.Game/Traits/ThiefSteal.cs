using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class ThiefSteal : IOrder
	{
		public ThiefSteal(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!underCursor.traits.Contains<Building>()) return null;

			// todo: other bits

			return new Order("Steal", self, underCursor, int2.Zero, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Steal")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new StealOre(order.TargetActor));
			}
		}
	}
}
