#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PlaceBeaconInfo : ITraitInfo
	{
		public readonly int Duration = 30 * 25;
		public readonly string NotificationType = "Sounds";
		public readonly string Notification = "Beacon";
		public readonly string PalettePrefix = "player";

		public object Create(ActorInitializer init) { return new PlaceBeacon(init.self, this); }
	}

	public class PlaceBeacon : IResolveOrder
	{
		readonly PlaceBeaconInfo info;
		readonly RadarPings radarPings;

		Beacon playerBeacon;
		RadarPing playerRadarPing;

		public PlaceBeacon(Actor self, PlaceBeaconInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "PlaceBeacon")
				return;

			var pos = order.TargetLocation.CenterPosition;

			self.World.AddFrameEndTask(w =>
			{
				if (playerBeacon != null)
					self.World.Remove(playerBeacon);

				playerBeacon = new Beacon(self.Owner, pos, info.Duration, info.PalettePrefix);
				self.World.Add(playerBeacon);

				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					Sound.PlayNotification(null, info.NotificationType, info.Notification,
						self.World.RenderPlayer != null ? self.World.RenderPlayer.Country.Race : null);

				if (radarPings != null)
				{
					if (playerRadarPing != null)
						radarPings.Remove(playerRadarPing);

					playerRadarPing = radarPings.Add(
						() => self.Owner.IsAlliedWith(self.World.RenderPlayer),
						pos,
						self.Owner.Color.RGB,
						info.Duration);
				}
			});
		}
	}
}
