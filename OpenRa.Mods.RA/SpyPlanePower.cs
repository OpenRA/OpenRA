using System.Collections.Generic;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
{
	class SpyPlanePowerInfo : SupportPowerInfo
	{
		public readonly int Range = 10;
		public override object Create(Actor self) { return new SpyPlanePower(self,this); }
	}

	class SpyPlanePower : SupportPower, IResolveOrder
	{
		public SpyPlanePower(Actor self, SpyPlanePowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "spypln1.aud"); }
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

				var enterCell = self.World.ChooseRandomEdgeCell();
				var exitCell = self.World.ChooseRandomEdgeCell();

				var plane = self.World.CreateActor("U2", enterCell, self.Owner);
				plane.CancelActivity();
				plane.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
				plane.QueueActivity(new CallFunc(
					() => Owner.Shroud.Explore(Owner.World, order.TargetLocation,
						(Info as SpyPlanePowerInfo).Range)));
				plane.QueueActivity(new Fly(Util.CenterOfCell(exitCell)));
				plane.QueueActivity(new RemoveSelf());
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

				yield return new Order("SpyPlane", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) {}
			public void Render(World world) {}

			public Cursor GetCursor(World world, int2 xy, MouseInput mi) { return Cursor.Ability; }
		}
	}
}
