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
				Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.Traits.Get<UnitInfo>().ROT);
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
