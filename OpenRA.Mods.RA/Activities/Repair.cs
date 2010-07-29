#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Repair : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		int remainingTicks;
		Actor host;

		public Repair(Actor host) { this.host = host; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (remainingTicks == 0)
			{
				var health = self.traits.GetOrDefault<Health>();
				if (health == null) return NextActivity;
				
				var unitCost = self.Info.Traits.Get<ValuedInfo>().Cost;

				var costPerHp = (host.Info.Traits.Get<RepairsUnitsInfo>().URepairPercent * unitCost) / health.MaxHP;
				var hpToRepair = Math.Min(host.Info.Traits.Get<RepairsUnitsInfo>().URepairStep, health.MaxHP - health.HP);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.PlayerActor.traits.Get<PlayerResources>().TakeCash(cost))
				{
					remainingTicks = 1;
					return this;
				}

				self.InflictDamage(self, -hpToRepair, null);
				if (health.MaxHP == health.HP)
					return NextActivity;

				if (host != null)
					host.traits.Get<RenderBuilding>()
						.PlayCustomAnim(host, "active");

				remainingTicks = (int)(self.World.Defaults.RepairRate * 60 * 25);
			}
			else
				--remainingTicks;

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
