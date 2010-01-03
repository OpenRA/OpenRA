using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenRa.Game.Orders
{
	class TeleportOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;

		public TeleportOrderGenerator(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}
						
			yield return new Order("Chronoshift", self, null, xy, null);
		}

		public void Tick() {}
		public void Render()
		{
			Game.worldRenderer.DrawSelectionBox(self, Color.White, true);
		}
	}
}
