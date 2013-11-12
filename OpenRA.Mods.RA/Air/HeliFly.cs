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
	class HeliFly : Activity
	{
		readonly WPos pos;

		public HeliFly(WPos pos) { this.pos = pos; }
		public HeliFly(CPos pos) { this.pos = pos.CenterPosition; }

		public static bool AdjustAltitude(Actor self, Helicopter helicopter, WRange targetAltitude)
		{
			var altitude = helicopter.CenterPosition.Z;
			if (altitude == targetAltitude.Range)
				return false;

			var delta = helicopter.Info.AltitudeVelocity.Range;
			var dz = (targetAltitude.Range - altitude).Clamp(-delta, delta);
			helicopter.SetPosition(self, helicopter.CenterPosition + new WVec(0, 0, dz));

			return true;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var helicopter = self.Trait<Helicopter>();

			var cruiseAltitude = new WRange(helicopter.Info.CruiseAltitude * 1024 / Game.CellSize);
			if (AdjustAltitude(self, helicopter, cruiseAltitude))
				return this;

			// Rotate towards the target
			var dist = pos - self.CenterPosition;
			var desiredFacing = Util.GetFacing(dist, helicopter.Facing);
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.ROT);

			// The next move would overshoot, so just set the final position
			var move = helicopter.FlyStep(desiredFacing);
			if (dist.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				helicopter.SetPosition(self, pos + new WVec(0, 0, cruiseAltitude.Range - pos.Z));
				return NextActivity;
			}

			helicopter.SetPosition(self, helicopter.CenterPosition + move);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(pos);
		}
	}
}
