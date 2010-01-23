using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Orders;

namespace OpenRa.Traits
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public readonly bool KillCargo = true;
		public override object Create(Actor self) { return new ChronoshiftPower(self,this); }
	}

	class ChronoshiftPower : SupportPower, IResolveOrder
	{
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }
		protected override void OnBeginCharging() { Sound.Play("chrochr1.aud"); }
		protected override void OnFinishCharging() { Sound.Play("chrordy1.aud"); }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new ChronosphereSelectOrderGenerator();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect" && self.Owner == self.World.LocalPlayer)
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(order.TargetActor);
			
			if (order.OrderString == "ChronosphereFinish")
			{
				Game.controller.CancelInputMode();
				FinishActivate();

				Sound.Play("chrono2.aud");

				var chronosphere = self.World.Actors.Where(a => a.Owner == self.Owner 
					&& a.traits.Contains<Chronosphere>()).FirstOrDefault();
				if (chronosphere != null)
					Game.orderManager.IssueOrder(new Order("PlayAnimation", chronosphere, "active"));

				// Trigger screen desaturate effect
				foreach (var a in self.World.Actors.Where(a => a.traits.Contains<ChronoshiftPaletteEffect>()))
					a.traits.Get<ChronoshiftPaletteEffect>().DoChronoshift();
			}
		}
	}

	// tag trait to identify the building
	class ChronosphereInfo : StatelessTraitInfo<Chronosphere> { }
	public class Chronosphere { }
}
