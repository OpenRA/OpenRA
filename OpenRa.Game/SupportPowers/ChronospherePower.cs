using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.Orders;
using OpenRa.Traits;

namespace OpenRa.SupportPowers
{
	class ChronospherePower : ISupportPowerImpl
	{
		public void IsReadyNotification(SupportPower p) { Sound.Play("chrordy1.aud"); }
		public void IsChargingNotification(SupportPower p) { Sound.Play("chrochr1.aud"); }

		public void OnFireNotification(Actor target, int2 xy)
		{
			p.FinishActivate();
			Game.controller.CancelInputMode();

			Sound.Play("chrono2.aud");

			// Play chronosphere active anim
			var chronosphere = target.World.Actors.Where(a => a.Owner == p.Owner && a.traits.Contains<Chronosphere>()).FirstOrDefault();
			if (chronosphere != null)
				Game.controller.AddOrder(Order.PlayAnimation(chronosphere, "active"));
			
			// Trigger screen desaturate effect
			foreach (var a in target.World.Actors.Where(a => a.traits.Contains<ChronoshiftPaletteEffect>()))
				a.traits.Get<ChronoshiftPaletteEffect>().DoChronoshift();
		}
		SupportPower p;
		public void Activate(SupportPower p)
		{
			this.p = p;
			Game.controller.orderGenerator = new ChronosphereSelectOrderGenerator(p);
			Sound.Play("slcttgt1.aud");
		}
	}
}
