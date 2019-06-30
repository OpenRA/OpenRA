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

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Land : Activity
	{
		readonly Aircraft aircraft;
		readonly WVec offset;
		readonly int desiredFacing;
		readonly bool assignTargetOnFirstRun;
		readonly CPos[] clearCells;
		readonly WDist landRange;

		Target target;
		WPos targetPosition;
		CPos landingCell;
		bool landingInitiated;
		bool finishedApproach;

		public Land(Actor self, int facing = -1)
			: this(self, Target.Invalid, new WDist(-1), WVec.Zero, facing, null)
		{
			assignTargetOnFirstRun = true;
		}

		public Land(Actor self, Target target, int facing = -1)
			: this(self, target, new WDist(-1), WVec.Zero, facing) { }

		public Land(Actor self, Target target, WDist landRange, int facing = -1)
			: this(self, target, landRange, WVec.Zero, facing) { }

		public Land(Actor self, Target target, WVec offset, int facing = -1)
			: this(self, target, WDist.Zero, offset, facing) { }

		public Land(Actor self, Target target, WDist landRange, WVec offset, int facing = -1, CPos[] clearCells = null)
		{
			aircraft = self.Trait<Aircraft>();
			this.target = target;
			this.offset = offset;
			this.clearCells = clearCells ?? new CPos[0];
			this.landRange = landRange.Length >= 0 ? landRange : aircraft.Info.LandRange;

			// NOTE: desiredFacing = -1 means we should not prefer any particular facing and instead just
			// use whatever facing gives us the most direct path to the landing site.
			if (facing == -1 && aircraft.Info.TurnToLand)
				desiredFacing = aircraft.Info.InitialFacing;
			else
				desiredFacing = facing;
		}

		protected override void OnFirstRun(Actor self)
		{
			// When no target is provided we should land in the most direct manner possible.
			// TODO: For fixed-wing aircraft self.Location is not necessarily the most direct landing site.
			if (assignTargetOnFirstRun)
				target = Target.FromCell(self.World, self.Location);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || target.Type == TargetType.Invalid)
			{
				if (landingInitiated)
				{
					// We must return the actor to a sensible height before continuing.
					// If the aircraft lands when idle and is idle, continue landing,
					// otherwise climb back to CruiseAltitude.
					// TODO: Remove this after fixing all activities to work properly with arbitrary starting altitudes.
					var continueLanding = aircraft.Info.LandWhenIdle && self.CurrentActivity.IsCanceling && self.CurrentActivity.NextActivity == null;
					if (!continueLanding)
					{
						var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
						if (dat > aircraft.LandAltitude && dat < aircraft.Info.CruiseAltitude)
						{
							QueueChild(self, new TakeOff(self), true);
							return this;
						}

						aircraft.RemoveInfluence();
						return NextActivity;
					}
				}
				else
					return NextActivity;
			}

			var pos = aircraft.GetPosition();

			// Reevaluate target position in case the target has moved.
			targetPosition = target.CenterPosition + offset;
			landingCell = self.World.Map.CellContaining(targetPosition);

			// We are already at the landing location.
			if ((targetPosition - pos).LengthSquared == 0)
				return NextActivity;

			// Look for free landing cell
			if (target.Type == TargetType.Terrain && !landingInitiated)
			{
				var newLocation = aircraft.FindLandingLocation(landingCell, landRange);

				// Cannot land so fly towards the last target location instead.
				if (!newLocation.HasValue)
				{
					Cancel(self, true);
					QueueChild(self, aircraft.MoveTo(landingCell, 0), true);
					return this;
				}

				if (newLocation.Value != landingCell)
				{
					target = Target.FromCell(self.World, newLocation.Value);
					targetPosition = target.CenterPosition + offset;
					landingCell = self.World.Map.CellContaining(targetPosition);
				}
			}

			// Move towards landing location
			if (aircraft.Info.VTOL && (pos - targetPosition).HorizontalLengthSquared != 0)
			{
				QueueChild(self, new Fly(self, Target.FromPos(targetPosition)), true);

				if (desiredFacing != -1)
					QueueChild(self, new Turn(self, desiredFacing));

				return this;
			}

			if (!aircraft.Info.VTOL && !finishedApproach)
			{
				// Calculate approach trajectory
				var altitude = aircraft.Info.CruiseAltitude.Length;

				// Distance required for descent.
				var landDistance = altitude * 1024 / aircraft.Info.MaximumPitch.Tan();

				// Approach landing from the opposite direction of the desired facing
				// TODO: Calculate sensible trajectory without preferred facing.
				var rotation = WRot.Zero;
				if (desiredFacing != -1)
					rotation = WRot.FromFacing(desiredFacing);

				var approachStart = targetPosition + new WVec(0, landDistance, altitude).Rotate(rotation);

				// Add 10% to the turning radius to ensure we have enough room
				var speed = aircraft.MovementSpeed * 32 / 35;
				var turnRadius = Fly.CalculateTurnRadius(speed, aircraft.Info.TurnSpeed);

				// Find the center of the turning circles for clockwise and counterclockwise turns
				var angle = WAngle.FromFacing(aircraft.Facing);
				var fwd = -new WVec(angle.Sin(), angle.Cos(), 0);

				// Work out whether we should turn clockwise or counter-clockwise for approach
				var side = new WVec(-fwd.Y, fwd.X, fwd.Z);
				var approachDelta = self.CenterPosition - approachStart;
				var sideTowardBase = new[] { side, -side }
					.MinBy(a => WVec.Dot(a, approachDelta));

				// Calculate the tangent line that joins the turning circles at the current and approach positions
				var cp = self.CenterPosition + turnRadius * sideTowardBase / 1024;
				var posCenter = new WPos(cp.X, cp.Y, altitude);
				var approachCenter = approachStart + new WVec(0, turnRadius * Math.Sign(self.CenterPosition.Y - approachStart.Y), 0);
				var tangentDirection = approachCenter - posCenter;
				var tangentLength = tangentDirection.Length;
				var tangentOffset = WVec.Zero;
				if (tangentLength != 0)
					tangentOffset = new WVec(-tangentDirection.Y, tangentDirection.X, 0) * turnRadius / tangentLength;

				// TODO: correctly handle CCW <-> CW turns
				if (tangentOffset.X > 0)
					tangentOffset = -tangentOffset;

				var w1 = posCenter + tangentOffset;
				var w2 = approachCenter + tangentOffset;
				var w3 = approachStart;

				turnRadius = Fly.CalculateTurnRadius(aircraft.Info.Speed, aircraft.Info.TurnSpeed);

				// Move along approach trajectory.
				QueueChild(self, new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)), true);
				QueueChild(self, new Fly(self, Target.FromPos(w2)), true);

				// Fix a problem when the airplane is sent to land near the landing cell
				QueueChild(self, new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)), true);
				finishedApproach = true;
				return this;
			}

			if (!landingInitiated)
			{
				var blockingCells = clearCells.Append(landingCell);

				if (!aircraft.CanLand(blockingCells, target.Actor))
				{
					// Maintain holding pattern.
					if (aircraft.Info.CanHover)
						QueueChild(self, new Wait(25), true);
					else
						QueueChild(self, new FlyCircle(self, 25), true);

					self.NotifyBlocker(blockingCells);
					finishedApproach = false;
					return this;
				}

				if (aircraft.Info.LandingSounds.Length > 0)
					Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds, self.World, aircraft.CenterPosition);

				aircraft.AddInfluence(landingCell);
				aircraft.EnteringCell(self);
				landingInitiated = true;
			}

			// Final descent.
			if (aircraft.Info.VTOL)
			{
				var landAltitude = self.World.Map.DistanceAboveTerrain(targetPosition) + aircraft.LandAltitude;
				if (Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, landAltitude))
					return this;

				return NextActivity;
			}

			var d = targetPosition - pos;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				var landingAltVec = new WVec(WDist.Zero, WDist.Zero, aircraft.LandAltitude);
				aircraft.SetPosition(self, targetPosition + landingAltVec);
				return NextActivity;
			}

			var landingAlt = self.World.Map.DistanceAboveTerrain(targetPosition) + aircraft.LandAltitude;
			Fly.FlyTick(self, aircraft, d.Yaw.Facing, landingAlt);

			return this;
		}
	}
}
