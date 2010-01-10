using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Chronosphere : IResolveOrder
	{
		public Chronosphere(Actor self) { }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlayAnimation")
			{
				var rb = self.traits.Get<RenderBuilding>();
				if (rb != null)
					rb.PlayCustomAnim(self, order.TargetString);
			}
		}
	}
}
