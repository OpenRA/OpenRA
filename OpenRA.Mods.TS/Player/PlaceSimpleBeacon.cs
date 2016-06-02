#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.TS.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	[Desc("A beacon that consists of a single sprite that can be animated.")]
	public class PlaceSimpleBeaconInfo : ITraitInfo
	{
		public readonly int Duration = 30 * 25;
		public readonly string NotificationType = "Sounds";
		public readonly string Notification = "Beacon";

		public readonly bool IsPlayerPalette = false;
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "effect";

		public readonly string BeaconImage = "beacon";
		[SequenceReference("BeaconImage")] public readonly string BeaconSequence = "idle";

		public object Create(ActorInitializer init) { return new PlaceSimpleBeacon(init.Self, this); }
	}

	public class PlaceSimpleBeacon : IResolveOrder
	{
		readonly PlaceSimpleBeaconInfo info;
		readonly RadarPings radarPings;

		AnimatedBeacon playerBeacon;
		RadarPing playerRadarPing;

		public PlaceSimpleBeacon(Actor self, PlaceSimpleBeaconInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "PlaceBeacon")
				return;

			var pos = self.World.Map.CenterOfCell(order.TargetLocation);

			self.World.AddFrameEndTask(w =>
			{
				if (playerBeacon != null)
					self.World.Remove(playerBeacon);

				playerBeacon = new AnimatedBeacon(self.Owner, pos, info.Duration, info.Palette, info.IsPlayerPalette, info.BeaconImage, info.BeaconSequence);
				self.World.Add(playerBeacon);

				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					Game.Sound.PlayNotification(self.World.Map.Rules, null, info.NotificationType, info.Notification,
						self.World.RenderPlayer != null ? self.World.RenderPlayer.Faction.InternalName : null);

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
