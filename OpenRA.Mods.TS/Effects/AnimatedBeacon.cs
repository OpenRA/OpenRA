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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Scripting;

namespace OpenRA.Mods.TS.Effects
{
	public class AnimatedBeacon : IEffect
	{
		readonly Player owner;
		readonly WPos position;
		readonly string beaconPalette;
		readonly bool isPlayerPalette;
		readonly Animation beacon;

		public AnimatedBeacon(Player owner, WPos position, int duration, string beaconPalette, bool isPlayerPalette, string beaconImage, string beaconSequence)
		{
			this.owner = owner;
			this.position = position;
			this.beaconPalette = beaconPalette;
			this.isPlayerPalette = isPlayerPalette;

			if (!string.IsNullOrEmpty(beaconSequence))
			{
				beacon = new Animation(owner.World, beaconImage);
				beacon.PlayRepeating(beaconSequence);
			}

			if (duration > 0)
				owner.World.Add(new DelayedAction(duration, () => owner.World.Remove(this)));
		}

		public void Tick(World world)
		{
			if (beacon != null)
				beacon.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (beacon == null)
				return Enumerable.Empty<IRenderable>();

			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				return Enumerable.Empty<IRenderable>();

			var palette = r.Palette(isPlayerPalette ? beaconPalette + owner.InternalName : beaconPalette);
			return beacon.Render(position, palette);
		}
	}
}
