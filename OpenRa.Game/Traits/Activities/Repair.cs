using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Repair : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		int remainingTicks = ticksPerPoint;

		const int ticksPerPoint = 15;
		const int hpPerPoint = 8;

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (--remainingTicks == 0)
			{
				self.Health += hpPerPoint;
				if (self.Health >= self.Info.Strength)
				{
					self.Health = self.Info.Strength;
					return NextActivity;
				}

				var hostBuilding = Game.FindUnits(self.CenterLocation, self.CenterLocation)
					.FirstOrDefault(a => a.traits.Contains<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.traits.Get<RenderBuilding>().PlayCustomAnim(hostBuilding, "active" );

				remainingTicks = ticksPerPoint;
			}

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
