using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Orders;

namespace OpenRa.Traits
{
	class NukePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new NukePower(self, this); }
	}
	
	class NukePower : SupportPower, IResolveOrder
	{
		public NukePower(Actor self, NukePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.Play("aprep1.aud"); }
		protected override void OnFinishCharging() { Sound.Play("aready1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "NuclearMissile")
			{
				var silo = self.World.Actors.Where(a => a.Owner == self.Owner
					&& a.traits.Contains<NukeSilo>()).FirstOrDefault();
				if (silo != null)
					silo.traits.Get<RenderBuilding>().PlayCustomAnim(silo, "active");
				
				Owner.World.AddFrameEndTask(w =>
				{
					// Play to everyone but the current player
					if (Owner != Owner.World.LocalPlayer)
						Sound.Play("alaunch1.aud");

					// TODO: FIRE ZE MISSILES
					//w.Add(new NukeLaunch(silo));
				});
				
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public SelectTarget() {	}

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
					yield return new Order("NuclearMissile", Game.world.LocalPlayer.PlayerActor, xy);
				}

				yield break;
			}

			public void Tick(World world)
			{
				var hasStructure = world.Actors
					.Any(a => a.Owner == world.LocalPlayer && a.traits.Contains<NukeSilo>());

				if (!hasStructure)
					Game.controller.CancelInputMode();
			}

			public void Render(World world) { }
			public Cursor GetCursor(World world, int2 xy, MouseInput mi) { return Cursor.Nuke; }
		}
	}

	// tag trait for the building
	class NukeSiloInfo : StatelessTraitInfo<NukeSilo> { }
	class NukeSilo { }
}
