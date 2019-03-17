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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliFly : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist maxRange;
		readonly WDist minRange;
		readonly Color? targetLineColor;
		bool soundPlayed;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;

		public HeliFly(Actor self, Target t, WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			aircraft = self.Trait<Aircraft>();
			target = t;
			this.targetLineColor = targetLineColor;

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
			else if (initialTargetPosition.HasValue)
				lastVisibleTarget = Target.FromPos(initialTargetPosition.Value);
		}

		public HeliFly(Actor self, Target t, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: this(self, t, initialTargetPosition, targetLineColor)
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
				Cancel(self);

			if (IsCanceling)
				return NextActivity;

			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				lastVisibleTarget = Target.FromTargetPositions(target);

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget && targetLineColor.HasValue)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return NextActivity;

			if (!soundPlayed && aircraft.Info.TakeoffSounds.Length > 0 && self.IsAtGroundLevel())
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds, self.World, aircraft.CenterPosition);
				soundPlayed = true;
			}

			// We are taking off, so remove influence in ground cells.
			if (self.IsAtGroundLevel())
				aircraft.RemoveInfluence();

			if (AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// Update facing
			var delta = checkTarget.CenterPosition - aircraft.CenterPosition;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);
			if (AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

			var move = aircraft.FlyStep(desiredFacing);

			// Inside the minimum range, so reverse
			if (minRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, minRange))
			{
				aircraft.SetPosition(self, aircraft.CenterPosition - move);
				return this;
			}

			// Inside the maximum range, so we're done
			if (maxRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, maxRange))
				return NextActivity;

			// The next move would overshoot, so just set the final position
			if (delta.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				var targetAltitude = aircraft.CenterPosition.Z + aircraft.Info.CruiseAltitude.Length - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length;
				aircraft.SetPosition(self, checkTarget.CenterPosition + new WVec(0, 0, targetAltitude - checkTarget.CenterPosition.Z));
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

			if (activity == null && !IsCanceling && info.LandWhenIdle)
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
