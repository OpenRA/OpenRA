#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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
		bool isCalculated;
		Actor dest;
		WPos w1, w2, w3;

		public ReturnToBase(Actor self, Actor dest = null)
		{
			this.dest = dest;
			plane = self.Trait<Aircraft>();
			planeInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public static Actor ChooseAirfield(Actor self, bool unreservedOnly)
		{
			var rearmBuildings = self.Info.TraitInfo<AircraftInfo>().RearmBuildings;
			return self.World.ActorsWithTrait<Reservable>()
				.Where(a => a.Actor.Owner == self.Owner)
				.Where(a => rearmBuildings.Contains(a.Actor.Info.Name)
					&& (!unreservedOnly || !Reservable.IsReserved(a.Actor)))
				.Select(a => a.Actor)
				.ClosestTo(self);
		}

		void Calculate(Actor self)
		{
			if (dest == null || Reservable.IsReserved(dest))
				dest = ChooseAirfield(self, true);

			if (dest == null)
				return;

			var res = dest.TraitOrDefault<Reservable>();
			if (res != null)
			{
				plane.UnReserve();
				plane.Reservation = res.Reserve(dest, self, plane);
			}

			var landPos = dest.CenterPosition;
			var altitude = planeInfo.CruiseAltitude.Length;

			// Distance required for descent.
			var landDistance = altitude * 1024 / planeInfo.MaximumPitch.Tan();

			// Land towards the east
			var approachStart = landPos + new WVec(-landDistance, 0, altitude);

			// Add 10% to the turning radius to ensure we have enough room
			var speed = plane.MovementSpeed * 32 / 35;
			var turnRadius = (int)(141 * speed / planeInfo.ROT / (float)Math.PI);

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

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || self.IsDead)
				return NextActivity;

			if (!isCalculated)
				Calculate(self);

			if (dest == null)
			{
				var nearestAfld = ChooseAirfield(self, false);

				self.CancelActivity();
				if (nearestAfld != null)
					return Util.SequenceActivities(new Fly(self, Target.FromActor(nearestAfld)), new FlyCircle(self));
				else
					return new FlyCircle(self);
			}

			return Util.SequenceActivities(
				new Fly(self, Target.FromPos(w1)),
				new Fly(self, Target.FromPos(w2)),
				new Fly(self, Target.FromPos(w3)),
				new Land(self, Target.FromActor(dest)),
				NextActivity);
		}
	}
}
