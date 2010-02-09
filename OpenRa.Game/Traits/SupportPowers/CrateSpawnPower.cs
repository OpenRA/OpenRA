using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Orders;

namespace OpenRa.Traits
{
	class CrateSpawnPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public override object Create(Actor self) { return new CrateSpawnPower(self, this); }
	}

	class CrateSpawnPower : SupportPower, IResolveOrder
	{
		public CrateSpawnPower(Actor self, CrateSpawnPowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpawnCrate")
			{
				self.World.AddFrameEndTask(
					w => w.CreateActor("crate", order.TargetLocation, self.Owner));
					
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}

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
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location).FirstOrDefault();
					if (underCursor == null)
						yield return new Order("SpawnCrate", world.LocalPlayer.PlayerActor, xy);
				}

				yield break;
			}

			public void Tick(World world) {	}

			public void Render(World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "ability" : "move-blocked";
			}
		}
	}
}
