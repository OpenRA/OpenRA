using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class AirstrikePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new AirstrikePower(self, this); }
	}

	class AirstrikePower : SupportPower, IResolveOrder
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info) : base(self, info) { }

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					Game.controller.CancelInputMode();
				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
					yield return new Order("Airstrike", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) { }
			public void Render(World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi) { return "ability"; }
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Airstrike")
			{
				// todo: spawn a10, have it dump napalm all over the target

				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}
	}
}
