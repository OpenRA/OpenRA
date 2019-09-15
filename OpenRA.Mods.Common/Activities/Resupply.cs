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
		readonly bool stayOnResupplier;
		readonly bool wasRepaired;

		int remainingTicks;
		bool played;
		bool actualResupplyStarted;
		ResupplyType activeResupplyTypes = ResupplyType.None;

		public Resupply(Actor self, Actor host, WDist closeEnough, bool stayOnResupplier = false)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			this.stayOnResupplier = stayOnResupplier;
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
			{
				activeResupplyTypes |= ResupplyType.Repair;

				// HACK: Reservable logic can't handle repairs, so force a take-off if resupply included repairs.
				// TODO: Make reservation logic or future docking logic properly handle this.
				wasRepaired = true;
			}

			var cannotRearmAtHost = rearmable == null || !rearmable.Info.RearmActors.Contains(host.Info.Name) || rearmable.RearmableAmmoPools.All(p => p.FullAmmo());
			if (!cannotRearmAtHost)
				activeResupplyTypes |= ResupplyType.Rearm;
		}

		public override bool Tick(Actor self)
		{
			// Wait for the cooldown to expire before releasing the unit if this was cancelled
			if (IsCanceling && remainingTicks > 0)
			{
				remainingTicks--;
				return false;
			}

			var isHostInvalid = host.Type != TargetType.Actor || !host.Actor.IsInWorld;
			var isCloseEnough = false;
			if (!isHostInvalid)
			{
				// Negative means there's no distance limit.
				// If RepairableNear, use TargetablePositions instead of CenterPosition
				// to ensure the actor moves close enough to the host.
				// Otherwise check against host CenterPosition.
				if (closeEnough < WDist.Zero)
					isCloseEnough = true;
				else if (repairableNear != null)
					isCloseEnough = host.IsInRange(self.CenterPosition, closeEnough);
				else
					isCloseEnough = (host.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= closeEnough.LengthSquared;
			}

			// This ensures transports are also cancelled when the host becomes invalid
			if (!IsCanceling && isHostInvalid)
				Cancel(self, true);

			if (IsCanceling || isHostInvalid)
			{
				// Only tick host INotifyResupply traits one last time if host is still alive
				if (!isHostInvalid)
					foreach (var notifyResupply in notifyResupplies)
						notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.None);

				// HACK: If the activity is cancelled while we're on the host resupplying (or about to start resupplying),
				// move actor outside the resupplier footprint to prevent it from blocking other actors.
				// Additionally, if the host is no longer valid, make aircaft take off.
				if (isCloseEnough || isHostInvalid)
					OnResupplyEnding(self, isHostInvalid);

				return true;
			}
			else if (activeResupplyTypes != 0 && aircraft == null && !isCloseEnough)
			{
				var targetCell = self.World.Map.CellContaining(host.Actor.CenterPosition);

				QueueChild(move.MoveWithinRange(host, closeEnough, targetLineColor: Color.Green));

				// HACK: Repairable needs the actor to move to host center.
				// TODO: Get rid of this or at least replace it with something less hacky.
				if (repairableNear == null)
					QueueChild(move.MoveTo(targetCell, host.Actor));

				var delta = (self.CenterPosition - host.CenterPosition).LengthSquared;
				var transport = transportCallers.FirstOrDefault(t => t.MinimumDistance.LengthSquared < delta);
				if (transport != null)
					transport.RequestTransport(self, targetCell);

				return false;
			}

			// We don't want to trigger this until we've reached the resupplier and can start resupplying
			if (!actualResupplyStarted && activeResupplyTypes > 0)
			{
				actualResupplyStarted = true;
				foreach (var notifyResupply in notifyResupplies)
					notifyResupply.BeforeResupply(host.Actor, self, activeResupplyTypes);
			}

			if (activeResupplyTypes.HasFlag(ResupplyType.Repair))
				RepairTick(self);

			if (activeResupplyTypes.HasFlag(ResupplyType.Rearm))
				RearmTick(self);

			foreach (var notifyResupply in notifyResupplies)
				notifyResupply.ResupplyTick(host.Actor, self, activeResupplyTypes);

			if (activeResupplyTypes == 0)
			{
				OnResupplyEnding(self);
				return true;
			}

			return false;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			foreach (var t in transportCallers)
				t.MovementCancelled(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity == null)
				yield return new TargetLineNode(host, Color.Green);
			else
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;
		}

		void OnResupplyEnding(Actor self, bool isHostInvalid = false)
		{
			var rp = !isHostInvalid ? host.Actor.TraitOrDefault<RallyPoint>() : null;
			if (aircraft != null)
			{
				if (wasRepaired || isHostInvalid || (!stayOnResupplier && aircraft.Info.TakeOffOnResupply))
				{
					if (self.CurrentActivity.NextActivity == null && rp != null)
						QueueChild(move.MoveTo(rp.Location, repairableNear != null ? null : host.Actor, targetLineColor: Color.Green));
					else
						QueueChild(new TakeOff(self));

					aircraft.UnReserve();
				}

				// Aircraft without TakeOffOnResupply remain on the resupplier until something else needs it
				// The rally point location is queried by the aircraft before it takes off
				else
					aircraft.AllowYieldingReservation();
			}
			else if (!stayOnResupplier && !isHostInvalid)
			{
				// If there's no next activity, move to rallypoint if available, else just leave host if Repairable.
				// Do nothing if RepairableNear (RepairableNear actors don't enter their host and will likely remain within closeEnough).
				// If there's a next activity and we're not RepairableNear, first leave host if the next activity is not a Move.
				if (self.CurrentActivity.NextActivity == null)
				{
					if (rp != null)
						QueueChild(move.MoveTo(rp.Location, repairableNear != null ? null : host.Actor));
					else if (repairableNear == null)
						QueueChild(move.MoveToTarget(self, host));
				}
				else if (repairableNear == null && !(self.CurrentActivity.NextActivity is Move))
					QueueChild(move.MoveToTarget(self, host));
			}
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
