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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
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
		readonly ICallForTransport[] transportCallers;
		readonly IMove move;
		readonly Aircraft aircraft;

		int remainingTicks;
		bool played;
		bool actualResupplyStarted;
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
			transportCallers = self.TraitsImplementing<ICallForTransport>().ToArray();
			move = self.Trait<IMove>();
			aircraft = move as Aircraft;

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

		public override bool Tick(Actor self)
		{
			// HACK: If the activity is cancelled while we're already resupplying (or about to start resupplying),
			// move actor outside the resupplier footprint.
			// TODO: This check is nowhere near robust enough, and should be rewritten.
			if (IsCanceling && host.IsInRange(self.CenterPosition, closeEnough))
			{
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.None);

				if (aircraft != null)
				{
					aircraft.AllowYieldingReservation();
					if (aircraft.Info.FlightDynamics.HasFlag(FlightDynamic.TakeOffOnResupply))
						Queue(new TakeOff(self));

					return true;
				}
				else if (repairableNear != null)
					return true;
				else
				{
					QueueChild(move.MoveToTarget(self, host));
					return false;
				}
			}
			else if (IsCanceling || host.Type != TargetType.Actor || !host.Actor.IsInWorld || host.Actor.IsDead)
			{
				// This is necessary to ensure host resupply actions (like animations) finish properly
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.None);

				return true;
			}
			else if (activeResupplyTypes != 0 && aircraft == null &&
				(closeEnough.LengthSquared > 0 && !host.IsInRange(self.CenterPosition, closeEnough)))
			{
				var targetCell = self.World.Map.CellContaining(host.Actor.CenterPosition);
				List<Activity> movement = new List<Activity>();

				movement.Add(move.MoveWithinRange(host, closeEnough, targetLineColor: Color.Green));

				// HACK: Repairable needs the actor to move to host center.
				// TODO: Get rid of this or at least replace it with something less hacky.
				if (repairableNear == null)
					movement.Add(move.MoveTo(targetCell, host.Actor));

				var moveActivities = ActivityUtils.SequenceActivities(movement.ToArray());

				var delta = (self.CenterPosition - host.CenterPosition).LengthSquared;
				var transport = transportCallers.FirstOrDefault(t => t.MinimumDistance.LengthSquared < delta);
				if (transport != null)
				{
					QueueChild(new WaitForTransport(self, moveActivities));

					// TODO: Make this compatible with RepairableNear
					transport.RequestTransport(self, targetCell, new Resupply(self, host.Actor, closeEnough));
				}
				else
					QueueChild(moveActivities);

				return false;
			}

			// We don't want to trigger this until we've reached the resupplier and can start resupplying
			if (!actualResupplyStarted && activeResupplyTypes > 0)
			{
				actualResupplyStarted = true;
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.BeforeResupply(host.Actor, self, activeResupplyTypes);

				// Reset the ReloadDelay to avoid any issues with early cancellation
				// from previous reload attempts (explicit order, host building died, etc).
				// HACK: this really shouldn't be managed from here
				if (activeResupplyTypes.HasFlag(ResupplyType.Rearm))
					foreach (var pool in rearmable.RearmableAmmoPools)
						pool.RemainingTicks = pool.Info.ReloadDelay;
			}

			if (activeResupplyTypes.HasFlag(ResupplyType.Repair))
				RepairTick(self);

			if (activeResupplyTypes.HasFlag(ResupplyType.Rearm))
				RearmTick(self);

			foreach (var notifyResupply in notifyResupplies)
				notifyResupply.ResupplyTick(host.Actor, self, activeResupplyTypes);

			if (activeResupplyTypes == 0)
			{
				if (aircraft != null)
					aircraft.AllowYieldingReservation();

				if (self.CurrentActivity.NextActivity == null)
				{
					var rp = host.Actor.TraitOrDefault<RallyPoint>();
					if (rp != null)
						Queue(move.MoveTo(rp.Location, repairableNear != null ? null : host.Actor));
					else if (repairableNear == null)
						Queue(move.MoveToTarget(self, host));
				}

				return true;
			}

			return false;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			if (NextActivity != null)
				return;

			base.Cancel(self, keepQueue);
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
