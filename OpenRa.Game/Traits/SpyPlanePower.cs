using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	class SpyPlanePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new SpyPlanePower(self,this); }
	}

	class SpyPlanePower : SupportPower, IResolveOrder
	{
		public SpyPlanePower(Actor self, SpyPlanePowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging() { Sound.Play("spypln1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpyPlane")
			{
				FinishActivate();

				if (order.Player == Owner.World.LocalPlayer)
					Game.controller.CancelInputMode();

				// todo: pick a cell p1 on the edge of the map; get the cell p2 at the other end of the line
				// through that p1 & the target location;

				// todo: spawn a SpyPlane at p1 with activities:
				//		-- fly to target point
				//		-- take picture
				//		-- fly to p2
				//		-- leave the world
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					Game.controller.CancelInputMode();
					yield break;
				}

				yield return new Order("SpyPlane", Game.world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) {}
			public void Render(World world) {}

			public Cursor GetCursor(World world, int2 xy, MouseInput mi) { return Cursor.Ability; }
		}
	}
}
