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

using System.Linq;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	sealed class UnitsForProtectionIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }
		public void Tick(Squad owner) { owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackState(), true); }
		public void Deactivate(Squad owner) { }
	}

	sealed class UnitsForProtectionAttackState : GroundStateBase, IState
	{
		public const int BackoffTicks = 4;
		internal int Backoff = BackoffTicks;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid())
			{
				var target = owner.SquadManager.FindClosestEnemy(owner.Units.First(), WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));
				owner.SetActorToTarget(target);
				if (target.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					return;
				}
			}

			if (!owner.IsTargetVisible)
			{
				if (Backoff < 0)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					Backoff = BackoffTicks;
					return;
				}

				Backoff--;
			}
			else
				owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class UnitsForProtectionFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.SquadManager.UnregisterSquad(owner); }
	}
}
