using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
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
			return this;
		}

		public void Cancel(Actor self) { remainingTicks = 0; NextActivity = null; }
	}
}
