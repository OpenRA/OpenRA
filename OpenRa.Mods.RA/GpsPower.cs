using System.Linq;
using OpenRa.Effects;
using OpenRa.Mods.RA.Effects;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(Actor self) { return new GpsPower(self, this); }
	}

	class GpsPower : SupportPower
	{
		public GpsPower(Actor self, GpsPowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging()
		{
			var launchSite = Owner.World.Actors
				.FirstOrDefault(a => a.Owner == Owner && a.traits.Contains<GpsLaunchSite>());

			if (launchSite == null)
				return;

			Owner.World.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(Owner, "satlnch1.aud");

				w.Add(new SatelliteLaunch(launchSite));
				w.Add(new DelayedAction((Info as GpsPowerInfo).RevealDelay * 25, 
					() => Owner.Shroud.HasGPS = true));
			});

			FinishActivate();
		}
	}

	// tag trait to identify the building
	class GpsLaunchSiteInfo : StatelessTraitInfo<GpsLaunchSite> { }
	class GpsLaunchSite { }
}
