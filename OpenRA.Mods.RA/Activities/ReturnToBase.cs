#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class ReturnToBase : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isCanceled;
		bool isCalculated;
		Actor dest;

		float2 w1, w2, w3;	/* tangent points to turn circles */

		public static Actor ChooseAirfield(Actor self)
		{
			return self.World.Queries.OwnedBy[self.Owner]
				.Where(a => self.Info.Traits.Get<PlaneInfo>().RearmBuildings.Contains(a.Info.Name)
					&& !Reservable.IsReserved(a))
				.OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		void Calculate(Actor self)
		{
			if (dest == null)
			{
				dest = ChooseAirfield(self);
			}

			var res = dest.traits.GetOrDefault<Reservable>();
			if (res != null)
			{
				var plane = self.traits.Get<Plane>();
				plane.UnReserve();
				plane.reservation = res.Reserve(self);
			}

			var landPos = dest.CenterLocation;
			var aircraft = self.traits.Get<Aircraft>();

			var speed = .2f * aircraft.MovementSpeedForCell(self, self.Location);
			
			var approachStart = landPos - new float2(aircraft.Altitude * speed, 0);
			var turnRadius = (128f / self.Info.Traits.Get<AircraftInfo>().ROT) * speed / (float)Math.PI;

			/* work out the center points */
			var fwd = -float2.FromAngle(aircraft.Facing / 128f * (float)Math.PI);
			var side = new float2(-fwd.Y, fwd.X);		/* rotate */
			var sideTowardBase = new[] { side, -side }
				.OrderBy(a => float2.Dot(a, self.CenterLocation - approachStart))
				.First();

			var c1 = self.CenterLocation + turnRadius * sideTowardBase;
			var c2 = approachStart + new float2(0,
				turnRadius * Math.Sign(self.CenterLocation.Y - approachStart.Y));		// above or below start point

			/* work out tangent points */
			var d = c2 - c1;
			var e = (turnRadius / d.Length) * d;
			var f = new float2(-e.Y, e.X);		/* rotate */

			/* todo: support internal tangents, too! */

			if (f.X > 0) f = -f;

			w1 = c1 + f;
			w2 = c2 + f;
			w3 = approachStart;

			isCalculated = true;
		}

		public ReturnToBase(Actor self, Actor dest)
		{
			this.dest = dest;
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (!isCalculated)
				Calculate(self);

			return Util.SequenceActivities(
				new Fly(w1),
				new Fly(w2),
				new Fly(w3),
				new Land(Target.FromActor(dest)),
				NextActivity);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
