using OpenRa.Mods.RA.Activities;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
{
	class ThiefInfo : StatelessTraitInfo<Thief> { }

	class Thief : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!underCursor.traits.Contains<IAcceptThief>()) return null;

			return new Order("Steal", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Steal")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new Steal(order.TargetActor));
			}
		}
	}
}
