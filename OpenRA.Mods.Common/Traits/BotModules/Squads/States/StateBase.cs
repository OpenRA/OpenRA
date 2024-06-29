#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	abstract class StateBase
	{
		protected static CPos RandomBuildingLocation(Squad squad)
		{
			var location = squad.SquadManager.GetRandomBaseCenter();
			var buildings = squad.World.ActorsHavingTrait<Building>()
				.Where(a => a.Owner == squad.Bot.Player).ToList();
			if (buildings.Count > 0)
				location = buildings.Random(squad.Random).Location;

			return location;
		}

		protected static bool BusyAttack(Actor a)
		{
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
			var type = activity.GetType();
			if (type == typeof(Attack) || type == typeof(FlyAttack))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();
			if (nextType == typeof(Attack) || nextType == typeof(FlyAttack))
				return true;

			return false;
		}

		protected static bool CanAttackTarget(Actor a, Actor target)
		{
			if (!a.Info.HasTraitInfo<AttackBaseInfo>())
				return false;

			var targetTypes = target.GetEnabledTargetTypes();
			if (targetTypes.IsEmpty)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
			{
				if (arm.IsTraitDisabled)
					continue;

				if (arm.Weapon.IsValidTarget(targetTypes))
					return true;
			}

			return false;
		}

		protected virtual bool ShouldFlee(Squad squad, Func<IReadOnlyCollection<Actor>, bool> flee)
		{
			if (!squad.IsValid)
				return false;

			var dangerRadius = squad.SquadManager.Info.DangerScanRadius;
			var units = squad.World.FindActorsInCircle(squad.CenterPosition(), WDist.FromCells(dangerRadius)).ToList();

			// If there are any own buildings within the DangerRadius, don't flee
			// PERF: Avoid LINQ
			foreach (var u in units)
				if (u.Owner == squad.Bot.Player && u.Info.HasTraitInfo<BuildingInfo>())
					return false;

			var enemyAroundUnit = units
				.Where(unit => squad.SquadManager.IsPreferredEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>())
				.ToList();
			if (enemyAroundUnit.Count == 0)
				return false;

			return flee(enemyAroundUnit);
		}

		protected static bool IsRearming(Actor a)
		{
			return !a.IsIdle && (a.CurrentActivity.ActivitiesImplementing<Resupply>().Any() || a.CurrentActivity.ActivitiesImplementing<ReturnToBase>().Any());
		}

		protected static bool FullAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (!ap.HasFullAmmo)
					return false;

			return true;
		}

		protected static bool HasAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (!ap.HasAmmo)
					return false;

			return true;
		}

		protected static bool ReloadsAutomatically(IEnumerable<AmmoPool> ammoPools, Rearmable rearmable)
		{
			if (rearmable == null)
				return true;

			foreach (var ap in ammoPools)
				if (!rearmable.Info.AmmoPools.Contains(ap.Info.Name))
					return false;

			return true;
		}

		// Retreat units from combat, or for supply only in idle
		protected static void Retreat(Squad squad, bool flee, bool rearm, bool repair)
		{
			// HACK: "alreadyRepair" is to solve Aircraft repair cannot queue,
			// if repairpad logic is better we can just drop it.
			var alreadyRepair = false;

			var rearmingUnits = new List<Actor>();
			var fleeingUnits = new List<Actor>();

			foreach (var u in squad.Units)
			{
				if (IsRearming(u))
					continue;

				var orderQueued = false;

				// Units need to rearm will be added to rearming group.
				if (rearm)
				{
					var ammoPools = u.TraitsImplementing<AmmoPool>().ToArray();
					if (!ReloadsAutomatically(ammoPools, u.TraitOrDefault<Rearmable>()) && !FullAmmo(ammoPools))
					{
						rearmingUnits.Add(u);
						orderQueued = true;
					}
				}

				// Units need to repair will be repaired.
				if (repair && !alreadyRepair)
				{
					Actor repairBuilding = null;
					var orderId = "Repair";
					var health = u.TraitOrDefault<IHealth>();

					if (health != null && health.DamageState > DamageState.Undamaged)
					{
						var repairable = u.TraitOrDefault<Repairable>();
						if (repairable != null)
							repairBuilding = repairable.FindRepairBuilding(u);
						else
						{
							var repairableNear = u.TraitOrDefault<RepairableNear>();
							if (repairableNear != null)
							{
								orderId = "RepairNear";
								repairBuilding = repairableNear.FindRepairBuilding(u);
							}
						}

						if (repairBuilding != null)
						{
							squad.Bot.QueueOrder(new Order(orderId, u, Target.FromActor(repairBuilding), orderQueued));
							orderQueued = true;
							if (squad.Type == SquadType.Air)
								alreadyRepair = true;
						}
					}
				}

				// If there is no order in queue and units should flee, add unit to fleeing group.
				if (flee && !orderQueued)
					fleeingUnits.Add(u);
			}

			if (rearmingUnits.Count > 0)
				squad.Bot.QueueOrder(new Order("ReturnToBase", null, true, groupedActors: rearmingUnits.ToArray()));

			if (fleeingUnits.Count > 0)
				squad.Bot.QueueOrder(new Order("Move", null, Target.FromCell(squad.World, RandomBuildingLocation(squad)), false, groupedActors: fleeingUnits.ToArray()));
		}
	}
}
