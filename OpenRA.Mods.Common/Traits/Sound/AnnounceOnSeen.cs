#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Players will be notified when this actor becomes visible to them.")]
	public class AnnounceOnSeenInfo : ITraitInfo
	{
		[Desc("Should there be a radar ping on enemies' radar at the actor's location when they see him")]
		public readonly bool PingRadar = false;

		public readonly string Notification = null;

		public object Create(ActorInitializer init) { return new AnnounceOnSeen(init.Self, this); }
	}

	public class AnnounceOnSeen : INotifyDiscovered
	{
		public readonly AnnounceOnSeenInfo Info;

		readonly Lazy<RadarPings> radarPings;

		public AnnounceOnSeen(Actor self, AnnounceOnSeenInfo info)
		{
			Info = info;
			radarPings = Exts.Lazy(() => self.World.WorldActor.Trait<RadarPings>());
		}

		public void OnDiscovered(Actor self, Player discoverer, bool playNotification)
		{
			if (!playNotification || discoverer != self.World.RenderPlayer)
				return;

			// Audio notification
			if (discoverer != null && !string.IsNullOrEmpty(Info.Notification))
				Sound.PlayNotification(self.World.Map.Rules, discoverer, "Speech", Info.Notification, discoverer.Country.Race);

			// Radar notificaion
			if (Info.PingRadar && radarPings.Value != null)
				radarPings.Value.Add(() => true, self.CenterPosition, Color.Red, 50);
		}
	}
}