#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyCircle : Activity
	{
		readonly Aircraft plane;
		readonly WDist cruiseAltitude;
		int remainingTicks;

		public FlyCircle(Actor self, int ticks = -1)
		{
			plane = self.Trait<Aircraft>();
			cruiseAltitude = plane.Info.CruiseAltitude;
			remainingTicks = ticks;
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (plane.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled)
				return NextActivity;

			if (remainingTicks > 0)
				remainingTicks--;
			else if (remainingTicks == 0)
				return NextActivity;

			// We can't possibly turn this fast
			var desiredFacing = plane.Facing + 64;
			Fly.FlyToward(self, plane, desiredFacing, cruiseAltitude);

			return this;
		}
	}
}
