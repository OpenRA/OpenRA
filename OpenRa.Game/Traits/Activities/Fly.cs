using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Fly : IActivity
	{
		readonly int2 Cell;
		bool isCanceled;

		public Fly(int2 cell) { Cell = cell; }

		public IActivity NextActivity { get; set; }

		const int CruiseAltitude = 20;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			var d = Util.CenterOfCell(Cell) - self.CenterLocation;
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

	class FlyTimed : IActivity
	{
		public IActivity NextActivity { get; set; }
		int remainingTicks;

		public FlyTimed(int ticks) { remainingTicks = ticks; }
		
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
			return new Fly(Cell)
			{
				NextActivity =
					new FlyTimed(50)
					{
						NextActivity = this
					}
			};
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
