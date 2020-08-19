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

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Land : Activity
	{
		readonly Aircraft aircraft;
		readonly WVec offset;
		readonly WAngle? desiredFacing;
		readonly bool assignTargetOnFirstRun;
		readonly CPos[] clearCells;
		readonly WDist landRange;
		readonly Color? targetLineColor;

		Target target;
		WPos targetPosition;
		CPos landingCell;
		bool landingInitiated;
		bool finishedApproach;

		public Land(Actor self, WAngle? facing = null, Color? targetLineColor = null)
			: this(self, Target.Invalid, new WDist(-1), WVec.Zero, facing, null)
		{
			assignTargetOnFirstRun = true;
		}

		public Land(Actor self, in Target target, WAngle? facing = null, Color? targetLineColor = null)
			: this(self, target, new WDist(-1), WVec.Zero, facing, targetLineColor: targetLineColor) { }

		public Land(Actor self, in Target target, WDist landRange, WAngle? facing = null, Color? targetLineColor = null)
			: this(self, target, landRange, WVec.Zero, facing, targetLineColor: targetLineColor) { }

		public Land(Actor self, in Target target, WVec offset, WAngle? facing = null, Color? targetLineColor = null)
			: this(self, target, WDist.Zero, offset, facing, targetLineColor: targetLineColor) { }

		public Land(Actor self, in Target target, WDist landRange, WVec offset, WAngle? facing = null, CPos[] clearCells = null, Color? targetLineColor = null)
		{
			aircraft = self.Trait<Aircraft>();
			this.target = target;
			this.offset = offset;
			this.clearCells = clearCells ?? new CPos[0];
			this.landRange = landRange.Length >= 0 ? landRange : aircraft.Info.LandRange;
			this.targetLineColor = targetLineColor;

			// NOTE: desiredFacing = -1 means we should not prefer any particular facing and instead just
			// use whatever facing gives us the most direct path to the landing site.
			if (!facing.HasValue && aircraft.Info.TurnToLand)
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

		public override bool Tick(Actor self)
		{
			if (IsCanceling || target.Type == TargetType.Invalid)
			{
				if (landingInitiated)
				{
					// We must return the actor to a sensible height before continuing.
					// If the aircraft lands when idle and is idle, continue landing,
					// otherwise climb back to CruiseAltitude.
					// TODO: Remove this after fixing all activities to work properly with arbitrary starting altitudes.
					var shouldLand = aircraft.Info.IdleBehavior == IdleBehaviorType.Land;
					var continueLanding = shouldLand && self.CurrentActivity.IsCanceling && self.CurrentActivity.NextActivity == null;
					if (!continueLanding)
					{
						var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
						if (dat > aircraft.LandAltitude && dat < aircraft.Info.CruiseAltitude)
						{
							QueueChild(new TakeOff(self));
							return false;
						}

						aircraft.RemoveInfluence();
						return true;
					}
				}
				else
					return true;
			}

			var pos = aircraft.GetPosition();

			// Reevaluate target position in case the target has moved.
			targetPosition = target.CenterPosition + offset;
			landingCell = self.World.Map.CellContaining(targetPosition);

			// We are already at the landing location.
			if ((targetPosition - pos).LengthSquared == 0)
				return true;

			// Look for free landing cell
			if (target.Type == TargetType.Terrain && !landingInitiated)
			{
				var newLocation = aircraft.FindLandingLocation(landingCell, landRange);

				// Cannot land so fly towards the last target location instead.
				if (!newLocation.HasValue)
				{
					QueueChild(aircraft.MoveTo(landingCell, 0));
					return true;
				}

				if (newLocation.Value != landingCell)
				{
					target = Target.FromCell(self.World, newLocation.Value);
					targetPosition = target.CenterPosition + offset;
					landingCell = self.World.Map.CellContaining(targetPosition);
				}
			}

			// Move towards landing location/facing
			if (aircraft.Info.VTOL)
			{
				if ((pos - targetPosition).HorizontalLengthSquared != 0)
				{
					QueueChild(new Fly(self, Target.FromPos(targetPosition)));
					return false;
				}

				if (desiredFacing.HasValue && desiredFacing.Value != aircraft.Facing)
				{
					QueueChild(new Turn(self, desiredFacing.Value));
					return false;
				}
			}

			if (!aircraft.Info.VTOL && !finishedApproach)
			{
				// Calculate approach trajectory
				var altitude = aircraft.Info.CruiseAltitude.Length;

				// Distance required for descent.
				var landDistance = altitude * 1024 / aircraft.Info.MaximumPitch.Tan();

				// Approach landing from the opposite direction of the desired facing
				// TODO: Calculate sensible trajectory without preferred facing.
				var rotation = WRot.None;
				if (desiredFacing.HasValue)
					rotation = WRot.FromYaw(desiredFacing.Value);

				var approachStart = targetPosition + new WVec(0, landDistance, altitude).Rotate(rotation);

				// Add 10% to the turning radius to ensure we have enough room
				var speed = aircraft.MovementSpeed * 32 / 35;
				var turnRadius = Fly.CalculateTurnRadius(speed, aircraft.TurnSpeed);

				// Find the center of the turning circles for clockwise and counterclockwise turns
				var angle = aircraft.Facing;
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

				turnRadius = Fly.CalculateTurnRadius(aircraft.Info.Speed, aircraft.TurnSpeed);

				// Move along approach trajectory.
				QueueChild(new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)));
				QueueChild(new Fly(self, Target.FromPos(w2)));

				// Fix a problem when the airplane is sent to land near the landing cell
				QueueChild(new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)));
				finishedApproach = true;
				return false;
			}

			if (!landingInitiated)
			{
				var blockingCells = clearCells.Append(landingCell);

				if (!aircraft.CanLand(blockingCells, target.Actor))
				{
					// Maintain holding pattern.
					QueueChild(new FlyIdle(self, 25));

					self.NotifyBlocker(blockingCells);
					finishedApproach = false;
					return false;
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
					return false;

				return true;
			}

			var d = targetPosition - pos;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				var landingAltVec = new WVec(WDist.Zero, WDist.Zero, aircraft.LandAltitude);
				aircraft.SetPosition(self, targetPosition + landingAltVec);
				return true;
			}

			var landingAlt = self.World.Map.DistanceAboveTerrain(targetPosition) + aircraft.LandAltitude;
			Fly.FlyTick(self, aircraft, d.Yaw, landingAlt);

			return false;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(target, targetLineColor.Value);
		}
	}
}
