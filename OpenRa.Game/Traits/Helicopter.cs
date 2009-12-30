using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Helicopter : IOrder, IMovement
	{
		public Helicopter(Actor self) {}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
				return new Order("Move", self, null, xy, null);

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(order.TargetLocation)));
				self.QueueActivity(new HeliLand(true));
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
