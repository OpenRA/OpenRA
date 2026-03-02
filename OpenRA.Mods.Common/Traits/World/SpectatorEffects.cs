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

using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Handles spectator beacon and waypoint effects broadcast over the network.",
		"Requires PlaceBeacon on the player actor to define beacon appearance and duration.")]
	public class SpectatorEffectsInfo : TraitInfo<SpectatorEffects> { }

	public class SpectatorEffects : INotifySpectatorBeacon, INotifySpectatorWaypoint
	{
		RadarPings radarPings;

		void INotifySpectatorBeacon.SpectatorBeaconPlaced(World world, WPos position, Color color, string spectatorName)
		{
			var spectatorPlayer = world.Players.FirstOrDefault(p => p.Spectating && p.NonCombatant);
			if (spectatorPlayer == null)
				return;

			var beaconInfo = spectatorPlayer.PlayerActor.Info.TraitInfoOrDefault<PlaceBeaconInfo>();
			if (beaconInfo == null)
				return;

			radarPings ??= world.WorldActor.TraitOrDefault<RadarPings>();

			world.AddFrameEndTask(w =>
			{
				var beacon = new Beacon(spectatorPlayer, position, beaconInfo.Duration,
					"effect", false,
					beaconInfo.BeaconImage, beaconInfo.BeaconSequence, beaconInfo.ArrowSequence, beaconInfo.CircleSequence, spectatorName: spectatorName);

				w.Add(beacon);

				if (world.RenderPlayer == null || world.RenderPlayer.Spectating)
					Game.Sound.PlayNotification(world.Map.Rules, null, beaconInfo.NotificationType, beaconInfo.Notification, null);

				radarPings?.Add(
					() => world.RenderPlayer == null || world.RenderPlayer.Spectating,
					position,
					color,
					beaconInfo.Duration);
			});
		}

		void INotifySpectatorWaypoint.SpectatorWaypointDrawn(World world, IReadOnlyList<WPos> waypoints, Color color, string spectatorName)
		{
			if (waypoints.Count < 2)
				return;

			var spectatorPlayer = world.Players.FirstOrDefault(p => p.Spectating && p.NonCombatant);
			var duration = spectatorPlayer?.PlayerActor.Info.TraitInfoOrDefault<PlaceBeaconInfo>()?.Duration ?? 750;

			world.AddFrameEndTask(w => w.Add(new SpectatorWaypointEffect(waypoints, duration, color, spectatorName)));
		}
	}
}
