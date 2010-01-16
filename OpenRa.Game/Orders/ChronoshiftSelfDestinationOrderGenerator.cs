using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class ChronoshiftSelfDestinationOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;

		public ChronoshiftSelfDestinationOrderGenerator(Actor self)
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

			yield return new Order("ChronoshiftSelf", self, null, xy, null);
		}

		public void Tick() { }
		public void Render()
		{
			Game.worldRenderer.DrawSelectionBox(self, Color.White, true);
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			if (!Game.LocalPlayer.Shroud.IsExplored(xy))
				return Cursor.MoveBlocked;

			var movement = self.traits.GetOrDefault<IMovement>();
			return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
		}
	}
}
