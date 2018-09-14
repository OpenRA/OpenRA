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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Fly : Activity
	{
		readonly Aircraft aircraft;
		readonly Target target;
		readonly WDist maxRange;
		readonly WDist minRange;

		public Fly(Actor self, Target t)
		{
			aircraft = self.Trait<Aircraft>();
			target = t;
		}

		public Fly(Actor self, Target t, WDist minRange, WDist maxRange)
			: this(self, t)
		{
			this.maxRange = maxRange;
			this.minRange = minRange;
		}

		public static void FlyToward(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude)
		{
			desiredAltitude = new WDist(aircraft.CenterPosition.Z) + desiredAltitude - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			var move = aircraft.FlyStep(aircraft.Facing);
			var altitude = aircraft.CenterPosition.Z;

			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			if (altitude != desiredAltitude.Length)
			{
				var delta = move.HorizontalLength * aircraft.Info.MaximumPitch.Tan() / 1024;
				var dz = (desiredAltitude.Length - altitude).Clamp(-delta, delta);
				move += new WVec(0, 0, dz);
			}

			aircraft.SetPosition(self, aircraft.CenterPosition + move);
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			// Inside the target annulus, so we're done
			var insideMaxRange = maxRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, maxRange);
			var insideMinRange = minRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, minRange);
			if (insideMaxRange && !insideMinRange)
				return NextActivity;

			var d = target.CenterPosition - self.CenterPosition;

			// The next move would overshoot, so consider it close enough
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
				return NextActivity;

			// Don't turn until we've reached the cruise altitude
			var desiredFacing = d.Yaw.Facing;
			var targetAltitude = aircraft.CenterPosition.Z + aircraft.Info.CruiseAltitude.Length - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length;
			if (aircraft.CenterPosition.Z < targetAltitude)
				desiredFacing = aircraft.Facing;

			FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}
	}

	public class FlyAndContinueWithCirclesWhenIdle : Fly
	{
		public FlyAndContinueWithCirclesWhenIdle(Actor self, Target t)
			: base(self, t) { }

		public FlyAndContinueWithCirclesWhenIdle(Actor self, Target t, WDist minRange, WDist maxRange)
			: base(self, t, minRange, maxRange) { }

		public override Activity Tick(Actor self)
		{
			var activity = base.Tick(self);

			if (activity == null && !IsCanceled)
			{
				self.QueueActivity(new FlyCircle(self));
				activity = NextActivity;
			}

			return activity;
		}
	}
}
