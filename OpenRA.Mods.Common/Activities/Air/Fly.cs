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

		public static void FlyTick(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude, WVec moveOverride, int turnSpeedOverride = -1)
		{
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var move = aircraft.Info.CanHover ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);
			if (moveOverride != WVec.Zero)
				move = moveOverride;

			var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			// Note: we assume that if move.Z is not zero, it's intentional and we want to move in that vertical direction instead of towards desiredAltitude.
			// If that is not desired, the place that calls this should make sure moveOverride.Z is zero.
			if (dat != desiredAltitude || move.Z != 0)
			{
				var maxDelta = move.HorizontalLength * aircraft.Info.MaximumPitch.Tan() / 1024;
				var moveZ = move.Z != 0 ? move.Z : (desiredAltitude.Length - dat.Length);
				var deltaZ = moveZ.Clamp(-maxDelta, maxDelta);
				move = new WVec(move.X, move.Y, deltaZ);
			}

			aircraft.SetPosition(self, aircraft.CenterPosition + move);
		}

		public static void FlyTick(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude, int turnSpeedOverride = -1)
		{
			FlyTick(self, aircraft, desiredFacing, desiredAltitude, WVec.Zero, turnSpeedOverride);
		}

		// Should only be used for vertical-only movement, usually VTOL take-off or land. Terrain-induced altitude changes should always be handled by FlyTick.
		public static bool VerticalTakeOffOrLandTick(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude, int turnSpeedOverride = -1)
		{
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var move = WVec.Zero;

			var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			if (dat != desiredAltitude)
			{
				var maxDelta = aircraft.Info.AltitudeVelocity.Length;
				var deltaZ = (desiredAltitude.Length - dat.Length).Clamp(-maxDelta, maxDelta);
				move += new WVec(0, 0, deltaZ);
			}
			else
				return false;

			aircraft.SetPosition(self, aircraft.CenterPosition + move);
			return true;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			if (IsCanceling)
				return NextActivity;

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			if (dat <= aircraft.LandAltitude)
			{
				QueueChild(self, new TakeOff(self, target), true);
				return this;
			}

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

			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
			var delta = checkTarget.CenterPosition - self.CenterPosition;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;

			// Inside the target annulus, so we're done
			var insideMaxRange = maxRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, maxRange);
			var insideMinRange = minRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, minRange);
			if (insideMaxRange && !insideMinRange)
				return NextActivity;

			var move = aircraft.Info.CanHover ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);

			// Inside the minimum range, so reverse if CanHover
			if (aircraft.Info.CanHover && insideMinRange)
			{
				FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, -move);
				return this;
			}

			// The next move would overshoot, so consider it close enough or set final position if CanHover
			if (delta.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				if (aircraft.Info.CanHover)
				{
					// Set final (horizontal) position
					if (delta.HorizontalLengthSquared != 0)
					{
						// Ensure we don't include a non-zero vertical component here that would move us away from CruiseAltitude
						var deltaMove = new WVec(delta.X, delta.Y, 0);
						FlyTick(self, aircraft, desiredFacing, dat, deltaMove);
					}

					// Move to CruiseAltitude, if not already there
					if (dat != aircraft.Info.CruiseAltitude)
					{
						Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
						return this;
					}
				}

				return NextActivity;
			}

			if (!aircraft.Info.CanHover)
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

			FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);

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
