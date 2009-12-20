using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Fly : IActivity
	{
		readonly float2 Pos;
		bool isCanceled;

		public Fly(float2 pos) { Pos = pos; }

		public IActivity NextActivity { get; set; }

		const int CruiseAltitude = 20;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude < CruiseAltitude)
				++unit.Altitude;

			var desiredFacing = Util.GetFacing(d, unit.Facing);
			if (unit.Altitude == CruiseAltitude)
				Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.ROT);
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return null;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}

	class Land : IActivity
	{
		readonly float2 Pos;
		bool isCanceled;

		public Land(float2 pos) { Pos = pos; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude > 0)
				--unit.Altitude;

			var desiredFacing = Util.GetFacing(d, unit.Facing);
				Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.ROT);
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return null;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}

	class FlyTimed : IActivity
	{
		public IActivity NextActivity { get; set; }
		int remainingTicks;
		int targetAltitude;

		public FlyTimed(int ticks, int targetAltitude) { remainingTicks = ticks; this.targetAltitude = targetAltitude; }
		
		public IActivity Tick(Actor self)
		{
			if (remainingTicks == 0)
				return NextActivity;

			--remainingTicks;

			var unit = self.traits.Get<Unit>();
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			unit.Altitude += Math.Sign(targetAltitude - unit.Altitude);
			return null;
		}

		public void Cancel(Actor self) { remainingTicks = 0; NextActivity = null; }
	}

	class Circle : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		readonly int2 Cell;

		public Circle(int2 cell) { Cell = cell; }
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			return new Fly(Util.CenterOfCell(Cell))
			{
				NextActivity =
					new FlyTimed(50, 20)
					{
						NextActivity = this
					}
			};
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}

	class ReturnToBase : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		readonly float2 w1, w2, w3;	/* tangent points to turn circles */
		readonly float2 landPoint;

		public ReturnToBase(Actor self, float2 landPos)
		{
			var unit = self.traits.Get<Unit>();
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var approachStart = landPos - new float2(unit.Altitude * speed, 0);
			var turnRadius = (128f / self.Info.ROT) * speed / (float)Math.PI;

			/* work out the center points */
			var fwd = -float2.FromAngle(unit.Facing / 128f * (float)Math.PI);
			var side = new float2(-fwd.Y, fwd.X);		/* rotate */
			var sideTowardBase = new [] { side, -side }
				.OrderBy( a => float2.Dot( a, self.CenterLocation - approachStart ) )
				.First();

			var c1 = self.CenterLocation + turnRadius * sideTowardBase;
			var c2 = approachStart + new float2(0, 
				turnRadius * Math.Sign( self.CenterLocation.Y - approachStart.Y ));		// above or below start point

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

			var rup = self.traits.Get<RenderUnitPlane>();
			rup.wps = new[] { self.CenterLocation, w1, w2, w3, landPoint, c1, c2 };
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			return new Fly(w1)
			{
				NextActivity = new Fly(w2)
				{
					NextActivity = new Fly(w3)
					{
						NextActivity = new Land(landPoint)
					}
				}
			};
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
