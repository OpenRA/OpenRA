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
		public readonly PPos Pos;

		Fly(PPos px) { Pos = px; }

		public static Fly ToPx( PPos px ) { return new Fly( px ); }
		public static Fly ToCell(CPos pos) { return new Fly(Util.CenterOfCell(pos)); }

		public override Activity Tick(Actor self)
		{
			var cruiseAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

			if (IsCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var aircraft = self.Trait<Aircraft>();

			var desiredFacing = Util.GetFacing(d, aircraft.Facing);
			if (aircraft.Altitude == cruiseAltitude)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);

			if (aircraft.Altitude < cruiseAltitude)
				++aircraft.Altitude;

			FlyUtil.Fly(self, cruiseAltitude);
			return this;
		}

		public override IEnumerable<Target> GetTargets( Actor self )
		{
			yield return Target.FromPos(Pos);
		}
	}

	public static class FlyUtil
	{
		public static void Fly(Actor self, int desiredAltitude )
		{
			var aircraft = self.Trait<Aircraft>();
			aircraft.TickMove( PSubPos.PerPx * aircraft.MovementSpeed, aircraft.Facing );
			aircraft.Altitude += Math.Sign(desiredAltitude - aircraft.Altitude);
		}
	}
}
