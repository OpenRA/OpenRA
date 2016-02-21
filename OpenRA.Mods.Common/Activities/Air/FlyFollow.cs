#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyFollow : Activity
	{
		Target target;
		Aircraft plane;
		WDist minRange;
		WDist maxRange;

		public FlyFollow(Actor self, Target target, WDist minRange, WDist maxRange)
		{
			this.target = target;
			plane = self.Trait<Aircraft>();
			this.minRange = minRange;
			this.maxRange = maxRange;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.IsInRange(self.CenterPosition, maxRange) && !target.IsInRange(self.CenterPosition, minRange))
			{
				Fly.FlyToward(self, plane, plane.Facing, plane.Info.CruiseAltitude);
				return this;
			}

			return ActivityUtils.SequenceActivities(new Fly(self, target, minRange, maxRange), this);
		}
	}
}
