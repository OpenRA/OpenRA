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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	abstract class GroundStateBase : StateBase
	{
		Actor leader;

		/// <summary>
		/// Elects a unit to lead the squad, other units in the squad will regroup to the leader if they start to spread out.
		/// The leader remains the same unless a new one is forced or the leader is no longer part of the squad.
		/// </summary>
		protected Actor Leader(Squad owner)
		{
			if (leader == null || !owner.Units.Contains(leader))
				leader = NewLeader(owner);
			return leader;
		}

		static Actor NewLeader(Squad owner)
		{
			IEnumerable<Actor> units = owner.Units;

			// Identify the Locomotor with the most restrictive passable terrain list. For squads with mixed
			// locomotors, we hope to choose the most restrictive option. This means we won't nominate a leader who has
			// more options. This avoids situations where we would nominate a hovercraft as the leader and tanks would
			// fail to follow it because they can't go over water. By forcing us to choose a unit with limited movement
			// options, we maximise the chance other units will be able to follow it. We could still be screwed if the
			// squad has a mix of units with disparate movement, e.g. land units and naval units. We must trust the
			// squad has been formed from a set of units that don't suffer this problem.
			var leastCommonDenominator = units
				.Select(a => a.TraitOrDefault<Mobile>()?.Locomotor)
				.Where(l => l != null)
				.MinByOrDefault(l => l.Info.TerrainSpeeds.Count)
				?.Info.TerrainSpeeds.Count;
			if (leastCommonDenominator != null)
				units = units.Where(a => a.TraitOrDefault<Mobile>()?.Locomotor.Info.TerrainSpeeds.Count == leastCommonDenominator).ToList();

			// Choosing a unit in the center reduces the need for an immediate regroup.
			var centerPosition = units.Select(a => a.CenterPosition).Average();
			return units.MinBy(a => (a.CenterPosition - centerPosition).LengthSquared);
		}

		protected virtual bool ShouldFlee(Squad owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemies));
		}

		protected (Actor Actor, WVec Offset) NewLeaderAndFindClosestEnemy(Squad owner)
		{
			leader = null; // Force a new leader to be elected, useful if we are targeting a new enemy.
			return owner.SquadManager.FindClosestEnemy(Leader(owner));
		}

		protected IEnumerable<(Actor Actor, WVec Offset)> FindEnemies(Squad owner, IEnumerable<Actor> actors)
		{
			return owner.SquadManager.FindEnemies(
				actors,
				Leader(owner));
		}

		protected static Actor ClosestToEnemy(Squad owner)
		{
			return SquadManagerBotModule.ClosestTo(owner.Units, owner.TargetActor);
		}
	}

	sealed class GroundUnitsIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
					return;
			}

			var enemyUnits =
				FindEnemies(owner,
					owner.World.FindActorsInCircle(owner.Target.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius)))
				.Select(x => x.Actor)
				.ToList();

			if (enemyUnits.Count == 0)
				return;

			if (AttackOrFleeFuzzy.Default.CanAttack(owner.Units, enemyUnits))
			{
				owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));

				// We have gathered sufficient units. Attack the nearest enemy unit.
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class GroundUnitsAttackMoveState : GroundStateBase, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			var leader = Leader(owner);
			if (leader.Location != lastLeaderLocation)
			{
				lastLeaderLocation = leader.Location;
				lastUpdatedTick = owner.World.WorldTick;
			}

			if (owner.TargetActor != lastTarget)
			{
				lastTarget = owner.TargetActor;
				lastUpdatedTick = owner.World.WorldTick;
			}

			// HACK: Drop back to the idle state if we haven't moved in 2.5 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 63)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
				return;
			}

			var ownUnits = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.Units.Count) / 3)
				.Where(owner.Units.Contains).ToHashSet();

			if (ownUnits.Count < owner.Units.Count)
			{
				// Since units have different movement speeds, they get separated while approaching the target.
				// Let them regroup into tighter formation.
				owner.Bot.QueueOrder(new Order("Stop", leader, false));

				var units = owner.Units.Where(a => !ownUnits.Contains(a)).ToArray();
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: units));
			}
			else
			{
				var target = owner.SquadManager.FindClosestEnemy(leader, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius));
				if (target.Actor != null)
				{
					owner.SetActorToTarget(target);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				}
				else
					owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));
			}

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class GroundUnitsAttackState : GroundStateBase, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			var leader = Leader(owner);
			if (leader.Location != lastLeaderLocation)
			{
				lastLeaderLocation = leader.Location;
				lastUpdatedTick = owner.World.WorldTick;
			}

			if (owner.TargetActor != lastTarget)
			{
				lastTarget = owner.TargetActor;
				lastUpdatedTick = owner.World.WorldTick;
			}

			// HACK: Drop back to the idle state if we haven't moved in 2.5 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 63)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
				return;
			}

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("AttackMove", a, owner.Target, false));

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class GroundUnitsFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.SquadManager.UnregisterSquad(owner); }
	}
}
