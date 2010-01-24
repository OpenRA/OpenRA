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

	public class SonarPulsePower : SupportPower
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { }
		protected override void OnFinishCharging() { Sound.Play("pulse1.aud"); }
		protected override void OnActivate()
		{
			// Question: Is this method synced? or does it have to go via an order?
			
			// TODO: Reveal submarines
			
			// Should this play for all players?
			Sound.Play("sonpulse.aud");
			FinishActivate();
		}
	}
}
