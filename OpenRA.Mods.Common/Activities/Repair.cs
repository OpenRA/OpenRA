#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Repair : Activity
	{
		readonly IHealth health;
		readonly RepairsUnits[] allRepairsUnits;
		readonly Target host;
		readonly WDist closeEnough;
		readonly Repairable repairable;

		int remainingTicks;
		bool played = false;

		public Repair(Actor self, Actor host, WDist closeEnough)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			allRepairsUnits = host.TraitsImplementing<RepairsUnits>().ToArray();
			health = self.TraitOrDefault<IHealth>();
			repairable = self.TraitOrDefault<Repairable>();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (host.Actor.IsDead)
				return;

			foreach (var depot in host.Actor.TraitsImplementing<INotifyRepair>())
				depot.BeforeRepair(host.Actor, self);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
			{
				if (remainingTicks-- == 0)
					return NextActivity;

				return this;
			}

			// First active.
			RepairsUnits repairsUnits = null;
			var paused = false;
			foreach (var r in allRepairsUnits)
			{
				if (!r.IsTraitDisabled)
				{
					if (r.IsTraitPaused)
						paused = true;
					else
					{
						repairsUnits = r;
						break;
					}
				}
			}

			if (repairsUnits == null)
				return paused ? this : NextActivity;

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
						exp.GiveExperience(repairsUnits.Info.PlayerExperience);
				}

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", repairsUnits.Info.FinishRepairingNotification, self.Owner.Faction.InternalName);

				return NextActivity;
			}

			if (remainingTicks == 0)
			{
				var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
				var unitCost = valued != null ? valued.Cost : 0;
				var hpToRepair = repairable != null && repairable.Info.HpPerStep > 0 ? repairable.Info.HpPerStep : repairsUnits.Info.HpPerStep;

				// Cast to long to avoid overflow when multiplying by the health
				var cost = Math.Max(1, (int)(((long)hpToRepair * unitCost * repairsUnits.Info.ValuePercentage) / (health.MaxHP * 100L)));

				if (!played)
				{
					played = true;
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", repairsUnits.Info.StartRepairingNotification, self.Owner.Faction.InternalName);
				}

				if (!self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(cost, true))
				{
					remainingTicks = 1;
					return this;
				}

				self.InflictDamage(host.Actor, new Damage(-hpToRepair));

				foreach (var depot in host.Actor.TraitsImplementing<INotifyRepair>())
					depot.RepairTick(host.Actor, self);

				remainingTicks = repairsUnits.Info.Interval;
			}
			else
				--remainingTicks;

			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			if (host.Actor.IsDead)
				return;

			foreach (var depot in host.Actor.TraitsImplementing<INotifyRepair>())
				depot.AfterRepair(host.Actor, self);
		}

		protected override void OnActorDispose(Actor self)
		{
			// If the actor died (or will be disposed directly) this tick, Activity.TickOuter won't be ticked again,
			// so we need to run OnLastRun directly (otherwise it would be skipped completely).
			OnLastRun(self);
		}
	}
}
