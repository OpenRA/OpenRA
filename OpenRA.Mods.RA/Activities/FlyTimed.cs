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
	public class FlyTimed : CancelableActivity
	{
		int remainingTicks;

		public FlyTimed(int ticks) { remainingTicks = ticks; }

		public override IActivity Tick(Actor self)
		{
			if( IsCanceled ) return NextActivity;
			var targetAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			if (remainingTicks-- == 0) return NextActivity;
			FlyUtil.Fly(self, targetAltitude);
			return this;
		}
	}

	public class FlyOffMap : IActivity
	{
		IActivity NextActivity { get; set; }
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

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}
	}
}
