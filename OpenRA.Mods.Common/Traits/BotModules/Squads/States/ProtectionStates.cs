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

		// Retreat units from combat, or for supply only in idle
		protected override void Retreat(Squad owner, bool resupplyonly = false)
		{
			// Reload units.
			foreach (var a in owner.Units)
			{
				if (!ReloadsAutomatically(a) && !FullAmmo(a))
				{
					if (IsRearm(a))
						continue;

					owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
					continue;
				}
				else if (!resupplyonly)
					owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}

			// Repair units. One by one to avoid give out mass orders
			foreach (var a in owner.Units)
			{
				if (IsRearm(a))
					continue;

				Actor repairBuilding = null;
				var orderId = "Repair";

				if (a.TraitOrDefault<IHealth>() != null && a.TraitOrDefault<IHealth>().DamageState > DamageState.Undamaged)
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
						owner.Bot.QueueOrder(new Order(orderId, a, Target.FromActor(repairBuilding), true));
						break;
					}
				}
			}
		}
	}

	class UnitsForProtectionIdleState : ProtectionStateBase, IState
	{
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
					Retreat(owner, true);
					return;
				}
			}

			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackState(), true);
		}

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
			var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius))
				.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsHiddenUnit(a));
			var target = enemies.ClosestTo(leader.CenterPosition);
			if (target != null)
				owner.TargetActor = target;

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
				foreach (var a in owner.Units)
				{
					if (a.Info.HasTraitInfo<AircraftInfo>() && a.TraitsImplementing<AmmoPool>().Any())
					{
						if (BusyAttack(a))
							continue;

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

						// avoid AttackMove, since AttackMove will let fragile unit walk on the face of enemy
						if (CanAttackTarget(a, owner.TargetActor))
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
						else
							owner.Bot.QueueOrder(new Order("Guard", a, Target.FromActor(leader), false));
					}
					else
					{
						if (CanAttackTarget(a, owner.TargetActor))
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
						else
							owner.Bot.QueueOrder(new Order("Guard", a, Target.FromActor(leader), false));
					}
				}
			}
		}

		public void Deactivate(Squad owner) { }
	}

	class UnitsForProtectionFleeState : ProtectionStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner);

			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.Units.Clear(); }
	}
}
