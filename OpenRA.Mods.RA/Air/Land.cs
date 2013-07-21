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

			// The next move would overshoot, so just set the final position
			var moveDist = aircraft.MovementSpeed * 7 * 1024 / (Game.CellSize * 32);
			if (d.HorizontalLengthSquared < moveDist*moveDist)
			{
				aircraft.SetPosition(self, Target.CenterPosition);
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
