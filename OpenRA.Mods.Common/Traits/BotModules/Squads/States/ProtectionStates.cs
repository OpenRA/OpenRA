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
	class ProtectionStateBase : GroundStateBase
	{
		protected static bool FullAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.HasFullAmmo);
		}

		protected static bool HasAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.HasAmmo);
		}

		protected static bool ReloadsAutomatically(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			var rearmable = a.TraitOrDefault<Rearmable>();
			if (rearmable == null)
				return true;

			return ammoPools.All(ap => !rearmable.Info.AmmoPools.Contains(ap.Info.Name));
		}
	}

	class UnitsForProtectionIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }
		public void Tick(Squad owner) { owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackState(), true); }
		public void Deactivate(Squad owner) { }
	}

	class UnitsForProtectionAttackState : ProtectionStateBase, IState
	{
		public const int BackoffTicks = 4;
		internal int Backoff = BackoffTicks;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(owner.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));

				if (owner.TargetActor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					return;
				}
			}

			// rescan target to prevent being ambushed and die without fight
			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius))
				.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a));
			var target = enemies.ClosestTo(leader.CenterPosition);
			if (target != null)
				owner.TargetActor = target;

			var cannotRetaliate = false;

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
			{
				cannotRetaliate = true;

				foreach (var a in owner.Units)
				{
					// Air units control:
					if (a.Info.HasTraitInfo<AircraftInfo>() && a.TraitsImplementing<AmmoPool>().Any())
					{
						if (BusyAttack(a))
						{
							cannotRetaliate = false;
							continue;
						}

						if (!ReloadsAutomatically(a))
						{
							if (IsRearm(a))
								continue;

							if (!HasAmmo(a))
							{
								owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
								continue;
							}
						}

						if (CanAttackTarget(a, owner.TargetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else if (leader.TraitsImplementing<Guardable>().Any())
							owner.Bot.QueueOrder(new Order("Guard", a, Target.FromActor(leader), false));
					}

					// Ground/naval units control:
					else
					{
						if (CanAttackTarget(a, owner.TargetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else if (leader.TraitsImplementing<Guardable>().Any())
							owner.Bot.QueueOrder(new Order("Guard", a, Target.FromActor(leader), false));
					}
				}
			}

			if (cannotRetaliate)
				owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	class UnitsForProtectionFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.Units.Clear(); }
	}
}
