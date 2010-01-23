using System.Collections.Generic;
using System.Drawing;
using OpenRa.Traits;

namespace OpenRa.Mods.Aftermath.Orders
{
	class SetChronoTankDestination : IOrderGenerator
	{
		public readonly Actor self;

		public SetChronoTankDestination(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}

			yield return new Order("ChronoshiftSelf", self, xy);
		}

		public void Tick( World world ) { }
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
