using System.Collections.Generic;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
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

				DoParadrop(Owner, order.TargetLocation, 
					self.Info.Traits.Get<ParatroopersPowerInfo>().DropItems);

				FinishActivate();
			}
		}

		static void DoParadrop(Player owner, int2 p, string[] items)
		{
			var startPos = owner.World.ChooseRandomEdgeCell();
			owner.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("BADR", startPos, owner);

				a.CancelActivity();
				a.QueueActivity(new FlyCircle(p));
				a.traits.Get<ParaDrop>().SetLZ(p);

				var cargo = a.traits.Get<Cargo>();
				foreach (var i in items)
					cargo.Load(a, new Actor(owner.World, i.ToLowerInvariant(), 
						new int2(0,0), a.Owner));
			});
		}
	}
}
