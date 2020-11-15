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

		protected Actor GetRandomPreferredTarget(Squad owner)
		{
			var manager = owner.SquadManager;
			var preferredTargets = manager.World.ActorsHavingTrait<MustBeDestroyed>(t => t.Info.RequiredForShortGame)
					.Where(a => manager.IsPreferredEnemyUnit(a) && manager.IsNotHiddenUnit(a)).ToArray();

			if (!preferredTargets.Any())
				return FindClosestEnemy(owner);

			return preferredTargets.Random(owner.World.LocalRandom);
		}

		protected Actor ScanThreat(Squad owner, Actor teamLeader, WDist scanRadius)
		{
			var enemies = owner.World.FindActorsInCircle(teamLeader.CenterPosition, scanRadius)
					.Where(a => owner.SquadManager.IsPreferredEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a));
			return enemies.ClosestTo(teamLeader.CenterPosition);
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
				.Where(owner.SquadManager.IsPreferredEnemyUnit).ToList();

			if (enemyUnits.Count == 0)
				return;

			if (AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemyUnits))
			{
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
		const int MaxAttemptsToAdvance = 6;
		const int MakeWayTicks = 2;

		// Give tolerance for AI grouping team at start
		int attemptsToAdvance = MaxAttemptsToAdvance * 3;
		int makeWay = MakeWayTicks;
		WPos lastPos = WPos.Zero;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var targetActor = GetRandomPreferredTarget(owner);
				if (targetActor != null)
					owner.TargetActor = targetActor;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			// Initialize leader. Optimaze pathfinding by using leader.
			var leader = owner.Units.FirstOrDefault();
			if (leader == null)
				return;

			// 1. Scanning if there is enemy around and switch to GroundUnitsAttackState if any.
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			var enemyActor = ScanThreat(owner, leader, attackScanRadius);
			if (enemyActor != null)
			{
				owner.TargetActor = enemyActor;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				return;
			}

			// 2. Make sure the guide unit has not been blocked by the rest of the squad
			if (attemptsToAdvance <= 0)
			{
				if (makeWay > 0)
				{
					owner.Bot.QueueOrder(new Order("AttackMove", leader, Target.FromCell(owner.World, owner.TargetActor.Location), false));

					foreach (var a in owner.Units)
					{
						if (a != leader)
							owner.Bot.QueueOrder(new Order("Scatter", a, false));
					}

					makeWay--;
				}
				else
				{
					// When going through is over, restore the check
					attemptsToAdvance = MaxAttemptsToAdvance + MakeWayTicks;
					makeWay = MakeWayTicks;
				}

				return;
			}

			// 3. Check if the squad is stuck due to the map having a very twisted path
			// or currently bridge and tunnel from TS mod
			if ((leader.CenterPosition - lastPos).LengthSquared <= 4)
				attemptsToAdvance--;
			else
				attemptsToAdvance = MaxAttemptsToAdvance;

			lastPos = leader.CenterPosition;

			// 4. Since units have different movement speeds, they get separated while approaching the target.

			// Let them regroup into tighter formation towards "leader".
			//
			// "groupArea" means the space the squad units will occupy (if 1 per Cell).
			// leader only stop when scope of "lookAround" is not covered all units;
			// units in "unitsHurryUp"  will catch up, which keep the team tight while not stuck.
			//
			// Imagining "groupArea" takes up a a place shape like square, we need to draw a circle
			// to cover the the enitire circle.
			//
			// PathGuide will check how many squad members are around
			// to decide if it needs to continue. And units that need hurry up will try
			// catch up before guide waiting.
			//
			// However in practice because of the poor PF, squad tend to PF to a ellipse.
			// "guideWaitCheck" now has the radius of two times of the circle mentioned before.
			var occupiedArea = (long)WDist.FromCells(owner.Units.Count).Length * 1024;

			var unitsHurryUp = owner.Units.Where(a => (a.CenterPosition - leader.CenterPosition).LengthSquared >= occupiedArea * 2);
			var guideWaitCheck = owner.Units.Any(a => (a.CenterPosition - leader.CenterPosition).LengthSquared > occupiedArea * 5);

			if (guideWaitCheck)
				owner.Bot.QueueOrder(new Order("Stop", leader, false));
			else
				owner.Bot.QueueOrder(new Order("AttackMove", leader, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			foreach (var unit in unitsHurryUp)
				owner.Bot.QueueOrder(new Order("AttackMove", unit, Target.FromCell(owner.World, leader.Location), false));
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

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));

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

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.Units.Clear(); }
	}
}
