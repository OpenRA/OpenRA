using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class GpsLaunchSiteInfo : ITraitInfo
	{
		public object Create(Actor self) { return new GpsLaunchSite(self); }
	}

	class GpsLaunchSite { public GpsLaunchSite(Actor self) { } }
}
