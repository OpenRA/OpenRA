#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class Fly : Activity
	{
		readonly WPos pos;
		readonly Plane plane;

		public Fly(Actor self, Target t)
		{
			plane = self.Trait<Plane>();
			pos = t.CenterPosition;
		}

		public static void FlyToward(Actor self, Plane plane, int desiredFacing, WRange desiredAltitude)
		{
			var move = plane.FlyStep(plane.Facing);
			var altitude = plane.CenterPosition.Z;

			plane.Facing = Util.TickFacing(plane.Facing, desiredFacing, plane.ROT);

			if (altitude != desiredAltitude.Range)
			{
				var delta = move.HorizontalLength * plane.Info.MaximumPitch.Tan() / 1024;
				var dz = (desiredAltitude.Range - altitude).Clamp(-delta, delta);
				move += new WVec(0, 0, dz);
			}

			plane.SetPosition(self, plane.CenterPosition + move);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			// Close enough (ported from old code which checked length against sqrt(50) px)
			var d = pos - self.CenterPosition;
			if (d.HorizontalLengthSquared < 91022)
				return NextActivity;

			var desiredFacing = Util.GetFacing(d, plane.Facing);

			// Don't turn until we've reached the cruise altitude
			if (plane.CenterPosition.Z <  plane.Info.CruiseAltitude.Range)
				desiredFacing = plane.Facing;

			FlyToward(self, plane, desiredFacing, plane.Info.CruiseAltitude);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(pos);
		}
	}
}
