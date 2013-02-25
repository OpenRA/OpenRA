#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Air
{
	public class ReturnToBase : Activity
	{
		bool isCalculated;
		Actor dest;

		PPos w1, w2, w3;	/* tangent points to turn circles */

		public static Actor ChooseAirfield(Actor self, bool unreservedOnly)
		{
			var rearmBuildings = self.Info.Traits.Get<PlaneInfo>().RearmBuildings;
			return self.World.ActorsWithTrait<Reservable>()
				.Where(a => a.Actor.Owner == self.Owner)
				.Where(a => rearmBuildings.Contains(a.Actor.Info.Name)
					&& (!unreservedOnly || !Reservable.IsReserved(a.Actor)))
				.Select(a => a.Actor)
				.ClosestTo( self.CenterLocation );
		}

		void Calculate(Actor self)
		{
			if (dest == null || Reservable.IsReserved(dest)) dest = ChooseAirfield(self, true);

			if (dest == null) return;

			var plane = self.Trait<Plane>();
			var res = dest.TraitOrDefault<Reservable>();
			if (res != null)
			{
				plane.UnReserve();
				plane.reservation = res.Reserve(dest, self, plane);
			}

			var landPos = dest.CenterLocation;
			var aircraft = self.Trait<Aircraft>();

			var speed = .2f * aircraft.MovementSpeed;

			/* if the aircraft is on the ground, it will take off to the cruise altitude first before approaching */
			var altitude = aircraft.Altitude;
			if (altitude == 0) altitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

			var approachStart = landPos.ToInt2() - new float2(altitude * speed, 0);
			var turnRadius = (128f / self.Info.Traits.Get<AircraftInfo>().ROT) * speed / (float)Math.PI;

			/* work out the center points */
			var fwd = -float2.FromAngle(aircraft.Facing / 128f * (float)Math.PI);
			var side = new float2(-fwd.Y, fwd.X);		/* rotate */
			var sideTowardBase = new[] { side, -side }
				.OrderBy(a => float2.Dot(a, self.CenterLocation.ToInt2() - approachStart))
				.First();

			var c1 = self.CenterLocation.ToInt2() + turnRadius * sideTowardBase;
			var c2 = approachStart + new float2(0, turnRadius * Math.Sign(self.CenterLocation.Y - approachStart.Y));	// above or below start point

			/* work out tangent points */
			var d = c2 - c1;
			var e = (turnRadius / d.Length) * d;
			var f = new float2(-e.Y, e.X);		/* rotate */

			/* todo: support internal tangents, too! */

			if (f.X > 0) f = -f;

			w1 = (PPos)(c1 + f).ToInt2();
			w2 = (PPos)(c2 + f).ToInt2();
			w3 = (PPos)(approachStart).ToInt2();
			plane.RTBPathHash = (PVecInt)w1 + (PVecInt)w2 + (PVecInt)w3;

			isCalculated = true;
		}

		public ReturnToBase(Actor self, Actor dest)
		{
			this.dest = dest;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
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
				Fly.ToPx(w1),
				Fly.ToPx(w2),
				Fly.ToPx(w3),
				new Land(Target.FromActor(dest)),
				NextActivity);
		}
	}
}
