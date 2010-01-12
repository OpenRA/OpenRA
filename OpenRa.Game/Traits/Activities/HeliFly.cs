using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class HeliFly : IActivity
	{
		const int CruiseAltitude = 20;
		readonly float2 Dest;
		public HeliFly(float2 dest)
		{
			Dest = dest;
		}

		public IActivity NextActivity { get; set; }
		bool isCanceled;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled)
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude != CruiseAltitude)
			{
				unit.Altitude += Math.Sign(CruiseAltitude - unit.Altitude);
				return this;
			}

			var dist = Dest - self.CenterLocation;
			if (float2.WithinEpsilon(float2.Zero, dist, 2))
			{
				self.CenterLocation = Dest;
				self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, 
				self.Info.Traits.Get<HelicopterInfo>().ROT);

			var rawSpeed = .2f * Util.GetEffectiveSpeed(self);
			self.CenterLocation += (rawSpeed / dist.Length) * dist;
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
