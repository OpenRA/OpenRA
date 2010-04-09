using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class AirstrikePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new AirstrikePower(self, this); }
	}

	class AirstrikePower : SupportPower, IResolveOrder
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play(Info.SelectTargetSound);
		}

		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, Info.EndChargeSound); }

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
				var startPos = Owner.World.ChooseRandomEdgeCell();
				Owner.World.AddFrameEndTask(w =>
					{
						var a = w.CreateActor("a10", startPos, Owner);
						a.traits.Get<Unit>().Facing = Util.GetFacing(order.TargetLocation - startPos, 0);
						a.traits.Get<Unit>().Altitude = a.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
						a.traits.Get<CarpetBomb>().SetTarget(order.TargetLocation);

						a.CancelActivity();
						a.QueueActivity(new Fly(order.TargetLocation));
						a.QueueActivity(new FlyOffMap { Interruptible = false });
						a.QueueActivity(new RemoveSelf());
					});

				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}
	}
}
