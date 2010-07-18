#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.self, this); }
	}

	class GpsPower : SupportPower
	{
		public GpsPower(Actor self, GpsPowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging()
		{
			var launchSite = Owner.World.Queries.OwnedBy[Owner]
				.FirstOrDefault(a => a.traits.Contains<GpsLaunchSite>());

			if (launchSite == null)
				return;

			Owner.World.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(Owner, Info.LaunchSound);

				w.Add(new SatelliteLaunch(launchSite));
				w.Add(new DelayedAction((Info as GpsPowerInfo).RevealDelay * 25, 
					() => Owner.Shroud.Disabled = true));
			});

			FinishActivate();
		}
	}

	// tag trait to identify the building
	class GpsLaunchSiteInfo : TraitInfo<GpsLaunchSite> { }
	class GpsLaunchSite { }
}
