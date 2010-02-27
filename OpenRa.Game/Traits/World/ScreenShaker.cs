
using System;
using OpenRa.Traits;
using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class ScreenShakerInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new ScreenShaker( self ); }
	}
	
	public class ScreenShaker : ITick
	{
		int ticks = 0;
		private static List<Tuple<int, float2, int>> shakeEffects = new List<Tuple<int, float2, int>>();
		
		public ScreenShaker (Actor self){}
		
		public void Tick (Actor self)
		{
			Game.viewport.Scroll(getScrollOffset());
			UpdateList();
			ticks++;
		}
		
		private void UpdateList()
		{
			var toRemove = new List<Tuple<int, float2, int>>();
			
			for (int i = 0; i < shakeEffects.Count; i++){
				var tuple = shakeEffects[i];
				tuple.a = tuple.a - 1;
				shakeEffects[i] = tuple;
				
				if (tuple.a == 0) 
					toRemove.Add(tuple);
			}
			
			foreach(Tuple<int, float2, int> t in toRemove){
				shakeEffects.Remove(t);
			}
		}
		
		public static void RegisterShakeEffect(int time, float2 position, int intensity)
		{
			shakeEffects.Add(Tuple.New<int, float2, int>(time, position, intensity));
		}
		
		public float2 getScrollOffset()
		{
			int xFreq = 4;
			int yFreq = 5;
			
			return GetIntensity() * new float2( (float) Math.Sin((ticks*2*Math.PI)/xFreq) , (float) Math.Cos((ticks*2*Math.PI)/yFreq));
		}
		
		public float GetIntensity()
		{
			float intensity = 0;
			foreach(Tuple<int, float2, int> tuple in shakeEffects)
			{
				intensity += (24*24*100*tuple.c)/( (tuple.b.X - Game.viewport.Location.X) * (tuple.b.X - Game.viewport.Location.X) 
				                + (tuple.b.Y - Game.viewport.Location.Y) * (tuple.b.Y - Game.viewport.Location.Y) ); 
			}
			return Math.Min(intensity, 10);	
		}
		
	}
}
