#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Repair : Activity
	{
		int remainingTicks;
		Actor host;
		Health health;

		public Repair(Actor host) { this.host = host; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (host == null || !host.IsInWorld) return NextActivity;

			health = self.TraitOrDefault<Health>();
			if (health == null) return NextActivity;

			if (health.DamageState == DamageState.Undamaged)
				return NextActivity;

			if (remainingTicks == 0)
			{
				var repairsUnits = host.Info.Traits.Get<RepairsUnitsInfo>();
				var unitCost = self.Info.Traits.Get<ValuedInfo>().Cost;
				var hpToRepair = repairsUnits.HpPerStep;
				var cost = Math.Max(1, (hpToRepair * unitCost * repairsUnits.ValuePercentage) / (health.MaxHP * 100));

				if (!self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(cost))
				{
					remainingTicks = 1;
					return this;
				}

				self.InflictDamage(self, -hpToRepair, null);

				foreach (var depot in host.TraitsImplementing<INotifyRepair>())
					depot.Repairing(self, host);

				remainingTicks = repairsUnits.Interval;
			}
			else
				--remainingTicks;

			return this;
		}
	}
}
