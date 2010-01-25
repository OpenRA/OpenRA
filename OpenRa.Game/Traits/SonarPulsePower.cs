using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Orders;

namespace OpenRa.Traits
{
	public class SonarPulsePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new SonarPulsePower(self, this); }
	}

	public class SonarPulsePower : SupportPower, IResolveOrder
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { }
		protected override void OnFinishCharging() { if (Owner == Owner.World.LocalPlayer) Sound.Play("pulse1.aud"); }

		protected override void OnActivate()
		{
			Game.orderManager.IssueOrder(new Order("SonarPulse", Owner.PlayerActor));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SonarPulse")
			{
				// TODO: Reveal submarines

				// Should this play for all players?
				Sound.Play("sonpulse.aud");
				FinishActivate();
			}
		}
	}
}
