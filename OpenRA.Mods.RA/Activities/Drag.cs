#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Drag : IActivity
	{
		public IActivity NextActivity { get; set; }

		float2 endLocation;
		float2 startLocation;
		int length;

		public Drag(float2 start, float2 end, int length)
		{
			startLocation = start;
			endLocation = end;
			this.length = length;
		}
		
		int ticks = 0;
		public IActivity Tick( Actor self )
		{
			self.CenterLocation = float2.Lerp(startLocation, endLocation, (float)ticks/(length-1));
			
			if (++ticks >= length)
				return NextActivity;
			
			return this;
		}

		public void Cancel(Actor self) {	}
	}
}
