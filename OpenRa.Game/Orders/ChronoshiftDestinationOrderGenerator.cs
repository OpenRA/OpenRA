using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.Traits;
using OpenRa.SupportPowers;

namespace OpenRa.Orders
{
	class ChronoshiftDestinationOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;
		SupportPower power;

		public ChronoshiftDestinationOrderGenerator(Actor self, SupportPower power)
		{
			this.self = self;
			this.power = power;
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}
			yield return new Order("Chronoshift", self, null, xy, 
				power != null ? power.Name : null);
		}

		public void Tick() {}
		public void Render()
		{
			Game.world.WorldRenderer.DrawSelectionBox(self, Color.White, true);
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			if (!Game.world.LocalPlayer.Shroud.IsExplored(xy))
				return Cursor.MoveBlocked;
			
			var movement = self.traits.GetOrDefault<IMovement>();
			return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
		}
	}
}
