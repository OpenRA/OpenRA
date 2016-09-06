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
	public class AnimatedBeacon : IEffect, IEffectAboveShroud
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

		void IEffect.Tick(World world)
		{
			if (beacon != null)
				beacon.Tick();
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer r)
		{
			if (beacon == null)
				return SpriteRenderable.None;

			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				return SpriteRenderable.None;

			var palette = r.Palette(isPlayerPalette ? beaconPalette + owner.InternalName : beaconPalette);
			return beacon.Render(position, palette);
		}
	}
}
