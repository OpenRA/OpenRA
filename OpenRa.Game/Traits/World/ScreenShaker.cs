
using System;
using System.Linq;
using OpenRa.Traits;
using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class ScreenShakerInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new ScreenShaker(); }
	}
	
	public class ScreenShaker : ITick
	{
		static int ticks = 0;
		static List<Tuple<int, float2, int>> shakeEffects = new List<Tuple<int, float2, int>>();
		
		public void Tick (Actor self)
		{
			Game.viewport.Scroll(getScrollOffset());
			shakeEffects.RemoveAll(t => t.a == ticks);
			ticks++;
		}
		
		public static void RegisterShakeEffect(int time, float2 position, int intensity)
		{
			shakeEffects.Add(Tuple.New(ticks + time, position, intensity));
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
				e => e.c / (e.b - cp).LengthSquared);

			return Math.Min(intensity, 10);	
		}
		
	}
}
