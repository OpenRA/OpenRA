using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	public class Land : IActivity
	{
		readonly float2 Pos;
		bool isCanceled;
		Actor Structure;
		
		public Land(float2 pos) { Pos = pos; }
		public Land(Actor structure) { Structure = structure; Pos = Structure.CenterLocation; }
		
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (Structure != null && Structure.IsDead)
			{
				Structure = null;
				isCanceled = true;
			}
			
			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude > 0)
				--unit.Altitude;

			var desiredFacing = Util.GetFacing(d, unit.Facing);
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
