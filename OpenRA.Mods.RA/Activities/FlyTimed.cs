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
	public class FlyTimed : IActivity
	{
		public IActivity NextActivity { get; set; }
		int remainingTicks;

		public FlyTimed(int ticks) { remainingTicks = ticks; }

		public IActivity Tick(Actor self)
		{
			var targetAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			if (remainingTicks-- == 0) return NextActivity;
			FlyUtil.Fly(self, targetAltitude);
			return this;
		}

		public void Cancel(Actor self) { remainingTicks = 0; NextActivity = null; }
	}

	public class FlyOffMap : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		public bool Interruptible = true;

		public IActivity Tick(Actor self)
		{
			var targetAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			if (isCanceled || !self.World.Map.IsInMap(self.Location)) return NextActivity;
			FlyUtil.Fly(self, targetAltitude);
			return this;
		}

		public void Cancel(Actor self)
		{
			if (Interruptible)
			{
				isCanceled = true; 
				NextActivity = null;
			}
		}
	}

}
