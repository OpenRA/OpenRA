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
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class Fly : Activity
	{
		readonly WPos pos;

		Fly(WPos pos) { this.pos = pos; }

		public static Fly ToPos(WPos pos) { return new Fly(pos); }
		public static Fly ToCell(CPos pos) { return new Fly(pos.CenterPosition); }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			// Close enough (ported from old code which checked length against sqrt(50) px)
			var d = pos - self.CenterPosition;
			if (d.HorizontalLengthSquared < 91022)
				return NextActivity;

			var aircraft = self.Trait<Aircraft>();
			var cruiseAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			var desiredFacing = Util.GetFacing(d, aircraft.Facing);
			if (aircraft.Altitude == cruiseAltitude)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);

			if (aircraft.Altitude < cruiseAltitude)
				++aircraft.Altitude;

			FlyUtil.Fly(self, cruiseAltitude);
			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(pos);
		}
	}

	public static class FlyUtil
	{
		public static void Fly(Actor self, int desiredAltitude)
		{
			var aircraft = self.Trait<Aircraft>();
			aircraft.TickMove(PSubPos.PerPx * aircraft.MovementSpeed, aircraft.Facing);
			aircraft.Altitude += Math.Sign(desiredAltitude - aircraft.Altitude);
		}
	}
}
