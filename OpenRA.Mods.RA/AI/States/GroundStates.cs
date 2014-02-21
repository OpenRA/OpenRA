#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	abstract class GroundStateBase : StateBase
	{
		protected virtual bool ShouldFlee(Squad owner)
		{
			return base.ShouldFlee(owner, enemies => !owner.attackOrFleeFuzzy.CanAttack(owner.units, enemies));
		}
	}

	class GroundUnitsIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.TargetIsValid)
			{
				var t = owner.bot.FindClosestEnemy(owner.units.FirstOrDefault().CenterPosition);
				if (t == null) return;
				owner.Target = t;
			}

			var enemyUnits = owner.world.FindActorsInCircle(owner.Target.CenterPosition, WDist.FromCells(10))
				.Where(unit => owner.bot.p.Stances[unit.Owner] == Stance.Enemy).ToList();

			if (enemyUnits.Any())
			{
				if (owner.attackOrFleeFuzzy.CanAttack(owner.units, enemyUnits))
				{
					foreach (var u in owner.units)
						owner.world.IssueOrder(new Order("AttackMove", u, false) { TargetLocation = owner.Target.CenterPosition.ToCPos() });

					// We have gathered sufficient units. Attack the nearest enemy unit.
					owner.fsm.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
					return;
				}
				else
					owner.fsm.ChangeState(owner, new GroundUnitsFleeState(), true);
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

			if (!owner.TargetIsValid)
			{
				var closestEnemy = owner.bot.FindClosestEnemy(owner.units.Random(owner.random).CenterPosition);
				if (closestEnemy != null)
					owner.Target = closestEnemy;
				else
				{
					owner.fsm.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			Actor leader = owner.units.ClosestTo(owner.Target.CenterPosition);
			if (leader == null)
				return;
			var ownUnits = owner.world.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.units.Count) / 3)
				.Where(a => a.Owner == owner.units.FirstOrDefault().Owner && owner.units.Contains(a)).ToList();
			if (ownUnits.Count < owner.units.Count)
			{
				owner.world.IssueOrder(new Order("Stop", leader, false));
				foreach (var unit in owner.units.Where(a => !ownUnits.Contains(a)))
					owner.world.IssueOrder(new Order("AttackMove", unit, false) { TargetLocation = leader.CenterPosition.ToCPos() });
			}
			else
			{
				var enemies = owner.world.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(12))
					.Where(a1 => !a1.Destroyed && !a1.IsDead()).ToList();
				var enemynearby = enemies.Where(a1 => a1.HasTrait<ITargetable>() && leader.Owner.Stances[a1.Owner] == Stance.Enemy).ToList();
				if (enemynearby.Any())
				{
					owner.Target = enemynearby.ClosestTo(leader.CenterPosition);
					owner.fsm.ChangeState(owner, new GroundUnitsAttackState(), true);
					return;
				}
				else
					foreach (var a in owner.units)
						owner.world.IssueOrder(new Order("AttackMove", a, false) { TargetLocation = owner.Target.Location });
			}

			if (ShouldFlee(owner))
			{
				owner.fsm.ChangeState(owner, new GroundUnitsFleeState(), true);
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

			if (!owner.TargetIsValid)
			{
				var closestEnemy = owner.bot.FindClosestEnemy(owner.units.Random(owner.random).CenterPosition);
				if (closestEnemy != null)
					owner.Target = closestEnemy;
				else
				{
					owner.fsm.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			foreach (var a in owner.units)
				if (!BusyAttack(a))
					owner.world.IssueOrder(new Order("Attack", a, false) { TargetActor = owner.bot.FindClosestEnemy(a.CenterPosition) });

			if (ShouldFlee(owner))
			{
				owner.fsm.ChangeState(owner, new GroundUnitsFleeState(), true);
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
			owner.fsm.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.units.Clear(); }
	}
}
