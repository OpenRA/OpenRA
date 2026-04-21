#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("A beacon that is constructed from a circle sprite that is animated once and a moving arrow sprite.")]
	public class PlaceBeaconInfo : TraitInfo
	{
		public readonly int Duration = 750;

		public readonly string NotificationType = "Sounds";

		[NotificationReference(typeFromField: "NotificationType")]
		public readonly string Notification = "Beacon";

		public readonly bool IsPlayerPalette = true;

		[PaletteReference(nameof(IsPlayerPalette))]
		public readonly string Palette = "player";

		public readonly string BeaconImage = "beacon";

		[SequenceReference(nameof(BeaconImage))]
		public readonly string BeaconSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string ArrowSequence = "arrow";

		[SequenceReference(nameof(BeaconImage))]
		public readonly string CircleSequence = "circles";

		public override object Create(ActorInitializer init) { return new PlaceBeacon(init.Self, this); }
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

			self.World.AddFrameEndTask(w =>
			{
				if (playerBeacon != null)
					self.World.Remove(playerBeacon);

				playerBeacon = new Beacon(self.Owner, order.Target.CenterPosition, info.Duration, info.Palette, info.IsPlayerPalette,
					info.BeaconImage, info.BeaconSequence, info.ArrowSequence, info.CircleSequence);

				self.World.Add(playerBeacon);

				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					Game.Sound.PlayNotification(self.World.Map.Rules, null, info.NotificationType, info.Notification, self.World.RenderPlayer?.Faction.InternalName);

				if (radarPings != null)
				{
					if (playerRadarPing != null)
						radarPings.Remove(playerRadarPing);

					playerRadarPing = radarPings.Add(
						() => self.Owner.IsAlliedWith(self.World.RenderPlayer),
						order.Target.CenterPosition,
						self.OwnerColor(),
						info.Duration);
				}
			});
		}
	}
}
