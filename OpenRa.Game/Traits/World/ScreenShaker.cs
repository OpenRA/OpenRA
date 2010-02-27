
using System;
using OpenRa.Traits;

namespace OpenRa.Traits
{
	class ScreenShakerInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new ScreenShaker( self ); }
	}
	
	public class ScreenShaker : ITick
	{
		int ticks = 0;
		
		public ScreenShaker (Actor self){}
		
		public void Tick (Actor self)
		{
			Game.viewport.Scroll(getScrollOffset());
			ticks++;
		}
		
		//public void registerShakeEffect(float2 position, int time)
		//{
		//}
		
		public float2 getScrollOffset()
		{
			int xFreq = 4;
			int yFreq = 5;
			int intensity = 3;
			
			return intensity * new float2( (float) Math.Sin((ticks*2*Math.PI)/xFreq) , (float) Math.Cos((ticks*2*Math.PI)/yFreq));
		}
		
	}
}
