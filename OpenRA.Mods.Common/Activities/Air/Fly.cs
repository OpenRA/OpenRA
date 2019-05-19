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

		// Use this overload for normal movement vectors
		public static bool FlyTick(Actor self, Aircraft aircraft, int desiredFacing, WDist desiredAltitude,
			int turnSpeedOverride = -1, MovementType types = MovementType.Horizontal | MovementType.Vertical | MovementType.Turn)
		{
			var move = aircraft.Info.CanHover ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);
			return FlyTick(self, aircraft, move, desiredFacing, desiredAltitude, turnSpeedOverride, types);
		}

		static bool FlyTick(Actor self, Aircraft aircraft, WVec move, int desiredFacing, WDist desiredAltitude,
			int turnSpeedOverride = -1, MovementType types = MovementType.Horizontal | MovementType.Vertical | MovementType.Turn)
		{
			var adjustedMove = types.HasFlag(MovementType.Horizontal) ? move : WVec.Zero;
			var hasMoved = adjustedMove != WVec.Zero;

			if (types.HasFlag(MovementType.Turn) && aircraft.Facing != desiredFacing)
			{
				var turnSpeed = turnSpeedOverride > -1 ? turnSpeedOverride : aircraft.TurnSpeed;
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, turnSpeed);
				hasMoved = true;
			}

			if (types.HasFlag(MovementType.Vertical))
			{
				var altitude = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
				if (altitude != desiredAltitude)
				{
					var maxDelta = aircraft.Info.VTOL ? aircraft.Info.AltitudeVelocity.Length : (move.HorizontalLength * aircraft.Info.MaximumPitch.Tan() / 1024);

					// If move contained an explicit Z value, use it instead of the desiredAltitude - altitude delta.
					var deltaZ = move.Z != 0 ? adjustedMove.Z.Clamp(-maxDelta, maxDelta) : (desiredAltitude.Length - altitude.Length).Clamp(-maxDelta, maxDelta);
					adjustedMove = new WVec(adjustedMove.X, adjustedMove.Y, deltaZ);
					hasMoved = true;
				}
			}
			else
				adjustedMove = new WVec(adjustedMove.X, adjustedMove.Y, 0);

			aircraft.SetPosition(self, aircraft.CenterPosition + adjustedMove);
			return hasMoved;
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

			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
			var delta = checkTarget.CenterPosition - self.CenterPosition;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			if (dat == WDist.Zero)
			{
				QueueChild(self, new TakeOff(self, checkTarget), true);
				return this;
			}

			var move = aircraft.Info.CanHover ? aircraft.FlyStep(desiredFacing) : aircraft.FlyStep(aircraft.Facing);

			// Inside the target annulus, so we're done
			var insideMaxRange = maxRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, maxRange);
			var insideMinRange = minRange.Length > 0 && checkTarget.IsInRange(aircraft.CenterPosition, minRange);
			if (insideMaxRange && !insideMinRange)
			{
				if (aircraft.Info.CanHover && dat != aircraft.Info.CruiseAltitude)
				{
					FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, -1, MovementType.Vertical | MovementType.Turn);
					return this;
				}

				return NextActivity;
			}
			else if (insideMinRange && aircraft.Info.CanHover)
			{
				// Inside the minimum range, so reverse if CanHover
				FlyTick(self, aircraft, -move, aircraft.Facing, dat);
				return this;
			}

			// The next move would overshoot, so consider it close enough or set final position if CanHover
			var horizontalDelta = delta.HorizontalLengthSquared;
			if (horizontalDelta == 0 || horizontalDelta < move.HorizontalLengthSquared)
			{
				if (aircraft.Info.CanHover)
				{
					FlyTick(self, aircraft, delta, desiredFacing, aircraft.Info.CruiseAltitude, -1, MovementType.Horizontal | MovementType.Turn);
					dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
					if (dat != aircraft.Info.CruiseAltitude)
						return this;
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
