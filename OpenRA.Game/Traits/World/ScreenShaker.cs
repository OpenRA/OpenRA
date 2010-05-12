#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class ScreenShakerInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new ScreenShaker(); }
	}
	
	public class ScreenShaker : ITick
	{
		int ticks = 0;
        List<ShakeEffect> shakeEffects = new List<ShakeEffect>();
		
		public void Tick (Actor self)
		{
			Game.viewport.Scroll(getScrollOffset());
			shakeEffects.RemoveAll(t => t.ExpiryTime == ticks);
			ticks++;
		}
		
		public void AddEffect(int time, float2 position, int intensity)
		{
			shakeEffects.Add(new ShakeEffect { ExpiryTime = ticks + time, Position = position, Intensity = intensity });
		}
		
		public float2 getScrollOffset()
		{
			int xFreq = 4;
			int yFreq = 5;
			
			return GetIntensity() * new float2( 
				(float) Math.Sin((ticks*2*Math.PI)/xFreq) , 
				(float) Math.Cos((ticks*2*Math.PI)/yFreq));
		}
		
		public float GetIntensity()
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
