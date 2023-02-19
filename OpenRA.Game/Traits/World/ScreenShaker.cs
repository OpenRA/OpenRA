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
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[TraitLocation(SystemActors.World)]
	public class ScreenShakerInfo : TraitInfo
	{
		public readonly float2 MinMultiplier = new float2(-3, -3);
		public readonly float2 MaxMultiplier = new float2(3, 3);

		public override object Create(ActorInitializer init) { return new ScreenShaker(this); }
	}

	public class ScreenShaker : ITick, IWorldLoaded
	{
		readonly ScreenShakerInfo info;
		WorldRenderer worldRenderer;
		readonly List<ShakeEffect> shakeEffects = new List<ShakeEffect>();
		int ticks = 0;

		public ScreenShaker(ScreenShakerInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		void ITick.Tick(Actor self)
		{
			if (shakeEffects.Count > 0)
			{
				worldRenderer.Viewport.Scroll(GetScrollOffset(), true);
				shakeEffects.RemoveAll(t => t.ExpiryTime == ticks);
			}

			ticks++;
		}

		public void AddEffect(int time, WPos position, int intensity)
		{
			AddEffect(time, position, intensity, new float2(1, 1));
		}

		public void AddEffect(int time, WPos position, int intensity, float2 multiplier)
		{
			shakeEffects.Add(new ShakeEffect { ExpiryTime = ticks + time, Position = position, Intensity = intensity, Multiplier = multiplier });
		}

		float2 GetScrollOffset()
		{
			return GetMultiplier() * GetIntensity() * new float2(
				(float)Math.Sin(ticks * 2 * Math.PI / 4),
				(float)Math.Cos(ticks * 2 * Math.PI / 5));
		}

		float2 GetMultiplier()
		{
			return shakeEffects.Aggregate(float2.Zero, (sum, next) => sum + next.Multiplier)
				.Constrain(info.MinMultiplier, info.MaxMultiplier);
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
		public float2 Multiplier;
	}
}
