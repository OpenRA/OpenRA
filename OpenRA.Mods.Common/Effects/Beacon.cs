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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Effects
{
	public class Beacon : IEffect, IScriptBindable, IEffectAboveShroud
	{
		const int MaxArrowHeight = 512;

		readonly Player owner;
		readonly WPos position;
		readonly Animation arrow, beacon, circles, clock, poster;
		readonly int duration;

		int delay;
		int arrowHeight = MaxArrowHeight;
		int arrowSpeed = 50;
		int tick;

		// Player-placed beacons are removed after a delay
		public Beacon(Player owner, WPos position, int duration, string beaconCollection, string beaconSequence,
			string arrowSprite, string circleSprite, int delay = 0)
		{
			this.owner = owner;
			this.position = position;
			this.duration = duration;
			this.delay = delay;

			if (!string.IsNullOrEmpty(beaconSequence))
			{
				beacon = new Animation(owner.World, beaconCollection);
				beacon.PlayRepeating(beaconSequence);
			}

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
		}

		// By default, support power beacons are expected to clean themselves up
		public Beacon(Player owner, WPos position, string posterCollection, string posterType,
			string beaconSequence, string arrowSequence, string circleSequence, string clockSequence, Func<float> clockFraction, int delay = 0, int duration = -1)
				: this(owner, position, duration, posterCollection, beaconSequence, arrowSequence, circleSequence, delay)
		{
			if (posterType != null)
			{
				poster = new Animation(owner.World, posterCollection);
				poster.Play(posterType);

				if (clockFraction != null)
				{
					clock = new Animation(owner.World, posterCollection);
					clock.PlayFetchIndex(clockSequence, () => ((int)(clockFraction() * (clock.CurrentSequence.Length - 1))).Clamp(0, clock.CurrentSequence.Length - 1));
				}
			}
		}

		void IEffect.Tick(World world)
		{
			if (delay-- > 0)
				return;

			arrowHeight += arrowSpeed;
			var clamped = arrowHeight.Clamp(0, MaxArrowHeight);
			if (arrowHeight != clamped)
			{
				arrowHeight = clamped;
				arrowSpeed *= -1;
			}

			arrow?.Tick();
			beacon?.Tick();
			circles?.Tick();
			clock?.Tick();

			if (duration > 0 && duration <= tick++)
				owner.World.AddFrameEndTask(w => w.Remove(this));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer r)
		{
			if (delay > 0)
				yield break;

			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				yield break;

			if (beacon != null)
				foreach (var a in beacon.Render(r, position, owner))
					yield return a;

			if (circles != null)
				foreach (var a in circles.Render(r, position, owner))
					yield return a;

			if (arrow != null)
				foreach (var a in arrow.Render(r, position + new WVec(0, 0, arrowHeight), owner))
					yield return a;

			if (poster != null)
			{
				foreach (var a in poster.Render(r, position, owner))
					yield return a;

				if (clock != null)
					foreach (var a in clock.Render(r, position, owner))
						yield return a;
			}
		}
	}
}
