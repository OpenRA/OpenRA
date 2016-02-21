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
	public class Land : Activity
	{
		readonly Target target;
		readonly Aircraft plane;

		public Land(Actor self, Target t)
		{
			target = t;
			plane = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (!target.IsValidFor(self))
				Cancel(self);

			if (IsCanceled)
				return NextActivity;

			var d = target.CenterPosition - self.CenterPosition;

			// The next move would overshoot, so just set the final position
			var move = plane.FlyStep(plane.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				plane.SetPosition(self, target.CenterPosition);
				return NextActivity;
			}

			Fly.FlyToward(self, plane, d.Yaw.Facing, WDist.Zero);

			return this;
		}
	}
}
