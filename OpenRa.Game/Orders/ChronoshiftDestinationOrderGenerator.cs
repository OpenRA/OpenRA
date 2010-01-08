using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.Game.Traits;
using OpenRa.Game.SupportPowers;

namespace OpenRa.Game.Orders
{
	class ChronoshiftDestinationOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;
		ISupportPowerImpl chronospherePower;
		public ChronoshiftDestinationOrderGenerator(Actor self, ISupportPowerImpl chronospherePower)
		{
			this.self = self;
			this.chronospherePower = chronospherePower;
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}
			yield return new Order("Chronoshift", self, null, xy, null, chronospherePower);
		}

		public void Tick() {}
		public void Render()
		{
			Game.worldRenderer.DrawSelectionBox(self, Color.White, true);
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			if (!Game.LocalPlayer.Shroud.IsExplored(xy))
				return Cursor.MoveBlocked;
			
			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
		}
	}
}
