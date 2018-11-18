#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class HeliFlyCircle : Activity
	{
		readonly Aircraft aircraft;
		readonly int turnSpeedOverride;

		public HeliFlyCircle(Actor self, int turnSpeedOverride = -1)
		{
			aircraft = self.Trait<Aircraft>();
			this.turnSpeedOverride = turnSpeedOverride;
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled)
				return NextActivity;

			if (HeliFly.AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

			var move = aircraft.FlyStep(aircraft.Facing);
			aircraft.SetPosition(self, aircraft.CenterPosition + move);

			var desiredFacing = aircraft.Facing + 64;
			var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			return this;
		}
	}
}
