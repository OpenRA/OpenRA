#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class HeliFly : Activity
	{
		readonly Helicopter helicopter;
		readonly Target target;
		readonly WRange maxRange;
		readonly WRange minRange;

		public HeliFly(Actor self, Target t)
		{
			helicopter = self.Trait<Helicopter>();
			target = t;
		}

		public HeliFly(Actor self, Target t, WRange minRange, WRange maxRange)
			: this(self, t)
		{
			this.maxRange = maxRange;
			this.minRange = minRange;
		}

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
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			var pos = target.CenterPosition;

			// Rotate towards the target
			var dist = pos - self.CenterPosition;
			var desiredFacing = Util.GetFacing(dist, helicopter.Facing);
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.ROT);
			var move = helicopter.FlyStep(desiredFacing);

			// Inside the minimum range, so reverse
			if (minRange.Range > 0 && target.IsInRange(helicopter.CenterPosition, minRange))
			{
				helicopter.SetPosition(self, helicopter.CenterPosition - move);
				return this;
			}

			// Inside the maximum range, so we're done
			if (maxRange.Range > 0 && target.IsInRange(helicopter.CenterPosition, maxRange))
				return NextActivity;

			// The next move would overshoot, so just set the final position
			if (dist.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				helicopter.SetPosition(self, pos + new WVec(0, 0, helicopter.Info.CruiseAltitude.Range - pos.Z));
				return NextActivity;
			}

			helicopter.SetPosition(self, helicopter.CenterPosition + move);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}
	}
}
