using System.Linq;
using OpenRa.Game.Effects;
using OpenRa.Game.Traits;

namespace OpenRa.Game.SupportPowers
{
	class GpsSatellite : ISupportPowerImpl
	{
		const int revealDelay = 15 * 25;

		public void OnFireNotification(Actor a, int2 xy) { }
		public void IsChargingNotification(SupportPower p) { }
		public void IsReadyNotification(SupportPower p)
		{
			// Power is auto-activated
			Activate(p);
		}
		
		public void Activate(SupportPower p)
		{
			var launchSite = Game.world.Actors
				.FirstOrDefault( a => a.Owner == p.Owner && a.traits.Contains<GpsLaunchSite>() );

			if (launchSite == null)
				return;

			Game.world.AddFrameEndTask(w =>
				{
					w.Add(new SatelliteLaunch(launchSite));
					w.Add(new DelayedAction(revealDelay, () => p.Owner.Shroud.HasGPS = true));
				});

			p.FinishActivate();
		}
	}
}
