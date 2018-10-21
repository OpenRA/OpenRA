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
		bool soundPlayed;

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

		public static bool FlyToward(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude, int turnSpeedOverride = -1,
			bool moveVerticalOnly = false, bool flyBackward = false, bool isFlyCircle = false)
		{
			desiredAltitude = new WDist(aircraft.CenterPosition.Z) + desiredAltitude - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			var altitude = aircraft.CenterPosition.Z;
			if (moveVerticalOnly && altitude == desiredAltitude.Length)
				return false;

			var move = WVec.Zero;
			if (!moveVerticalOnly)
			{
				// If FlyToward is called from FlyCircle, the aircraft needs to use aircraft.Facing even if it CanHover,
				// otherwise it will circle sideways.
				var direction = flyBackward ? -1 : 1;
				if (aircraft.Info.CanHover && !isFlyCircle)
					move = aircraft.FlyStep(direction * desiredFacing);
				else
					move = aircraft.FlyStep(direction * aircraft.Facing);
			}

			var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			if (altitude != desiredAltitude.Length)
			{
				var delta = moveVerticalOnly ? aircraft.Info.AltitudeVelocity.Length : move.HorizontalLength * aircraft.Info.MaximumPitch.Tan() / 1024;
				var dz = (desiredAltitude.Length - altitude).Clamp(-delta, delta);
				move += new WVec(0, 0, dz);
			}

			aircraft.SetPosition(self, aircraft.CenterPosition + move);

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

			if (!soundPlayed && aircraft.Info.TakeoffSounds.Length > 0 && self.IsAtGroundLevel())
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds.Random(self.World.SharedRandom), aircraft.CenterPosition);
				soundPlayed = true;
			}

			if (aircraft.Info.VTOL)
				if (FlyToward(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude, moveVerticalOnly: true))
					return this;

			if (aircraft.Info.CanHover)
			{
				var pos = target.CenterPosition;

				// Rotate towards the target
				var dist = pos - self.CenterPosition;
				var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : aircraft.Facing;
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);
				var move = aircraft.FlyStep(desiredFacing);

				// Inside the minimum range, so reverse
				if (minRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, minRange))
				{
					aircraft.SetPosition(self, aircraft.CenterPosition - move);
					return this;
				}

				// Inside the maximum range, so we're done
				if (maxRange.Length > 0 && target.IsInRange(aircraft.CenterPosition, maxRange))
					return NextActivity;

				// The next move would overshoot, so just set the final position
				if (dist.HorizontalLengthSquared < move.HorizontalLengthSquared)
				{
					var targetAltitude = aircraft.CenterPosition.Z + aircraft.Info.CruiseAltitude.Length - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length;
					aircraft.SetPosition(self, pos + new WVec(0, 0, targetAltitude - pos.Z));
					return NextActivity;
				}

				aircraft.SetPosition(self, aircraft.CenterPosition + move);
			}
			else
			{
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
			}

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}
	}
}
