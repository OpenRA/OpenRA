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
	public class Wait : IActivity
	{
		int remainingTicks;
		bool interruptable = true;
		
		public Wait(int period) { remainingTicks = period; }
		public Wait(int period, bool interruptable)
		{
			remainingTicks = period;
			this.interruptable = interruptable;
		}
		
		public IActivity Tick(Actor self)
		{
			if (remainingTicks-- == 0) return NextActivity;
			return this;
		}

		public void Cancel(Actor self)
		{
			if (!interruptable)
				return;
			
			remainingTicks = 0;
			NextActivity = null;
		}
		public IActivity NextActivity { get; set; }
	}
}
