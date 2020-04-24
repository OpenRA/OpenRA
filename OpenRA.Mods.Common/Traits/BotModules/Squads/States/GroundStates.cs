#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	abstract class GroundStateBase : StateBase
	{
		protected virtual bool ShouldFlee(Squad owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemies));
		}

		protected Actor FindClosestEnemy(Squad owner)
		{
			return owner.SquadManager.FindClosestEnemy(owner.Units.First().CenterPosition);
		}

		protected static bool IsRearm(Actor a)
		{
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
			var type = activity.GetType();
			if (type == typeof(Resupply))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();
			if (nextType == typeof(Resupply))
				return true;

			return false;
		}

		// Retreat units from combat, or for supply only in idle
		protected virtual void Retreat(Squad owner, bool resupplyonly)
		{
			// Repair units. One by one to avoid give out mass orders
			foreach (var a in owner.Units)
			{
				if (IsRearm(a))
					continue;

				Actor repairBuilding = null;
				var orderId = "Repair";
				var alreadysend = false;

				if (!alreadysend && a.TraitOrDefault<IHealth>() != null && a.TraitOrDefault<IHealth>().DamageState > DamageState.Undamaged)
				{
					var repairable = a.TraitOrDefault<Repairable>();
					if (repairable != null)
						repairBuilding = repairable.FindRepairBuilding(a);
					else
					{
						var repairableNear = a.TraitOrDefault<RepairableNear>();
						if (repairableNear != null)
						{
							orderId = "RepairNear";
							repairBuilding = repairableNear.FindRepairBuilding(a);
						}
					}

					if (repairBuilding != null)
					{
						owner.Bot.QueueOrder(new Order(orderId, a, Target.FromActor(repairBuilding), false));
						alreadysend = true;
						continue;
					}
					else if (!resupplyonly)
						owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
				}
				else if (!resupplyonly)
					owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}
		}
	}

	class GroundUnitsIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy == null)
					return;

				owner.TargetActor = closestEnemy;
			}

			var enemyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
				.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsHiddenUnit(a)).ToList();

			if (enemyUnits.Count == 0)
			{
				Retreat(owner, true);
				return;
			}

			if (AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemyUnits))
			{
				foreach (var u in owner.Units)
					owner.Bot.QueueOrder(new Order("AttackMove", u, Target.FromCell(owner.World, owner.TargetActor.Location), false));

				// We have gathered sufficient units. Attack the nearest enemy unit.
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	class GroundUnitsAttackMoveState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (leader == null)
				return;

			var ownUnits = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.Units.Count) / 3)
				.Where(a => a.Owner == owner.Units.First().Owner && owner.Units.Contains(a)).ToHashSet();

			if (ownUnits.Count < owner.Units.Count)
			{
				// Since units have different movement speeds, they get separated while approaching the target.
				// Let them regroup into tighter formation.
				owner.Bot.QueueOrder(new Order("Stop", leader, false));
				foreach (var unit in owner.Units.Where(a => !ownUnits.Contains(a)))
					owner.Bot.QueueOrder(new Order("AttackMove", unit, Target.FromCell(owner.World, leader.Location), false));
			}
			else
			{
				var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius))
					.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsHiddenUnit(a));
				var target = enemies.ClosestTo(leader.CenterPosition);
				if (target != null)
				{
					owner.TargetActor = target;
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				}
				else
					foreach (var a in owner.Units)
						owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));
			}

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	class GroundUnitsAttackState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			// rescan target to prevent being ambushed and die without fight
			// return to AttackMove state for formation
			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius))
				.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsHiddenUnit(a));
			var target = enemies.ClosestTo(leader.CenterPosition);
			if (target == null)
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
			else
			{
				if (target == owner.TargetActor)
				{
					foreach (var a in owner.Units)
					{
						if (!BusyAttack(a))
						{
							if (CanAttackTarget(a, target))
								owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							else
								owner.Bot.QueueOrder(new Order("Guard", a, Target.FromActor(leader), false));
						}
					}
				}
				else
					owner.TargetActor = target;
			}

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	class GroundUnitsFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, false);

			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.Units.Clear(); }
	}
}
