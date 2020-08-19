#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
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
		readonly WDist nearEnough;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		readonly List<WPos> positionBuffer = new List<WPos>();

		public Fly(Actor self, in Target t, WDist nearEnough, WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: this(self, t, initialTargetPosition, targetLineColor)
		{
			this.nearEnough = nearEnough;
		}

		public Fly(Actor self, in Target t, WPos? initialTargetPosition = null, Color? targetLineColor = null)
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

		public Fly(Actor self, in Target t, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: this(self, t, initialTargetPosition, targetLineColor)
		{
			this.maxRange = maxRange;
			this.minRange = minRange;
		}

		public static void FlyTick(Actor self, Aircraft aircraft, WAngle desiredFacing, WDist desiredAltitude, WVec moveOverride, bool idleTurn = false)
		{
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var move = aircraft.Info.CanSlide ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);
			if (moveOverride != WVec.Zero)
				move = moveOverride;

			var oldFacing = aircraft.Facing;
			var turnSpeed = idleTurn ? aircraft.IdleTurnSpeed ?? aircraft.TurnSpeed : aircraft.TurnSpeed;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);

			var roll = idleTurn ? aircraft.Info.IdleRoll ?? aircraft.Info.Roll : aircraft.Info.Roll;
			if (roll != WAngle.Zero)
			{
				var desiredRoll = aircraft.Facing == desiredFacing ? WAngle.Zero :
					new WAngle(roll.Angle * Util.GetTurnDirection(aircraft.Facing, oldFacing));

				aircraft.Roll = Util.TickFacing(aircraft.Roll, desiredRoll, aircraft.Info.RollSpeed);
			}

			if (aircraft.Info.Pitch != WAngle.Zero)
				aircraft.Pitch = Util.TickFacing(aircraft.Pitch, aircraft.Info.Pitch, aircraft.Info.PitchSpeed);

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

		public static void FlyTick(Actor self, Aircraft aircraft, WAngle desiredFacing, WDist desiredAltitude, bool idleTurn = false)
		{
			FlyTick(self, aircraft, desiredFacing, desiredAltitude, WVec.Zero, idleTurn);
		}

		// Should only be used for vertical-only movement, usually VTOL take-off or land. Terrain-induced altitude changes should always be handled by FlyTick.
		public static bool VerticalTakeOffOrLandTick(Actor self, Aircraft aircraft, WAngle desiredFacing, WDist desiredAltitude, bool idleTurn = false)
		{
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var move = WVec.Zero;

			var turnSpeed = idleTurn ? aircraft.IdleTurnSpeed ?? aircraft.TurnSpeed : aircraft.TurnSpeed;
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

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var isLanded = dat <= aircraft.LandAltitude;

			// HACK: Prevent paused (for example, EMP'd) aircraft from taking off.
			// This is necessary until the TODOs in the IsCanceling block below are adressed.
			if (isLanded && aircraft.IsTraitPaused)
				return false;

			if (IsCanceling)
			{
				// We must return the actor to a sensible height before continuing.
				// If the aircraft is on the ground we queue TakeOff to manage the influence reservation and takeoff sounds etc.
				// TODO: It would be better to not take off at all, but we lack the plumbing to detect current airborne/landed state.
				// If the aircraft lands when idle and is idle, we let the default idle handler manage this.
				// TODO: Remove this after fixing all activities to work properly with arbitrary starting altitudes.
				var landWhenIdle = aircraft.Info.IdleBehavior == IdleBehaviorType.Land;
				var skipHeightAdjustment = landWhenIdle && self.CurrentActivity.IsCanceling && self.CurrentActivity.NextActivity == null;
				if (aircraft.Info.CanHover && !skipHeightAdjustment && dat != aircraft.Info.CruiseAltitude)
				{
					if (isLanded)
						QueueChild(new TakeOff(self));
					else
						VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);

					return false;
				}

				return true;
			}
			else if (isLanded)
			{
				QueueChild(new TakeOff(self));
				return false;
			}

			target = target.Recalculate(self.Owner, out var targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				lastVisibleTarget = Target.FromTargetPositions(target);

			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return true;

			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
			var pos = aircraft.GetPosition();
			var delta = checkTarget.CenterPosition - pos;

			// Inside the target annulus, so we're done
			var insideMaxRange = maxRange.Length > 0 && checkTarget.IsInRange(pos, maxRange);
			var insideMinRange = minRange.Length > 0 && checkTarget.IsInRange(pos, minRange);
			if (insideMaxRange && !insideMinRange)
				return true;

			var isSlider = aircraft.Info.CanSlide;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw : aircraft.Facing;
			var move = isSlider ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);

			// Inside the minimum range, so reverse if we CanSlide, otherwise face away from the target.
			if (insideMinRange)
			{
				if (isSlider)
					FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, -move);
				else
				{
					FlyTick(self, aircraft, desiredFacing + new WAngle(512), aircraft.Info.CruiseAltitude, move);
				}

				return false;
			}

			// HACK: Consider ourselves blocked if we have moved by less than 64 WDist in the last five ticks
			// Stop if we are blocked and close enough
			if (positionBuffer.Count >= 5 && (positionBuffer.Last() - positionBuffer[0]).LengthSquared < 4096 &&
				delta.HorizontalLengthSquared <= nearEnough.LengthSquared)
				return true;

			// The next move would overshoot, so consider it close enough or set final position if we CanSlide
			if (delta.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				// For VTOL landing to succeed, it must reach the exact target position,
				// so for the final move it needs to behave as if it had CanSlide.
				if (isSlider || aircraft.Info.VTOL)
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
						return false;
					}
				}

				return true;
			}

			if (!isSlider)
			{
				// Using the turn rate, compute a hypothetical circle traced by a continuous turn.
				// If it contains the destination point, it's unreachable without more complex manuvering.
				var turnRadius = CalculateTurnRadius(aircraft.MovementSpeed, aircraft.TurnSpeed);

				// The current facing is a tangent of the minimal turn circle.
				// Make a perpendicular vector, and use it to locate the turn's center.
				var turnCenterFacing = aircraft.Facing + new WAngle(Util.GetTurnDirection(aircraft.Facing, desiredFacing) * 256);

				var turnCenterDir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(turnCenterFacing));
				turnCenterDir *= turnRadius;
				turnCenterDir /= 1024;

				// Compare with the target point, and keep flying away if it's inside the circle.
				var turnCenter = aircraft.CenterPosition + turnCenterDir;
				if ((checkTarget.CenterPosition - turnCenter).HorizontalLengthSquared < turnRadius * turnRadius)
					desiredFacing = aircraft.Facing;
			}

			positionBuffer.Add(self.CenterPosition);
			if (positionBuffer.Count > 5)
				positionBuffer.RemoveAt(0);

			FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor.HasValue)
				yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
		}

		public static int CalculateTurnRadius(int speed, WAngle turnSpeed)
		{
			// turnSpeed -> divide into 256 to get the number of ticks per complete rotation
			// speed -> multiply to get distance travelled per rotation (circumference)
			// 180 -> divide by 2*pi to get the turn radius: 180==1024/(2*pi), with some extra leeway
			return turnSpeed.Angle > 0 ? 180 * speed / turnSpeed.Angle : 0;
		}
	}
}
