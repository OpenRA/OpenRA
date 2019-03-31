#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		readonly Aircraft aircraft;
		readonly int turnSpeedOverride;
		int remainingTicks;

		public FlyCircle(Actor self, int ticks = -1, int turnSpeedOverride = -1)
		{
			aircraft = self.Trait<Aircraft>();
			remainingTicks = ticks;
			this.turnSpeedOverride = turnSpeedOverride;
		}

		public override Activity Tick(Actor self)
		{
			if (remainingTicks == 0 || (NextActivity != null && remainingTicks < 0))
				return NextActivity;

			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceling)
				return NextActivity;

			if (remainingTicks > 0)
				remainingTicks--;

			// We can't possibly turn this fast
			var desiredFacing = aircraft.Facing + 64;
			Fly.FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, turnSpeedOverride);

			return this;
		}
	}
}
