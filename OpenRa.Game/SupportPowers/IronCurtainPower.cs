using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Orders;
using OpenRa.Traits;

namespace OpenRa.SupportPowers
{
	class IronCurtainPower : ISupportPowerImpl
	{
		public void IsReadyNotification(SupportPower p)
		{
			Sound.Play("ironrdy1.aud");
		}

		public void IsChargingNotification(SupportPower p)
		{
			Sound.Play("ironchg1.aud");
		}
		
		public void OnFireNotification(Actor target, int2 xy)
		{
			p.FinishActivate();
			Game.controller.CancelInputMode();
			Sound.Play("ironcur9.aud");
			
			// Play active anim
			var ironCurtain = Game.world.Actors
				.Where(a => a.Owner == p.Owner && a.traits.Contains<IronCurtain>())
				.FirstOrDefault();
			if (ironCurtain != null)
				Game.controller.AddOrder(Order.PlayAnimation(ironCurtain, "active"));

		}
		SupportPower p;
		public void Activate(SupportPower p)
		{
			this.p = p;
			// Pick a building to use
			Game.controller.orderGenerator = new IronCurtainOrderGenerator(p);
			Sound.Play("slcttgt1.aud");
		}
	}
}
