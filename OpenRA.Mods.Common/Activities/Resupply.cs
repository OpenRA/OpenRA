#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class Resupply : Activity
	{
		readonly IHealth health;
		readonly RepairsUnits[] allRepairsUnits;
		readonly Target host;
		readonly WDist closeEnough;
		readonly Repairable repairable;
		readonly RepairableNear repairableNear;
		readonly Rearmable rearmable;
		readonly INotifyResupply[] notifyResupplies;

		int remainingTicks;
		bool played;
		ResupplyType activeResupplyTypes = ResupplyType.None;

		public Resupply(Actor self, Actor host, WDist closeEnough)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			allRepairsUnits = host.TraitsImplementing<RepairsUnits>().ToArray();
			health = self.TraitOrDefault<IHealth>();
			repairable = self.TraitOrDefault<Repairable>();
			repairableNear = self.TraitOrDefault<RepairableNear>();
			rearmable = self.TraitOrDefault<Rearmable>();
			notifyResupplies = host.TraitsImplementing<INotifyResupply>().ToArray();

			var cannotRepairAtHost = health == null || health.DamageState == DamageState.Undamaged
				|| !allRepairsUnits.Any()
				|| ((repairable == null || !repairable.Info.RepairActors.Contains(host.Info.Name))
					&& (repairableNear == null || !repairableNear.Info.RepairActors.Contains(host.Info.Name)));

			if (!cannotRepairAtHost)
				activeResupplyTypes |= ResupplyType.Repair;

			var cannotRearmAtHost = rearmable == null || !rearmable.Info.RearmActors.Contains(host.Info.Name) || rearmable.RearmableAmmoPools.All(p => p.FullAmmo());
			if (!cannotRearmAtHost)
				activeResupplyTypes |= ResupplyType.Rearm;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (host.Type == TargetType.Invalid)
				return;

			if (activeResupplyTypes > 0)
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.BeforeResupply(host.Actor, self, activeResupplyTypes);

			// Reset the ReloadDelay to avoid any issues with early cancellation
			// from previous reload attempts (explicit order, host building died, etc).
			// HACK: this really shouldn't be managed from here
			if (activeResupplyTypes.HasFlag(ResupplyType.Rearm))
				foreach (var pool in rearmable.RearmableAmmoPools)
					pool.RemainingTicks = pool.Info.ReloadDelay;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			// HACK: If the activity is cancelled while we're already resupplying (or about to start resupplying),
			// move actor outside the resupplier footprint
			// TODO: This check is nowhere near robust enough, and should be rewritten
			if (IsCanceling && host.IsInRange(self.CenterPosition, closeEnough))
			{
				QueueChild(self, self.Trait<IMove>().MoveToTarget(self, host), true);
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.None);

				return this;
			}

			if (IsCanceling || host.Type == TargetType.Invalid
				|| (closeEnough.LengthSquared > 0 && !host.IsInRange(self.CenterPosition, closeEnough)))
			{
				// This is necessary to ensure host resupply actions (like animations) finish properly
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.None);

				return NextActivity;
			}

			if (activeResupplyTypes.HasFlag(ResupplyType.Repair))
				RepairTick(self);

			if (activeResupplyTypes.HasFlag(ResupplyType.Rearm))
				RearmTick(self);

			foreach (var notifyResupply in notifyResupplies)
				notifyResupply.ResupplyTick(host.Actor, self, activeResupplyTypes);

			if (activeResupplyTypes == 0)
			{
				var aircraft = self.TraitOrDefault<Aircraft>();
				if (aircraft != null)
				{
					aircraft.AllowYieldingReservation();
					if (aircraft.Info.TakeOffOnResupply)
						Queue(self, new TakeOff(self, (a, b, c) => NextActivity == null && b.NextActivity == null));
				}

				return NextActivity;
			}

			return this;
		}

		void RepairTick(Actor self)
		{
			// First active.
			var repairsUnits = allRepairsUnits.FirstOrDefault(r => !r.IsTraitDisabled && !r.IsTraitPaused);
			if (repairsUnits == null)
			{
				if (!allRepairsUnits.Any(r => r.IsTraitPaused))
					activeResupplyTypes &= ~ResupplyType.Repair;

				return;
			}

			if (health.DamageState == DamageState.Undamaged)
			{
				if (host.Actor.Owner != self.Owner)
				{
					var exp = host.Actor.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
					if (exp != null)
						exp.GiveExperience(repairsUnits.Info.PlayerExperience);
				}

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", repairsUnits.Info.FinishRepairingNotification, self.Owner.Faction.InternalName);

				activeResupplyTypes &= ~ResupplyType.Repair;
				return;
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
					return;
				}

				self.InflictDamage(host.Actor, new Damage(-hpToRepair));
				remainingTicks = repairsUnits.Info.Interval;
			}
			else
				--remainingTicks;
		}

		void RearmTick(Actor self)
		{
			var rearmComplete = true;
			foreach (var ammoPool in rearmable.RearmableAmmoPools)
			{
				if (!ammoPool.FullAmmo())
				{
					if (--ammoPool.RemainingTicks <= 0)
					{
						ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
						if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
							Game.Sound.PlayToPlayer(SoundType.World, self.Owner, ammoPool.Info.RearmSound, self.CenterPosition);

						ammoPool.GiveAmmo(self, ammoPool.Info.ReloadCount);
					}

					rearmComplete = false;
				}
			}

			if (rearmComplete)
				activeResupplyTypes &= ~ResupplyType.Rearm;
		}
	}
}
