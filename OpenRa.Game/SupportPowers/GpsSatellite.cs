using System.Linq;
using OpenRa.Game.Effects;
using OpenRa.Game.Traits;

namespace OpenRa.Game.SupportPowers
{
	class GpsSatellite : ISupportPowerImpl
	{
		const int revealDelay = 30 * 25;

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
