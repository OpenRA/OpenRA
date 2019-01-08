#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class HeliFly : Activity
	{
		readonly Aircraft aircraft;
		readonly Target target;
		readonly WDist maxRange;
		readonly WDist minRange;
		bool soundPlayed;

		public HeliFly(Actor self, Target t)
		{
			aircraft = self.Trait<Aircraft>();
			target = t;
		}

		public HeliFly(Actor self, Target t, WDist minRange, WDist maxRange)
			: this(self, t)
		{
			this.maxRange = maxRange;
			this.minRange = minRange;
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

			if (!soundPlayed && aircraft.Info.TakeoffSounds.Length > 0 && self.IsAtGroundLevel())
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds.Random(self.World.SharedRandom), aircraft.CenterPosition);
				soundPlayed = true;
			}

			if (AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

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

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}
	}

	public class HeliFlyAndLandWhenIdle : HeliFly
	{
		private readonly AircraftInfo info;

		public HeliFlyAndLandWhenIdle(Actor self, Target t, AircraftInfo info)
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
