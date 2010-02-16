#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

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
			var launchSite = Owner.World.Queries.OwnedBy[Owner]
				.FirstOrDefault(a => a.traits.Contains<GpsLaunchSite>());

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
