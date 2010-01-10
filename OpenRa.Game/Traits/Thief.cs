using OpenRa.Game.Traits.Activities;
using System.Collections.Generic;
using System.Linq;
namespace OpenRa.Game.Traits
{
	class ThiefInfo : StatelessTraitInfo<Thief> { }

	class Thief : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!underCursor.traits.WithInterface<IAcceptThief>().Any()) return null;
			
			return new Order("Steal", self, underCursor, int2.Zero, null);
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
