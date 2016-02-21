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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	abstract class AirStateBase : StateBase
	{
		static readonly string[] AirTargetTypes = new[] { "Air" };

		protected const int MissileUnitMultiplier = 3;

		protected static int CountAntiAirUnits(IEnumerable<Actor> units)
		{
			if (!units.Any())
				return 0;

			var missileUnitsCount = 0;
			foreach (var unit in units)
			{
				if (unit != null && unit.Info.HasTraitInfo<AttackBaseInfo>() && !unit.Info.HasTraitInfo<AircraftInfo>()
					&& !unit.IsDisabled())
				{
					var arms = unit.TraitsImplementing<Armament>();
					foreach (var a in arms)
					{
						if (a.Weapon.IsValidTarget(AirTargetTypes))
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
			var map = owner.World.Map;
			detectedEnemyTarget = null;
			var x = (map.MapSize.X % DangerRadius) == 0 ? map.MapSize.X : map.MapSize.X + DangerRadius;
			var y = (map.MapSize.Y % DangerRadius) == 0 ? map.MapSize.Y : map.MapSize.Y + DangerRadius;

			for (var i = 0; i < x; i += DangerRadius * 2)
			{
				for (var j = 0; j < y; j += DangerRadius * 2)
				{
					var pos = new CPos(i, j);
					if (NearToPosSafely(owner, map.CenterOfCell(pos), out detectedEnemyTarget))
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
			var unitsAroundPos = owner.World.FindActorsInCircle(loc, WDist.FromCells(DangerRadius))
				.Where(unit => owner.Bot.Player.Stances[unit.Owner] == Stance.Enemy).ToList();

			if (!unitsAroundPos.Any())
				return true;

			if (CountAntiAirUnits(unitsAroundPos) * MissileUnitMultiplier < owner.Units.Count)
			{
				detectedEnemyTarget = unitsAroundPos.Random(owner.Random);
				return true;
			}

			return false;
		}

		protected static bool FullAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.FullAmmo());
		}

		protected static bool HasAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.HasAmmo());
		}

		protected static bool ReloadsAutomatically(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.Info.SelfReloads);
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
			return base.ShouldFlee(owner, enemies => CountAntiAirUnits(enemies) * MissileUnitMultiplier > owner.Units.Count);
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
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			var e = FindDefenselessTarget(owner);
			if (e == null)
				return;

			owner.TargetActor = e;
			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackState(), true);
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

			if (!owner.IsTargetValid)
			{
				var a = owner.Units.Random(owner.Random);
				var closestEnemy = owner.Bot.FindClosestEnemy(a.CenterPosition);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
					return;
				}
			}

			if (!NearToPosSafely(owner, owner.TargetActor.CenterPosition))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			foreach (var a in owner.Units)
			{
				if (BusyAttack(a))
					continue;

				if (!ReloadsAutomatically(a))
				{
					if (!HasAmmo(a))
					{
						if (IsRearm(a))
							continue;
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
						continue;
					}

					if (IsRearm(a))
						continue;
				}

				if (owner.TargetActor.Info.HasTraitInfo<ITargetableInfo>() && CanAttackTarget(a, owner.TargetActor))
					owner.Bot.QueueOrder(new Order("Attack", a, false) { TargetActor = owner.TargetActor });
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

			foreach (var a in owner.Units)
			{
				if (!ReloadsAutomatically(a) && !FullAmmo(a))
				{
					if (IsRearm(a))
						continue;

					owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
					continue;
				}

				owner.Bot.QueueOrder(new Order("Move", a, false) { TargetLocation = RandomBuildingLocation(owner) });
			}

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleState(), true);
		}

		public void Deactivate(Squad owner) { }
	}
}
