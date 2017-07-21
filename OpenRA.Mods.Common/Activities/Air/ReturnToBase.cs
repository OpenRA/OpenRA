#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ReturnToBase : Activity, IDockActivity
	{
		readonly Aircraft plane;
		readonly AircraftInfo planeInfo;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		Actor dest;

		public ReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			this.dest = dest;
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			plane = self.Trait<Aircraft>();
			planeInfo = self.Info.TraitInfo<AircraftInfo>();

			// Release first, before trying to dock.
			var dc = self.TraitOrDefault<DockClient>();
			if (dc != null)
				dc.Release(dc.CurrentDock);
		}

		public static IEnumerable<Actor> GetAirfields(Actor self)
		{
			var rearmBuildings = self.Info.TraitInfo<AircraftInfo>().RearmBuildings;
			return self.World.ActorsHavingTrait<DockManager>()
				.Where(a => a.Owner == self.Owner && rearmBuildings.Contains(a.Info.Name));
		}

		void CalculateLandingPath(Actor self, Dock dock, out WPos w1, out WPos w2, out WPos w3)
		{
			var plane = self.Trait<Aircraft>();
			var planeInfo = self.Info.TraitInfo<AircraftInfo>();
			var landPos = dock.CenterPosition;
			var altitude = planeInfo.CruiseAltitude.Length;

			// Distance required for descent.
			var landDistance = altitude * 1024 / planeInfo.MaximumPitch.Tan();

			// Land towards the east
			var approachStart = landPos + new WVec(-landDistance, 0, altitude);

			// Add 10% to the turning radius to ensure we have enough room
			var speed = plane.MovementSpeed * 32 / 35;
			var turnRadius = CalculateTurnRadius(planeInfo, speed);

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
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (planeInfo.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return planeInfo.RearmBuildings.Contains(dest.Info.Name) && self.TraitsImplementing<AmmoPool>()
					.Any(p => !p.Info.SelfReloads && !p.FullAmmo());
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			// Check status and make dest correct.
			// Priorities:
			// 1. closest reloadable afld
			// 2. closest afld
			// 3. null
			if (dest == null || dest.IsDead || dest.Disposed)
			{
				var aflds = GetAirfields(self);
				var dockableAflds = aflds.Where(p => p.Trait<DockManager>().HasFreeServiceDock(self));
				if (dockableAflds.Any())
					dest = dockableAflds.ClosestTo(self);
				else if (aflds.Any())
					dest = aflds.ClosestTo(self);
				else
					dest = null;
			}

			// Owner doesn't have any feasible afld. In this case,
			{
				// Prevent an infinite loop in case we'd return to the activity that called ReturnToBase in the first place.
				// Go idle instead.
				Cancel(self);
				return NextActivity;
			}

			// Player has an airfield but it is busy. Circle around.
			if (!dest.Trait<DockManager>().HasFreeServiceDock(self))
			{
				Queue(ActivityUtils.SequenceActivities(
					new Fly(self, Target.FromActor(dest), WDist.Zero, plane.Info.WaitDistanceFromResupplyBase),
					new FlyCircleTimed(self, plane.Info.NumberOfTicksToVerifyAvailableAirport),
					new ReturnToBase(self, abortOnResupply, null, alwaysLand)));
				return NextActivity;
			}

			// Now we land. Unlike helis, regardless of ShouldLandAtBuilding, we should land.
			// The difference is, do we just land or do we land and resupply.
			dest.Trait<DockManager>().ReserveDock(dest, self, this);
			return NextActivity;
		}

		public Activity LandingProcedure(Actor self, Dock dock)
		{
			var planeInfo = self.Info.TraitInfo<AircraftInfo>();
			WPos w1, w2, w3;
			CalculateLandingPath(self, dock, out w1, out w2, out w3);

			List<Activity> landingProcedures = new List<Activity>();

			var turnRadius = CalculateTurnRadius(planeInfo, planeInfo.Speed);

			landingProcedures.Add(new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)));
			landingProcedures.Add(new Fly(self, Target.FromPos(w2)));

			// Fix a problem when the airplane is send to resupply near the airport
			landingProcedures.Add(new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)));

			if (ShouldLandAtBuilding(self, dest))
				landingProcedures.Add(new Land(self, Target.FromPos(dock.CenterPosition)));

			/*
			// Causes bugs. Aircrafts should forget what they were doing.
			// if (!abortOnResupply)
			//	landingProcedures.Add(NextActivity);
			*/

			return ActivityUtils.SequenceActivities(landingProcedures.ToArray());
		}

		int CalculateTurnRadius(AircraftInfo planeInfo, int speed)
		{
			return 45 * speed / planeInfo.TurnSpeed;
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
			return LandingProcedure(client, dock);
		}

		Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
		{
			client.SetTargetLine(Target.FromCell(client.World, dock.Location), Color.Green, false);
			return new ResupplyAircraft(client);
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			// I'm ASSUMING rallypoint here.
			var rp = host.Trait<RallyPoint>();

			client.SetTargetLine(Target.FromCell(client.World, rp.Location), Color.Green, false);

			// ResupplyAircraft handles this.
			// Take off and move to RP.
			return ActivityUtils.SequenceActivities(
				new Fly(client, Target.FromCell(client.World, rp.Location)),
				new FlyCircle(client));
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			return new ReturnToBase(client, abortOnResupply);
		}
	}
}
