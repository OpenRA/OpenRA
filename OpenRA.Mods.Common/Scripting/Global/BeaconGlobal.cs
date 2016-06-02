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

using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Beacon")]
	public class BeaconGlobal : ScriptGlobal
	{
		readonly RadarPings radarPings;

		public BeaconGlobal(ScriptContext context) : base(context)
		{
			radarPings = context.World.WorldActor.TraitOrDefault<RadarPings>();
		}

		[Desc("Creates a new beacon that stays for the specified time at the specified WPos. " +
			"Does not remove player set beacons, nor gets removed by placing them.")]
		public Beacon New(Player owner, WPos position, int duration = 30 * 25, bool showRadarPings = true)
		{
			var beacon = owner.PlayerActor.Info.TraitInfo<PlaceBeaconInfo>();
			var playerBeacon = new Beacon(owner, position, duration, beacon.Palette, beacon.IsPlayerPalette, beacon.BeaconImage, beacon.ArrowSequence, beacon.CircleSequence);
			owner.PlayerActor.World.AddFrameEndTask(w => w.Add(playerBeacon));

			if (showRadarPings && radarPings != null)
			{
				radarPings.Add(
					() => owner.IsAlliedWith(owner.World.RenderPlayer),
					position,
					owner.Color.RGB,
					duration);
			}

			return playerBeacon;
		}
	}
}
