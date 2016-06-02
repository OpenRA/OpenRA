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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Effects
{
	public class Beacon : IEffect, IScriptBindable
	{
		static readonly int MaxArrowHeight = 512;

		readonly Player owner;
		readonly WPos position;
		readonly string beaconPalette;
		readonly bool isPlayerPalette;
		readonly string posterPalette;
		readonly Animation arrow;
		readonly Animation circles;
		readonly Animation poster;
		readonly Animation clock;

		int arrowHeight = MaxArrowHeight;
		int arrowSpeed = 50;

		// Player-placed beacons are removed after a delay
		public Beacon(Player owner, WPos position, int duration, string beaconPalette, bool isPlayerPalette, string beaconCollection, string arrowSprite, string circleSprite)
		{
			this.owner = owner;
			this.position = position;
			this.beaconPalette = beaconPalette;
			this.isPlayerPalette = isPlayerPalette;

			if (!string.IsNullOrEmpty(arrowSprite))
			{
				arrow = new Animation(owner.World, beaconCollection);
				arrow.Play(arrowSprite);
			}

			if (!string.IsNullOrEmpty(circleSprite))
			{
				circles = new Animation(owner.World, beaconCollection);
				circles.Play(circleSprite);
			}

			if (duration > 0)
				owner.World.AddFrameEndTask(w => w.Add(new DelayedAction(duration, () => owner.World.Remove(this))));
		}

		// Support power beacons are expected to clean themselves up
		public Beacon(Player owner, WPos position, bool isPlayerPalette, string palette, string posterCollection, string posterType, string posterPalette,
			string arrowSequence, string circleSequence, string clockSequence, Func<float> clockFraction)
				: this(owner, position, -1, palette, isPlayerPalette, posterCollection, arrowSequence, circleSequence)
		{
			this.posterPalette = posterPalette;

			if (posterType != null)
			{
				poster = new Animation(owner.World, posterCollection);
				poster.Play(posterType);

				if (clockFraction != null)
				{
					clock = new Animation(owner.World, posterCollection);
					clock.PlayFetchIndex(clockSequence, () => Exts.Clamp((int)(clockFraction() * (clock.CurrentSequence.Length - 1)), 0, clock.CurrentSequence.Length - 1));
				}
			}
		}

		public void Tick(World world)
		{
			arrowHeight += arrowSpeed;
			var clamped = arrowHeight.Clamp(0, MaxArrowHeight);
			if (arrowHeight != clamped)
			{
				arrowHeight = clamped;
				arrowSpeed *= -1;
			}

			if (arrow != null)
				arrow.Tick();

			if (circles != null)
				circles.Tick();

			if (clock != null)
				clock.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				yield break;

			var palette = r.Palette(isPlayerPalette ? beaconPalette + owner.InternalName : beaconPalette);

			if (circles != null)
				foreach (var a in circles.Render(position, palette))
					yield return a;

			if (arrow != null)
				foreach (var a in arrow.Render(position + new WVec(0, 0, arrowHeight), palette))
					yield return a;

			if (poster != null)
			{
				foreach (var a in poster.Render(position, r.Palette(posterPalette)))
					yield return a;

				if (clock != null)
					foreach (var a in clock.Render(position, r.Palette(posterPalette)))
						yield return a;
			}
		}
	}
}
