#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
		bool firstTick = true;
		bool isTakeOff;

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

		public static bool AdjustAltitude(Actor self, Aircraft aircraft, WDist targetAltitude)
		{
			targetAltitude = new WDist(aircraft.CenterPosition.Z) + targetAltitude - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			var altitude = aircraft.CenterPosition.Z;
			if (altitude == targetAltitude.Length)
				return false;

			var delta = aircraft.Info.AltitudeVelocity.Length;
			var dz = (targetAltitude.Length - altitude).Clamp(-delta, delta);
			aircraft.SetPosition(self, aircraft.CenterPosition + new WVec(0, 0, dz));

			return true;
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

			if (firstTick)
			{
				isTakeOff = self.IsAtGroundLevel();
				firstTick = false;

				if (aircraft.Info.TakeoffSound != null && isTakeOff)
				{
					if (!aircraft.Info.VTOL)
						isTakeOff = false;

					Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSound);
				}
			}

			// If it's either a hovering aicraft or a VTOL aircraft taking off, first climb to CruiseAltitude before doing anything else
			if (aircraft.Info.CanHover || (isTakeOff && aircraft.Info.VTOL))
			{
				// If isTakeOff is still true at this point, it must be a VTOL, so set it to false if AdjustAltitude is false
				if (AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
					return this;
				else if (isTakeOff)
					isTakeOff = false;
			}

			var pos = target.CenterPosition;
			var dist = pos - self.CenterPosition;

			var insideMaxRange = maxRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, maxRange);
			var insideMinRange = minRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, minRange);

			var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : aircraft.Facing;
			if (aircraft.Info.CanHover && desiredFacing != aircraft.Facing)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			var move = aircraft.FlyStep(desiredFacing);

			// Inside the target annulus, so we're done
			if (insideMaxRange && !insideMinRange)
				return NextActivity;
			else if (aircraft.Info.CanHover && insideMinRange)
			{
				// Helicopter inside the minimum range, so reverse
				aircraft.SetPosition(self, aircraft.CenterPosition - move);
				return this;
			}

			var targetAltitude = aircraft.CenterPosition.Z + aircraft.Info.CruiseAltitude.Length - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length;

			// The next move would overshoot...
			if (dist.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				// ...so consider it close enough if a plane
				if (!aircraft.Info.CanHover)
					return NextActivity;

				// ...or just set the final position if a helicopter
				aircraft.SetPosition(self, pos + new WVec(0, 0, targetAltitude - pos.Z));
				return NextActivity;
			}

			if (!aircraft.Info.CanHover)
			{
				// Don't turn until we've reached the cruise altitude
				desiredFacing = dist.Yaw.Facing;
				if (aircraft.CenterPosition.Z < targetAltitude)
					desiredFacing = aircraft.Facing;

				FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);
			}
			else
				aircraft.SetPosition(self, aircraft.CenterPosition + move);

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

	public class FlyAndLandWhenIdle : Fly
	{
		private readonly AircraftInfo info;

		public FlyAndLandWhenIdle(Actor self, Target t, AircraftInfo info)
			: base(self, t)
		{
			this.info = info;
		}

		public override Activity Tick(Actor self)
		{
			var activity = base.Tick(self);

			if (activity == null && !IsCanceled && info.LandWhenIdle)
			{
				if (info.TurnToLand)
					self.QueueActivity(new Turn(self, info.InitialFacing));

				self.QueueActivity(new HeliLand(self, true));
				activity = NextActivity;
			}

			return activity;
		}
	}
}
