using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class ChronoshiftDestinationOrderGenerator : IOrderGenerator
	{
		public readonly Actor self;

		public ChronoshiftDestinationOrderGenerator(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				Game.controller.CancelInputMode();
				yield break;
			}

			yield return new Order("Chronoshift", self, xy);
			yield return new Order("ChronosphereFinish", self.Owner.PlayerActor);
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
