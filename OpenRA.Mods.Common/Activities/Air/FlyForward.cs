#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class FlyForward : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist cruiseAltitude;
		readonly int flyTicks;
		int remainingDistance;
		int ticks;

		FlyForward(Actor self)
		{
			aircraft = self.Trait<Aircraft>();
			cruiseAltitude = aircraft.Info.CruiseAltitude;
		}

		public FlyForward(Actor self, int ticks = -1)
			: this(self)
		{
			flyTicks = ticks;
		}

		public FlyForward(Actor self, WDist distance)
			: this(self)
		{
			remainingDistance = distance.Length;
		}

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return true;
			}

			// Having flyTicks < 0 is valid and means the actor flies until this activity is canceled
			if (IsCanceling || (flyTicks > 0 && ticks++ >= flyTicks) || (flyTicks == 0 && remainingDistance <= 0))
				return true;

			// FlyTick moves the aircraft while FlyStep calculates how far we are moving
			if (remainingDistance != 0)
				remainingDistance -= aircraft.FlyStep(aircraft.Facing).HorizontalLength;

			Fly.FlyTick(self, aircraft, aircraft.Facing, cruiseAltitude);
			return false;
		}
	}
}
