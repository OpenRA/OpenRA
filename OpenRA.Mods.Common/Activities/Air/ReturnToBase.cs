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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ReturnToBase : Activity
	{
		readonly Aircraft aircraft;
		readonly RepairableInfo repairableInfo;
		readonly Rearmable rearmable;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		bool isCalculated;
		bool resupplied;
		Actor dest;
		WPos w1, w2, w3;

		public ReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			this.dest = dest;
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			aircraft = self.Trait<Aircraft>();
			repairableInfo = self.Info.TraitInfoOrDefault<RepairableInfo>();
			rearmable = self.TraitOrDefault<Rearmable>();
		}

		public static Actor ChooseResupplier(Actor self, bool unreservedOnly)
		{
			var rearmInfo = self.Info.TraitInfoOrDefault<RearmableInfo>();
			if (rearmInfo == null)
				return null;

			return self.World.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead
					&& a.Owner == self.Owner
					&& rearmInfo.RearmActors.Contains(a.Info.Name)
					&& (!unreservedOnly || Reservable.IsAvailableFor(a, self)))
				.ClosestTo(self);
		}

		// Calculates non-CanHover/non-VTOL approach vector and waypoints
		void Calculate(Actor self)
		{
			if (dest == null)
				return;

			var exit = dest.FirstExitOrDefault(null);
			var offset = exit != null ? exit.Info.SpawnOffset : WVec.Zero;

			var landPos = dest.CenterPosition + offset;
			var altitude = aircraft.Info.CruiseAltitude.Length;

			// Distance required for descent.
			var landDistance = altitude * 1024 / aircraft.Info.MaximumPitch.Tan();

			// Land towards the east
			var approachStart = landPos + new WVec(-landDistance, 0, altitude);

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

			w1 = posCenter + tangentOffset;
			w2 = approachCenter + tangentOffset;
			w3 = approachStart;

			isCalculated = true;
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (repairableInfo != null && repairableInfo.RepairActors.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return rearmable != null && rearmable.Info.RearmActors.Contains(dest.Info.Name)
					&& rearmable.RearmableAmmoPools.Any(p => !p.FullAmmo());
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
			// Special case: Don't kill other deploy hotkey activities.
			if (aircraft.ForceLanding)
				return NextActivity;

			// If a Cancel was triggered at this point, it's unlikely that previously queued child activities finished,
			// so 'resupplied' needs to be set to false, else it + abortOnResupply might cause another Cancel
			// that would cancel any other activities that were queued after the first Cancel was triggered.
			// TODO: This is a mess, we need to somehow make the activity cancelling a bit less tricky.
			if (resupplied && IsCanceling)
				resupplied = false;

			if (resupplied && abortOnResupply)
				Cancel(self);

			if (resupplied || IsCanceling || self.IsDead)
				return NextActivity;

			if (dest == null || dest.IsDead || !Reservable.IsAvailableFor(dest, self))
				dest = ReturnToBase.ChooseResupplier(self, true);

			if (!isCalculated)
				Calculate(self);

			if (dest == null)
			{
				var nearestResupplier = ChooseResupplier(self, false);

				if (nearestResupplier != null)
				{
					if (aircraft.Info.CanHover)
					{
						var distanceFromResupplier = (nearestResupplier.CenterPosition - self.CenterPosition).HorizontalLength;
						var distanceLength = aircraft.Info.WaitDistanceFromResupplyBase.Length;

						// If no pad is available, move near one and wait
						if (distanceFromResupplier > distanceLength)
						{
							var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * distanceLength / 1024;
							var target = Target.FromPos(nearestResupplier.CenterPosition + randomPosition);

							QueueChild(self, new HeliFly(self, target, WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green), true);
						}

						return this;
					}
					else
					{
						QueueChild(self,
							new Fly(self, Target.FromActor(nearestResupplier), WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green),
							true);

						QueueChild(self, new FlyCircle(self, aircraft.Info.NumberOfTicksToVerifyAvailableAirport), true);
						return this;
					}
				}
				else if (nearestResupplier == null && aircraft.Info.VTOL && aircraft.Info.LandWhenIdle)
				{
					// Using Queue instead of QueueChild here is intentional, as we want VTOLs with LandWhenIdle to land and stay there in this situation
					Cancel(self);
					if (aircraft.Info.TurnToLand)
						Queue(self, new Turn(self, aircraft.Info.InitialFacing));

					Queue(self, new HeliLand(self, true));
					return NextActivity;
				}
				else
				{
					// Prevent an infinite loop in case we'd return to the activity that called ReturnToBase in the first place. Go idle instead.
					Cancel(self);
					return NextActivity;
				}
			}

			var exit = dest.FirstExitOrDefault(null);
			var offset = exit != null ? exit.Info.SpawnOffset : WVec.Zero;

			if (aircraft.Info.CanHover)
				QueueChild(self, new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)), true);
			else if (aircraft.Info.VTOL)
				QueueChild(self, new Fly(self, Target.FromPos(dest.CenterPosition + offset)), true);
			else
			{
				var turnRadius = Fly.CalculateTurnRadius(aircraft.Info.Speed, aircraft.Info.TurnSpeed);

				QueueChild(self, new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)), true);
				QueueChild(self, new Fly(self, Target.FromPos(w2)), true);

				// Fix a problem when the airplane is sent to resupply near the airport
				QueueChild(self, new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)), true);
			}

			if (ShouldLandAtBuilding(self, dest))
			{
				aircraft.MakeReservation(dest);

				if (aircraft.Info.VTOL)
				{
					if (aircraft.Info.TurnToDock)
						QueueChild(self, new Turn(self, aircraft.Info.InitialFacing), true);

					QueueChild(self, new HeliLand(self, true, dest), true);
				}
				else
					QueueChild(self, new Land(self, Target.FromPos(dest.CenterPosition + offset), true, dest), true);

				QueueChild(self, new Resupply(self, dest, WDist.Zero), true);
				resupplied = true;
			}

			return this;
		}
	}
}
