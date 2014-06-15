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
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	public class Beacon : IEffect
	{
		readonly Player owner;
		readonly WPos position;
		readonly string palettePrefix;
		readonly string posterPalette;
		readonly Animation arrow;
		readonly Animation circles;
		readonly Animation poster;
		readonly Animation clock;

		static readonly int maxArrowHeight = 512;
		int arrowHeight = maxArrowHeight;
		int arrowSpeed = 50;

		// Player-placed beacons are removed after a delay
		public Beacon(Player owner, WPos position, int duration, string palettePrefix)
		{
			this.owner = owner;
			this.position = position;
			this.palettePrefix = palettePrefix;

			arrow = new Animation(owner.World, "beacon");
			circles = new Animation(owner.World, "beacon");

			arrow.Play("arrow");
			circles.Play("circles");

			if (duration > 0)
				owner.World.Add(new DelayedAction(duration, () => owner.World.Remove(this)));
		}

		// Support power beacons are expected to clean themselves up
		public Beacon(Player owner, WPos position, string palettePrefix, string posterType, string posterPalette, Func<float> clockFraction)
			: this(owner, position, -1, palettePrefix)
		{
			this.posterPalette = posterPalette;

			if (posterType != null)
			{
				poster = new Animation(owner.World, "beacon");
				poster.Play(posterType);

				if (clockFraction != null)
				{
					clock = new Animation(owner.World, "beacon");
					clock.PlayFetchIndex("clock", () => Exts.Clamp((int)(clockFraction() * (clock.CurrentSequence.Length - 1)), 0, clock.CurrentSequence.Length - 1));
				}
			}
		}

		public void Tick(World world)
		{
			arrowHeight += arrowSpeed;
			var clamped = arrowHeight.Clamp(0, maxArrowHeight);
			if (arrowHeight != clamped)
			{
				arrowHeight = clamped;
				arrowSpeed *= -1;
			}

			arrow.Tick();
			circles.Tick();

			if (clock != null)
				clock.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				yield break;

			var palette = r.Palette(palettePrefix + owner.InternalName);
			foreach (var a in circles.Render(position, palette))
				yield return a;
				
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
