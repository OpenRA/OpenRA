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
	public class Fly : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist maxRange;
		readonly WDist minRange;
		readonly Color? targetLineColor;
		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		bool soundPlayed;

		public Fly(Actor self, Target t, WPos? initialTargetPosition = null, Color? targetLineColor = null)
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

		public Fly(Actor self, Target t, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: this(self, t, initialTargetPosition, targetLineColor)
		{
			this.maxRange = maxRange;
			this.minRange = minRange;
		}

		public static bool FlyToward(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude, int turnSpeedOverride = -1, bool isVTOL = false)
		{
			desiredAltitude = new WDist(aircraft.CenterPosition.Z) + desiredAltitude - self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			var move = aircraft.FlyStep(aircraft.Facing);
			var altitude = aircraft.CenterPosition.Z;

			var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			if (altitude != desiredAltitude.Length)
			{
				var maxDelta = aircraft.Info.VTOL ? aircraft.Info.AltitudeVelocity.Length : (move.HorizontalLength * aircraft.Info.MaximumPitch.Tan() / 1024);
				var dz = (desiredAltitude.Length - altitude).Clamp(-maxDelta, maxDelta);
				if (isVTOL)
					move = new WVec(0, 0, dz);
				else
					move += new WVec(0, 0, dz);
			}
			else
				if (isVTOL)
					return false;

			aircraft.SetPosition(self, aircraft.CenterPosition + move);
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

			// We are taking off, so remove influence in ground cells.
			if (self.IsAtGroundLevel())
			{
				if (!soundPlayed && aircraft.Info.TakeoffSounds.Length > 0)
				{
					Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds, self.World, aircraft.CenterPosition);
					soundPlayed = true;
				}

				aircraft.RemoveInfluence();
			}

			// If we're a VTOL, rise before flying forward
			if (aircraft.Info.VTOL)
				if (FlyToward(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude, -1, true))
					return this;

			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
			var delta = checkTarget.CenterPosition - self.CenterPosition;
			var desiredFacing = !aircraft.Info.CanHover || delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;

			// Update facing if CanHover
			if (aircraft.Info.CanHover)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			// Inside the target annulus, so we're done
			var insideMaxRange = maxRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, maxRange);
			var insideMinRange = minRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, minRange);
			if (insideMaxRange && !insideMinRange)
				return NextActivity;

			var move = aircraft.Info.CanHover ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);

			// Inside the minimum range, so reverse if CanHover
			if (aircraft.Info.CanHover && insideMinRange)
			{
				aircraft.SetPosition(self, aircraft.CenterPosition - move);
				return this;
			}

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length;
			var targetAltitude = aircraft.CenterPosition.Z + aircraft.Info.CruiseAltitude.Length - dat;

			// The next move would overshoot, so consider it close enough or set final position if CanHover
			if (delta.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				if (aircraft.Info.CanHover)
					aircraft.SetPosition(self, checkTarget.CenterPosition + new WVec(0, 0, targetAltitude - checkTarget.CenterPosition.Z));

				return NextActivity;
			}

			if (aircraft.Info.CanHover)
			{
				aircraft.SetPosition(self, aircraft.CenterPosition + move);
				return this;
			}

			// Don't turn until we've reached the cruise altitude
			if (aircraft.CenterPosition.Z < targetAltitude)
				desiredFacing = aircraft.Facing;
			else
			{
				// Using the turn rate, compute a hypothetical circle traced by a continuous turn.
				// If it contains the destination point, it's unreachable without more complex manuvering.
				var turnRadius = CalculateTurnRadius(aircraft.MovementSpeed, aircraft.TurnSpeed);

				// The current facing is a tangent of the minimal turn circle.
				// Make a perpendicular vector, and use it to locate the turn's center.
				var turnCenterFacing = aircraft.Facing;
				turnCenterFacing += Util.GetNearestFacing(aircraft.Facing, desiredFacing) > 0 ? 64 : -64;

				var turnCenterDir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(turnCenterFacing));
				turnCenterDir *= turnRadius;
				turnCenterDir /= 1024;

				// Compare with the target point, and keep flying away if it's inside the circle.
				var turnCenter = aircraft.CenterPosition + turnCenterDir;
				if ((checkTarget.CenterPosition - turnCenter).HorizontalLengthSquared < turnRadius * turnRadius)
					desiredFacing = aircraft.Facing;
			}

			FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}

		public static int CalculateTurnRadius(int speed, int turnSpeed)
		{
			// turnSpeed -> divide into 256 to get the number of ticks per complete rotation
			// speed -> multiply to get distance travelled per rotation (circumference)
			// 45 -> divide by 2*pi to get the turn radius: 45==256/(2*pi), with some extra leeway
			return 45 * speed / turnSpeed;
		}
	}
}
