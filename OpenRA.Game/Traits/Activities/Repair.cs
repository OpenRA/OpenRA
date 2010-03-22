#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Traits.Activities
{
	public class Repair : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		int remainingTicks;

		public Repair(bool playHostAnim) {}

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

				var hostBuilding = self.World.FindUnits(self.CenterLocation, self.CenterLocation)
					.FirstOrDefault(a => a.traits.Contains<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.traits.Get<RenderBuilding>()
						.PlayCustomAnim(hostBuilding, "active");

				remainingTicks = (int)(Rules.General.RepairRate * 60 * 25);
			}
			else
				--remainingTicks;

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
