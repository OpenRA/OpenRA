#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
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
				var targeter = owner.units.First();
				var t = owner.bot.FindClosestEnemy(targeter, targeter.CenterPosition);
				if (t == null)
					return;

				owner.Target = t;
			}

			var enemyUnits = owner.world.FindActorsInCircle(owner.Target.CenterPosition, WRange.FromCells(10))
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
		public void Activate(Squad s) { }

		public void Tick(Squad s)
		{
			if (!s.IsValid)
				return;

			if (!s.TargetIsValid)
			{
				var targeter = s.units.Random(s.random);
				var closestEnemy = s.bot.FindClosestEnemy(targeter, targeter.CenterPosition);
				if (closestEnemy != null)
					s.Target = closestEnemy;
				else
				{
					s.fsm.ChangeState(s, new GroundUnitsFleeState(), true);
					return;
				}
			}

			// Force the squad to move as a group
			var leader = s.units.ClosestTo(s.Target.CenterPosition);
			var leaderPos = leader.CenterPosition;
			var nearRange = WRange.FromCells(s.units.Count) / 3;
			var nearLeader = s.units.Where(a => (a.CenterPosition - leaderPos).HorizontalLengthSquared < nearRange.Range * nearRange.Range);

			if (nearLeader.Count() < s.units.Count)
			{
				// Wait for the stragglers to catch up
				foreach (var a in s.units)
				{
					if (nearLeader.Contains(a))
						s.world.IssueOrder(new Order("Stop", a, false));
					else
						s.world.IssueOrder(new Order("AttackMove", a, false) { TargetLocation = s.Target.Location });
				}
			}
			else
			{
				// Units are grouped together - are they close enough to attack a target directly?
				var target = s.bot.FindClosestEnemy(leader, leaderPos, WRange.FromCells(12));
				if (target != null)
				{
					s.Target = target;
					s.fsm.ChangeState(s, new GroundUnitsAttackState(), true);
				}
				else
				{
					// No target nearby - keep moving
					foreach (var a in s.units)
						s.world.IssueOrder(new Order("AttackMove", a, false) { TargetLocation = s.Target.Location });
				}
			}

			if (ShouldFlee(s))
				s.fsm.ChangeState(s, new GroundUnitsFleeState(), true);
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
				var targeter = owner.units.Random(owner.random);
				var closestEnemy = owner.bot.FindClosestEnemy(targeter, targeter.CenterPosition);
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
					owner.world.IssueOrder(new Order("Attack", a, false) { TargetActor = owner.bot.FindClosestEnemy(a, a.CenterPosition) });

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
