using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Minelayer : IOrder
	{
		public Minelayer(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			// todo: check for ammo
			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("Deploy", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				// todo: check for and adjust ammo
				// todo: delay a bit?

				Game.world.AddFrameEndTask(
					w => w.Add(new Actor(Rules.UnitInfo[self.Info.Primary], self.Location, self.Owner)));
			}
		}
	}
}
