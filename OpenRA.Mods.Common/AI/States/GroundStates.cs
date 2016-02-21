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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	abstract class GroundStateBase : StateBase
	{
		protected virtual bool ShouldFlee(Squad owner)
		{
			return base.ShouldFlee(owner, enemies => !AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemies));
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
				var t = owner.Bot.FindClosestEnemy(owner.Units.FirstOrDefault().CenterPosition);
				if (t == null) return;
				owner.TargetActor = t;
			}

			var enemyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(10))
				.Where(unit => owner.Bot.Player.Stances[unit.Owner] == Stance.Enemy).ToList();

			if (enemyUnits.Any())
			{
				if (AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemyUnits))
				{
					foreach (var u in owner.Units)
						owner.Bot.QueueOrder(new Order("AttackMove", u, false) { TargetLocation = owner.TargetActor.Location });

					// We have gathered sufficient units. Attack the nearest enemy unit.
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
					return;
				}
				else
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
			}
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
				var closestEnemy = owner.Bot.FindClosestEnemy(owner.Units.Random(owner.Random).CenterPosition);
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
				.Where(a => a.Owner == owner.Units.FirstOrDefault().Owner && owner.Units.Contains(a)).ToHashSet();
			if (ownUnits.Count < owner.Units.Count)
			{
				owner.Bot.QueueOrder(new Order("Stop", leader, false));
				foreach (var unit in owner.Units.Where(a => !ownUnits.Contains(a)))
					owner.Bot.QueueOrder(new Order("AttackMove", unit, false) { TargetLocation = leader.Location });
			}
			else
			{
				var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(12))
					.Where(a1 => !a1.Disposed && !a1.IsDead);
				var enemynearby = enemies.Where(a1 => a1.Info.HasTraitInfo<ITargetableInfo>() && leader.Owner.Stances[a1.Owner] == Stance.Enemy);
				var target = enemynearby.ClosestTo(leader.CenterPosition);
				if (target != null)
				{
					owner.TargetActor = target;
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
					return;
				}
				else
					foreach (var a in owner.Units)
						owner.Bot.QueueOrder(new Order("AttackMove", a, false) { TargetLocation = owner.TargetActor.Location });
			}

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
				return;
			}
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
				var closestEnemy = owner.Bot.FindClosestEnemy(owner.Units.Random(owner.Random).CenterPosition);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("Attack", a, false) { TargetActor = owner.Bot.FindClosestEnemy(a.CenterPosition) });

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
				return;
			}
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

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.Units.Clear(); }
	}
}
