
using System;

namespace OpenRA.Traits.Activities
{

	public class Wait: IActivity	
	{
	int remainingTicks;
		
		public Wait (int period) 
		{
			remainingTicks = period;
		}
		
		public IActivity Tick (Actor self)
		{
			if (remainingTicks-- == 0) return NextActivity;
			return this;
		}
		
		
		public void Cancel (Actor self)
		{
			remainingTicks = 0; NextActivity = null;
		}
		
		
		public IActivity NextActivity { get; set; }

	}
}
