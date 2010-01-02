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
		int remainingTicks;

		public Repair() { remainingTicks = ticksPerPoint; }

		readonly int ticksPerPoint = (int)(Rules.General.RepairRate * 60 * 25);
		readonly int hpPerPoint = Rules.General.URepairStep;

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (--remainingTicks <= 0)
			{
				var costPerHp = (Rules.General.URepairPercent * self.Info.Cost) / self.Info.Strength;
				var hpToRepair = Math.Min( hpPerPoint, self.Info.Strength - self.Health );
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.TakeCash(cost))
					return this;

				self.InflictDamage(self, -hpPerPoint, Rules.WarheadInfo["Super"]);
				if (self.Health == self.Info.Strength)
					return NextActivity;

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
