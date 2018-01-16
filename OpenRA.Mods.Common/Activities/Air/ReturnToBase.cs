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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ReturnToBase : Activity
	{
		readonly Aircraft plane;
		readonly AircraftInfo planeInfo;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		bool isCalculated;
		Actor dest;
		WPos w1, w2, w3;

		public ReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			this.dest = dest;
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			plane = self.Trait<Aircraft>();
			planeInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public static Actor ChooseAirfield(Actor self, bool unreservedOnly)
		{
			var rearmBuildings = self.Info.TraitInfo<AircraftInfo>().RearmBuildings;
			return self.World.ActorsHavingTrait<Reservable>()
				.Where(a => a.Owner == self.Owner
					&& rearmBuildings.Contains(a.Info.Name)
					&& (!unreservedOnly || !Reservable.IsReserved(a)))
				.ClosestTo(self);
		}

		void Calculate(Actor self)
		{
			if (dest == null || dest.IsDead || Reservable.IsReserved(dest))
				dest = ChooseAirfield(self, true);

			if (dest == null)
				return;

			var landPos = dest.CenterPosition;
			var altitude = planeInfo.CruiseAltitude.Length;

			// Distance required for descent.
			var landDistance = altitude * 1024 / planeInfo.MaximumPitch.Tan();

			// Land towards the east
			var approachStart = landPos + new WVec(-landDistance, 0, altitude);

			// Add 10% to the turning radius to ensure we have enough room
			var speed = plane.MovementSpeed * 32 / 35;
			var turnRadius = CalculateTurnRadius(speed);

			// Find the center of the turning circles for clockwise and counterclockwise turns
			var angle = WAngle.FromFacing(plane.Facing);
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
			var tangentOffset = new WVec(-tangentDirection.Y, tangentDirection.X, 0) * turnRadius / tangentDirection.Length;

			// TODO: correctly handle CCW <-> CW turns
			if (tangentOffset.X > 0)
				tangentOffset = -tangentOffset;

			w1 = posCenter + tangentOffset;
			w2 = approachCenter + tangentOffset;
			w3 = approachStart;

			isCalculated = true;
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (planeInfo.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return planeInfo.RearmBuildings.Contains(dest.Info.Name) && self.TraitsImplementing<AmmoPool>()
					.Any(p => !p.AutoReloads && !p.FullAmmo());
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			// Special case: Don't kill other deploy hotkey activities.
			if (plane.ForceLanding)
				return NextActivity;

			if (IsCanceled || self.IsDead)
				return NextActivity;

			if (!isCalculated)
				Calculate(self);

			if (dest == null || dest.IsDead)
			{
				var nearestAfld = ChooseAirfield(self, false);

				if (nearestAfld != null)
					return ActivityUtils.SequenceActivities(
						new Fly(self, Target.FromActor(nearestAfld), WDist.Zero, plane.Info.WaitDistanceFromResupplyBase),
						new FlyCircle(self, plane.Info.NumberOfTicksToVerifyAvailableAirport),
						this);
				else
				{
					// Prevent an infinite loop in case we'd return to the activity that called ReturnToBase in the first place. Go idle instead.
					Cancel(self);
					return NextActivity;
				}
			}

			List<Activity> landingProcedures = new List<Activity>();

			var turnRadius = CalculateTurnRadius(planeInfo.Speed);

			landingProcedures.Add(new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)));
			landingProcedures.Add(new Fly(self, Target.FromPos(w2)));

			// Fix a problem when the airplane is send to resupply near the airport
			landingProcedures.Add(new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)));

			if (ShouldLandAtBuilding(self, dest))
			{
				plane.MakeReservation(dest);

				landingProcedures.Add(new Land(self, Target.FromActor(dest)));
				landingProcedures.Add(new ResupplyAircraft(self));
			}

			if (!abortOnResupply)
				landingProcedures.Add(NextActivity);

			return ActivityUtils.SequenceActivities(landingProcedures.ToArray());
		}

		int CalculateTurnRadius(int speed)
		{
			return 45 * speed / planeInfo.TurnSpeed;
		}
	}
}
