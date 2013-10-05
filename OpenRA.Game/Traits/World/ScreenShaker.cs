#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ScreenShakerInfo : TraitInfo<ScreenShaker> { }

	public class ScreenShaker : ITick, IWorldLoaded
	{
		WorldRenderer worldRenderer;
		List<ShakeEffect> shakeEffects = new List<ShakeEffect>();
		int ticks = 0;

		public void WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		public void Tick(Actor self)
		{
			if (shakeEffects.Any())
			{
				worldRenderer.Viewport.Scroll(GetScrollOffset(), true);
				shakeEffects.RemoveAll(t => t.ExpiryTime == ticks);
			}

			ticks++;
		}

		public void AddEffect(int time, WPos position, int intensity)
		{
			shakeEffects.Add(new ShakeEffect { ExpiryTime = ticks + time, Position = position, Intensity = intensity });
		}

		float2 GetScrollOffset()
		{
			return GetIntensity() * new float2(
				(float)Math.Sin((ticks * 2 * Math.PI) / 4),
				(float)Math.Cos((ticks * 2 * Math.PI) / 5));
		}

		float GetIntensity()
		{
			var cp = worldRenderer.Position(worldRenderer.Viewport.CenterLocation);
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
	}
}
