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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	abstract class AirStateBase : StateBase
	{
		protected const int MissileUnitMultiplier = 3;

		protected static int CountAntiAirUnits(IEnumerable<Actor> units)
		{
			if (!units.Any())
				return 0;

			var missileUnitsCount = 0;
			foreach (var unit in units)
			{
				if (unit != null && unit.HasTrait<AttackBase>() && !unit.HasTrait<Aircraft>()
					&& !unit.IsDisabled())
				{
					var arms = unit.TraitsImplementing<Armament>();
					foreach (var a in arms)
					{
						if (a.Weapon.ValidTargets.Contains("Air"))
						{
							missileUnitsCount++;
							break;
						}
					}
				}
			}

			return missileUnitsCount;
		}

		protected static Actor FindDefenselessTarget(Squad owner)
		{
			Actor target = null;
			FindSafePlace(owner, out target, true);
			return target;
		}

		protected static CPos? FindSafePlace(Squad owner, out Actor detectedEnemyTarget, bool needTarget)
		{
			var world = owner.world;
			detectedEnemyTarget = null;
			var x = (world.Map.MapSize.X % DangerRadius) == 0 ? world.Map.MapSize.X : world.Map.MapSize.X + DangerRadius;
			var y = (world.Map.MapSize.Y % DangerRadius) == 0 ? world.Map.MapSize.Y : world.Map.MapSize.Y + DangerRadius;

			for (var i = 0; i < x; i += DangerRadius * 2)
			{
				for (var j = 0; j < y; j += DangerRadius * 2)
				{
					var pos = new CPos(i, j);
					if (NearToPosSafely(owner, pos.CenterPosition, out detectedEnemyTarget))
					{
						if (needTarget && detectedEnemyTarget == null)
							continue;

						return pos;
					}
				}
			}

			return null;
		}

		protected static bool NearToPosSafely(Squad owner, WPos loc)
		{
			Actor a;
			return NearToPosSafely(owner, loc, out a);
		}

		protected static bool NearToPosSafely(Squad owner, WPos loc, out Actor detectedEnemyTarget)
		{
			detectedEnemyTarget = null;
			var unitsAroundPos = owner.world.FindActorsInCircle(loc, WRange.FromCells(DangerRadius))
				.Where(unit => owner.bot.p.Stances[unit.Owner] == Stance.Enemy).ToList();

			if (!unitsAroundPos.Any())
				return true;

			if (CountAntiAirUnits(unitsAroundPos) * MissileUnitMultiplier < owner.units.Count)
			{
				detectedEnemyTarget = unitsAroundPos.Random(owner.random);
				return true;
			}

			return false;
		}

		protected static bool FullAmmo(Actor a)
		{
			var limitedAmmo = a.TraitOrDefault<LimitedAmmo>();
			return limitedAmmo != null && limitedAmmo.FullAmmo();
		}

		protected static bool HasAmmo(Actor a)
		{
			var limitedAmmo = a.TraitOrDefault<LimitedAmmo>();
			return limitedAmmo != null && limitedAmmo.HasAmmo();
		}

		protected static bool ReloadsAutomatically(Actor a)
		{
			return a.HasTrait<Reloads>();
		}

		protected static bool IsRearm(Actor a)
		{
			var activity = a.GetCurrentActivity();
			if (activity == null)
				return false;

			var type = activity.GetType();
			if (type == typeof(Rearm) || type == typeof(ResupplyAircraft))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();
			if (nextType == typeof(Rearm) || nextType == typeof(ResupplyAircraft))
				return true;

			return false;
		}

		// Checks the number of anti air enemies around units
		protected virtual bool ShouldFlee(Squad owner)
		{
			return base.ShouldFlee(owner, enemies => CountAntiAirUnits(enemies) * MissileUnitMultiplier > owner.units.Count);
		}
	}

	class AirIdleState : AirStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.fsm.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			var e = FindDefenselessTarget(owner);
			if (e == null)
				return;

			owner.Target = e;
			owner.fsm.ChangeState(owner, new AirAttackState(), true);
		}

		public void Deactivate(Squad owner) { }
	}

	class AirAttackState : AirStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.TargetIsValid)
			{
				var a = owner.units.Random(owner.random);
				var closestEnemy = owner.bot.FindClosestEnemy(a.CenterPosition);
				if (closestEnemy != null)
					owner.Target = closestEnemy;
				else
				{
					owner.fsm.ChangeState(owner, new AirFleeState(), true);
					return;
				}
			}

			if (!NearToPosSafely(owner, owner.Target.CenterPosition))
			{
				owner.fsm.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			foreach (var a in owner.units)
			{
				if (BusyAttack(a))
					continue;

				if (!ReloadsAutomatically(a))
				{
					if (!HasAmmo(a))
					{
						if (IsRearm(a))
							continue;
						owner.world.IssueOrder(new Order("ReturnToBase", a, false));
						continue;
					}

					if (IsRearm(a))
						continue;
				}

				if (owner.Target.HasTrait<ITargetable>() && CanAttackTarget(a, owner.Target))
					owner.world.IssueOrder(new Order("Attack", a, false) { TargetActor = owner.Target });
			}
		}

		public void Deactivate(Squad owner) { }
	}

	class AirFleeState : AirStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			foreach (var a in owner.units)
			{
				if (!ReloadsAutomatically(a) && !FullAmmo(a))
				{
					if (IsRearm(a))
						continue;

					owner.world.IssueOrder(new Order("ReturnToBase", a, false));
					continue;
				}

				owner.world.IssueOrder(new Order("Move", a, false) { TargetLocation = RandomBuildingLocation(owner) });
			}

			owner.fsm.ChangeState(owner, new AirIdleState(), true);
		}

		public void Deactivate(Squad owner) { }
	}
}
