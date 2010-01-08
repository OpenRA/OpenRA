using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class EngineerCapture : IIssueOrder, IResolveOrder
	{
		public const int EngineerDamage = 300;	// todo: push into rules, as a weapon

		public EngineerCapture(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!underCursor.traits.Contains<Building>()) return null;
			
			// todo: other bits

			return new Order(underCursor.Health <= EngineerDamage ? "Capture" : "Infiltrate",
				self, underCursor, int2.Zero, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate" || order.OrderString == "Capture")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new CaptureBuilding(order.TargetActor));
			}
		}
	}
}
