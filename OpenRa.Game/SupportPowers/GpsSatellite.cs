using System.Linq;
using OpenRa.Effects;
using OpenRa.Traits;

namespace OpenRa.SupportPowers
{
	class GpsSatellite : ISupportPowerImpl
	{
		const int revealDelay = 15 * 25;

		public void OnFireNotification(Actor a, int2 xy) { }
		public void IsChargingNotification(SupportPower p) { }
		public void IsReadyNotification(SupportPower p)
		{
			var launchSite = p.Owner.World.Actors
				.FirstOrDefault(a => a.Owner == p.Owner && a.traits.Contains<GpsLaunchSite>());

			if (launchSite == null)
				return;

			p.Owner.World.AddFrameEndTask(w =>
			{
				w.Add(new SatelliteLaunch(launchSite));
				w.Add(new DelayedAction(revealDelay, () => p.Owner.Shroud.HasGPS = true));
			});

			p.FinishActivate();
		}
		
		public void Activate(SupportPower p) {}
	}
}
