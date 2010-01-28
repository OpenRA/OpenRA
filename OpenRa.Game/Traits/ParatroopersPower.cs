using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class ParatroopersPowerInfo : SupportPowerInfo
	{
		public string[] DropItems = { };
		public override object Create(Actor self) { return new ParatroopersPower(self,this); }
	}

	class ParatroopersPower : SupportPower, IResolveOrder
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
					yield return new Order("ParatroopersActivate", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) {}
			public void Render(World world) {}

			public Cursor GetCursor(World world, int2 xy, MouseInput mi)
			{
				return Cursor.Ability;
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ParatroopersActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				var startPos = self.World.ChooseRandomEdgeCell();
				self.World.AddFrameEndTask(w =>
				{
					var a = w.CreateActor("BADR", startPos, Owner);

					a.CancelActivity();
					a.QueueActivity(new FlyCircle(order.TargetLocation));
					a.traits.Get<ParaDrop>().SetLZ(order.TargetLocation);

					var cargo = a.traits.Get<Cargo>();
					foreach (var p in self.Info.Traits.Get<ParatroopersPowerInfo>().DropItems)
						cargo.Load(a, new Actor(self.World, p.ToLowerInvariant(), a.Location, a.Owner));
				});

				FinishActivate();
			}
		}
	}
}
