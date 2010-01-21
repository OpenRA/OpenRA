using System.Linq;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class MinelayerInfo : StatelessTraitInfo<Minelayer>
	{
		public readonly string Mine = "minv";
	}

	class Minelayer : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return null;

			// Ensure that the cell is empty except for the minelayer
			if (self.World.UnitInfluence.GetUnitsAt(xy).Any(a => a != self))
				return null;

			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("Deploy", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
				if (limitedAmmo != null)
					limitedAmmo.Attacking(self);

				// todo: delay a bit? (req making deploy-mine an activity)

				self.World.AddFrameEndTask(
					w => w.CreateActor(self.Info.Traits.Get<MinelayerInfo>().Mine, self.Location, self.Owner));
			}
		}
	}
}
