using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Orders
{
	class ChronoshiftDestinationOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;

		public ChronoshiftDestinationOrderGenerator(Actor self)
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

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
		}
	}
}
