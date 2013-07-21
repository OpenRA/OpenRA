#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class ReturnToBase : Activity
	{
		bool isCalculated;
		Actor dest;
		WPos w1, w2, w3;

		public static Actor ChooseAirfield(Actor self, bool unreservedOnly)
		{
			var rearmBuildings = self.Info.Traits.Get<PlaneInfo>().RearmBuildings;
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

			var plane = self.Trait<Plane>();
			var planeInfo = self.Info.Traits.Get<PlaneInfo>();
			var res = dest.TraitOrDefault<Reservable>();
			if (res != null)
			{
				plane.UnReserve();
				plane.Reservation = res.Reserve(dest, self, plane);
			}

			var landPos = dest.CenterPosition;

			// Distance required for descent.
			var landDistance = planeInfo.CruiseAltitude * 1024 * 1024 / (Game.CellSize * plane.Info.MaximumPitch.Tan());
			var altitude = planeInfo.CruiseAltitude * 1024 / Game.CellSize;

			// Land towards the east
			var approachStart = landPos + new WVec(-landDistance, 0, altitude);

			// Add 10% to the turning radius to ensure we have enough room
			var speed = plane.MovementSpeed * 1024 / (Game.CellSize * 5);
			var turnRadius = (int)(141 * speed / planeInfo.ROT / (float)Math.PI);

			// Find the center of the turning circles for clockwise and counterclockwise turns
			var angle = WAngle.FromFacing(plane.Facing);
			var fwd = -new WVec(angle.Sin(), angle.Cos(), 0);

			// Work out whether we should turn clockwise or counter-clockwise for approach
			var side = new WVec(-fwd.Y, fwd.X, fwd.Z);
			var approachDelta = self.CenterPosition - approachStart;
			var sideTowardBase = new[] { side, -side }
				.OrderBy(a => WVec.Dot(a, approachDelta))
				.First();

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
			plane.RTBPathHash = w1 + (WVec)w2 + (WVec)w3;

			isCalculated = true;
		}

		public ReturnToBase(Actor self, Actor dest)
		{
			this.dest = dest;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || self.IsDead())
				return NextActivity;

			if (!isCalculated)
				Calculate(self);

			if (dest == null)
			{
				var nearestAfld = ChooseAirfield(self, false);
				
				self.CancelActivity();
				if (nearestAfld != null)
					return Util.SequenceActivities(Fly.ToCell(nearestAfld.Location), new FlyCircle());
				else
					return new FlyCircle();
			}

			return Util.SequenceActivities(
				Fly.ToPos(w1),
				Fly.ToPos(w2),
				Fly.ToPos(w3),
				new Land(Target.FromActor(dest)),
				NextActivity);
		}
	}
}
