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

		int remainingTicks;
		bool played;
		bool paused;
		bool repairComplete;
		bool rearmComplete;

		public Resupply(Actor self, Actor host, WDist closeEnough)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			allRepairsUnits = host.TraitsImplementing<RepairsUnits>().ToArray();
			health = self.TraitOrDefault<IHealth>();
			repairable = self.TraitOrDefault<Repairable>();
			repairableNear = self.TraitOrDefault<RepairableNear>();
			rearmable = self.TraitOrDefault<Rearmable>();

			repairComplete = health == null || health.DamageState == DamageState.Undamaged
				|| !allRepairsUnits.Any()
				|| ((repairable == null || !repairable.Info.RepairActors.Contains(host.Info.Name))
					&& (repairableNear == null || !repairableNear.Info.RepairActors.Contains(host.Info.Name)));

			rearmComplete = rearmable == null || !rearmable.Info.RearmActors.Contains(host.Info.Name) || rearmable.RearmableAmmoPools.All(p => p.FullAmmo());
		}

		protected override void OnFirstRun(Actor self)
		{
			if (host.Type == TargetType.Invalid)
				return;

			if (!repairComplete)
				foreach (var notifyRepair in host.Actor.TraitsImplementing<INotifyRepair>())
					notifyRepair.BeforeRepair(host.Actor, self);

			if (!rearmComplete)
			{
				foreach (var notifyRearm in host.Actor.TraitsImplementing<INotifyRearm>())
					notifyRearm.RearmingStarted(host.Actor, self);

				// Reset the ReloadDelay to avoid any issues with early cancellation
				// from previous reload attempts (explicit order, host building died, etc).
				// HACK: this really shouldn't be managed from here
				foreach (var pool in rearmable.RearmableAmmoPools)
					pool.RemainingTicks = pool.Info.ReloadDelay;
			}
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceling)
				return NextActivity;

			if (host.Type == TargetType.Invalid || health == null)
				return NextActivity;

			if (closeEnough.LengthSquared > 0 && !host.IsInRange(self.CenterPosition, closeEnough))
				return NextActivity;

			if (!repairComplete)
				RepairTick(self);

			if (!rearmComplete)
				RearmTick(self);

			if (repairComplete && rearmComplete)
				return NextActivity;

			return this;
		}

		void RepairTick(Actor self)
		{
			// First active.
			RepairsUnits repairsUnits = null;
			paused = false;
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
			{
				if (!paused)
					repairComplete = true;

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

				foreach (var notifyRepair in host.Actor.TraitsImplementing<INotifyRepair>())
					notifyRepair.AfterRepair(host.Actor, self);

				repairComplete = true;
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

				foreach (var depot in host.Actor.TraitsImplementing<INotifyRepair>())
					depot.RepairTick(host.Actor, self);

				remainingTicks = repairsUnits.Info.Interval;
			}
			else
				--remainingTicks;
		}

		void RearmTick(Actor self)
		{
			rearmComplete = true;
			foreach (var pool in rearmable.RearmableAmmoPools)
			{
				if (!pool.FullAmmo())
				{
					Reload(self, host.Actor, pool);
					rearmComplete = false;
				}
			}

			if (rearmComplete)
				foreach (var notifyRearm in host.Actor.TraitsImplementing<INotifyRearm>())
					notifyRearm.RearmingFinished(host.Actor, self);
		}

		void Reload(Actor self, Actor host, AmmoPool ammoPool)
		{
			if (--ammoPool.RemainingTicks <= 0)
			{
				foreach (var notify in host.TraitsImplementing<INotifyRearm>())
					notify.Rearming(host, self);

				ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
				if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, ammoPool.Info.RearmSound, self.CenterPosition);

				ammoPool.GiveAmmo(self, ammoPool.Info.ReloadCount);
			}
		}
	}
}
