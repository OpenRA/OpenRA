#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Traits.Activities
{
	class ReturnToBase : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isCanceled;
		bool isCalculated;
		Actor dest;

		float2 w1, w2, w3;	/* tangent points to turn circles */
		float2 landPoint;

		public static Actor ChooseAirfield(Actor self)
		{
			return self.World.Queries.OwnedBy[self.Owner]
				.Where(a => a.Info.Name == "afld"
					&& !Reservable.IsReserved(a))
				.FirstOrDefault();
		}

		void Calculate(Actor self)
		{
			if (dest == null) dest = ChooseAirfield(self);
			var res = dest.traits.GetOrDefault<Reservable>();
			if (res != null)
				self.traits.Get<Plane>().reservation = res.Reserve(self);

			var landPos = dest.CenterLocation;
			var unit = self.traits.Get<Unit>();
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var approachStart = landPos - new float2(unit.Altitude * speed, 0);
			var turnRadius = (128f / self.Info.Traits.Get<UnitInfo>().ROT) * speed / (float)Math.PI;

			/* work out the center points */
			var fwd = -float2.FromAngle(unit.Facing / 128f * (float)Math.PI);
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
			landPoint = landPos;

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
				new Land(landPoint),
				NextActivity);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
