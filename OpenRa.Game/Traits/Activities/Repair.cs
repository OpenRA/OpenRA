using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	public class Repair : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		bool playHostAnim;
		int remainingTicks;

		public Repair(bool playHostAnim) { this.playHostAnim = playHostAnim; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (remainingTicks == 0)
			{
				var unitCost = self.Info.Traits.Get<BuildableInfo>().Cost;
				var hp = self.Info.Traits.Get<OwnedActorInfo>().HP;

				var costPerHp = (Rules.General.URepairPercent * unitCost) / hp;
				var hpToRepair = Math.Min(Rules.General.URepairStep, hp - self.Health);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.TakeCash(cost))
				{
					remainingTicks = 1;
					return this;
				}

				self.InflictDamage(self, -hpToRepair, Rules.WarheadInfo["Super"]);
				if (self.Health == hp)
					return NextActivity;

				var hostBuilding = Game.world.FindUnits(self.CenterLocation, self.CenterLocation)
					.FirstOrDefault(a => a.traits.Contains<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.traits.Get<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");

				remainingTicks = (int)(Rules.General.RepairRate * 60 * 25);
			}
			else
				--remainingTicks;

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
