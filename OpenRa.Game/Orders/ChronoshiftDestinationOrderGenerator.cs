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

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}
			yield return new Order("Chronoshift", self, null, xy, 
				power != null ? power.Name : null);
		}

		public void Tick( World world ) {}
		public void Render( World world )
		{
			world.WorldRenderer.DrawSelectionBox(self, Color.White, true);
		}

		public Cursor GetCursor(World world, int2 xy, MouseInput mi)
		{
			if (!world.LocalPlayer.Shroud.IsExplored(xy))
				return Cursor.MoveBlocked;
			
			var movement = self.traits.GetOrDefault<IMovement>();
			return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
		}
	}
}
