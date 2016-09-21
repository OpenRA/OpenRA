#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Repair : Activity
	{
		readonly RepairsUnitsInfo repairsUnits;
		readonly Target host;
		readonly WDist closeEnough;

		int remainingTicks;
		Health health;
		bool played = false;

		public Repair(Actor self, Actor host)
			: this(self, host, WDist.Zero) { }

		public Repair(Actor self, Actor host, WDist closeEnough)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			repairsUnits = host.Info.TraitInfo<RepairsUnitsInfo>();
			health = self.TraitOrDefault<Health>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
			{
				if (remainingTicks-- == 0)
					return NextActivity;

				return this;
			}

			if (host.Type == TargetType.Invalid || health == null)
				return NextActivity;

			if (closeEnough.LengthSquared > 0 && !host.IsInRange(self.CenterPosition, closeEnough))
				return NextActivity;

			if (health.DamageState == DamageState.Undamaged)
			{
				if (host.Actor.Owner != self.Owner)
				{
					var exp = host.Actor.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
					if (exp != null)
						exp.GiveExperience(repairsUnits.PlayerExperience);
				}

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", repairsUnits.FinishRepairingNotification, self.Owner.Faction.InternalName);
				return NextActivity;
			}

			if (remainingTicks == 0)
			{
				var unitCost = self.Info.TraitInfo<ValuedInfo>().Cost;
				var hpToRepair = repairsUnits.HpPerStep;
				var cost = Math.Max(1, (hpToRepair * unitCost * repairsUnits.ValuePercentage) / (health.MaxHP * 100));

				if (!played)
				{
					played = true;
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", repairsUnits.StartRepairingNotification, self.Owner.Faction.InternalName);
				}

				if (!self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(cost, true))
				{
					remainingTicks = 1;
					return this;
				}

				self.InflictDamage(self, new Damage(-hpToRepair));

				foreach (var depot in host.Actor.TraitsImplementing<INotifyRepair>())
					depot.Repairing(host.Actor, self);

				remainingTicks = repairsUnits.Interval;
			}
			else
				--remainingTicks;

			return this;
		}
	}
}
