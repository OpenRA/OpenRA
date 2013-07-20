#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class Land : Activity
	{
		Target Target;

		public Land(Target t) { Target = t; }

		public override Activity Tick(Actor self)
		{
			if (!Target.IsValid)
				Cancel(self);

			if (IsCanceled)
				return NextActivity;

			var aircraft = self.Trait<Aircraft>();
			var d = Target.CenterPosition - self.CenterPosition;

			// close enough (1/16 cell)
			// TODO: TickMove may overshoot if the aircraft speed is too high
			if (d.HorizontalLengthSquared < 4096)
			{
				aircraft.SetPxPosition(self, PPos.FromWPos(Target.CenterPosition));
				return NextActivity;
			}

			if (aircraft.Altitude > 0)
				--aircraft.Altitude;

			var desiredFacing = Util.GetFacing(d, aircraft.Facing);
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);
			aircraft.TickMove(PSubPos.PerPx * aircraft.MovementSpeed, aircraft.Facing);

			return this;
		}
	}
}
