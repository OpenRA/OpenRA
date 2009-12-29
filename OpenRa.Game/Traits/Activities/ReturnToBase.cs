using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class ReturnToBase : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isCanceled;
		bool isCalculated;
		Actor dest;

		float2 w1, w2, w3;	/* tangent points to turn circles */
		float2 landPoint;

		Actor ChooseAirfield(Actor self)
		{
			// todo: handle reservations

			var airfield = Game.world.Actors
				.Where(a => a.Info == Rules.UnitInfo["AFLD"]
					&& a.Owner == self.Owner)
				.FirstOrDefault();

			if (airfield == null)
				throw new NotImplementedException("nowhere to land; what to do?");

			return airfield;
		}

		void Calculate(Actor self)
		{
			if (dest == null) dest = ChooseAirfield(self);

			var landPos = dest.CenterLocation;
			var unit = self.traits.Get<Unit>();
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var approachStart = landPos - new float2(unit.Altitude * speed, 0);
			var turnRadius = (128f / self.Info.ROT) * speed / (float)Math.PI;

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
				new Land(landPoint));
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
