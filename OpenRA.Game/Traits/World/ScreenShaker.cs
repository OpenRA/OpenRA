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
using System.Linq;
using System.Numerics;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[TraitLocation(SystemActors.World)]
	public class ScreenShakerInfo : TraitInfo
	{
		public readonly Vector2 MinMultiplier = new(-3, -3);
		public readonly Vector2 MaxMultiplier = new(3, 3);

		public override object Create(ActorInitializer init) { return new ScreenShaker(this); }
	}

	public class ScreenShaker : ITick, IWorldLoaded
	{
		readonly ScreenShakerInfo info;
		WorldRenderer worldRenderer;
		readonly List<ShakeEffect> shakeEffects = [];
		int ticks = 0;
		Vector2 previousOffset = Vector2.Zero;

		public ScreenShaker(ScreenShakerInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		void ITick.Tick(Actor self)
		{
			shakeEffects.RemoveAll(t => t.ExpiryTime == ticks);

			var newOffset = shakeEffects.Count > 0 ? GetScrollOffset() : Vector2.Zero;
			if (newOffset != previousOffset)
			{
				worldRenderer.Viewport.Scroll(newOffset - previousOffset, true);
				previousOffset = newOffset;
			}

			ticks++;
		}

		public void AddEffect(int time, WPos position, int intensity)
		{
			AddEffect(time, position, intensity, Vector2.One);
		}

		public void AddEffect(int time, WPos position, int intensity, Vector2 multiplier)
		{
			shakeEffects.Add(new ShakeEffect { ExpiryTime = ticks + time, Position = position, Intensity = intensity, Multiplier = multiplier });
		}

		Vector2 GetScrollOffset()
		{
			return GetMultiplier() * GetIntensity() * new Vector2(
				(float)Math.Sin(ticks * 2 * Math.PI / 4),
				(float)Math.Cos(ticks * 2 * Math.PI / 5));
		}

		Vector2 GetMultiplier()
		{
			return Vector2.Clamp(shakeEffects.Aggregate(Vector2.Zero, (sum, next) => sum + next.Multiplier),
				info.MinMultiplier, info.MaxMultiplier);
		}

		float GetIntensity()
		{
			var cp = worldRenderer.Viewport.CenterPosition;
			var intensity = 100 * 1024 * 1024 * shakeEffects.Sum(
				e => (float)e.Intensity / (e.Position - cp).LengthSquared);

			return Math.Min(intensity, 10);
		}
	}

	struct ShakeEffect
	{
		public int ExpiryTime;
		public WPos Position;
		public int Intensity;
		public Vector2 Multiplier;
	}
}
