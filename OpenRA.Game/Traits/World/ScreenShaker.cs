#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class ScreenShakerInfo : TraitInfo<ScreenShaker> {}
	
	public class ScreenShaker : ITick
	{
		int ticks = 0;
        List<ShakeEffect> shakeEffects = new List<ShakeEffect>();
		
		public void Tick (Actor self)
		{
			if(shakeEffects.Any()){
				Game.viewport.Scroll(GetScrollOffset(), true);
				shakeEffects.RemoveAll(t => t.ExpiryTime == ticks);
			}
			ticks++;
		}
		
		public void AddEffect(int time, float2 position, int intensity)
		{
			shakeEffects.Add(new ShakeEffect { ExpiryTime = ticks + time, Position = position, Intensity = intensity });
		}
		
		float2 GetScrollOffset()
		{
			int xFreq = 4;
			int yFreq = 5;
			
			return GetIntensity() * new float2( 
				(float) Math.Sin((ticks*2*Math.PI)/xFreq) , 
				(float) Math.Cos((ticks*2*Math.PI)/yFreq));
		}
		
		float GetIntensity()
		{
			var cp = Game.viewport.Location 
				+ .5f * new float2(Game.viewport.Width, Game.viewport.Height);

			var intensity = 24 * 24 * 100 * shakeEffects.Sum(
				e => e.Intensity / (e.Position - cp).LengthSquared);

			return Math.Min(intensity, 10);	
		}
	}

	class ShakeEffect { public int ExpiryTime; public float2 Position; public int Intensity; }
}
