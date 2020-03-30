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
	public class FlyTimed : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist cruiseAltitude;
		int remainingTicks;

		public FlyTimed(int ticks, Actor self)
		{
			remainingTicks = ticks;
			aircraft = self.Trait<Aircraft>();
			cruiseAltitude = aircraft.Info.CruiseAltitude;
		}

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return true;
			}

			if (IsCanceling || remainingTicks-- == 0)
				return true;

			Fly.FlyTick(self, aircraft, aircraft.Facing, cruiseAltitude);

			return false;
		}
	}
}
