#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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

		protected static List<Actor> ScanEnemyUnits(Squad owner, int maxCount)
		{
			// Use random sampling. Search from near to far.
			var loc = owner.CenterLocation;
			var any = owner.Units.First();
			List<Actor> sampledCandidates;

			var cands = owner.World.FindActorsInCircle(owner.CenterPosition, WDist.FromCells(20))
			          .Where(a => a.AppearsHostileTo(any));
			if (!cands.Any())
				cands = owner.World.ActorsHavingTrait<IOccupySpace>().Where(a => a.AppearsHostileTo(any));

			// Find units from near.
			if (cands.Count() < maxCount)
			{
				sampledCandidates = cands.ToList();
			}
			else
			{
				sampledCandidates = new List<Actor>();
				for (int i = 0; i < 20; i++)
					sampledCandidates.Add(cands.Random(owner.Bot.Random));
			}

			// Sort them by distance.
			return sampledCandidates.OrderBy(o => (o.Location - loc).LengthSquared).ToList();
		}

		protected static CPos? FindSafePlace(Squad owner, out Actor detectedEnemyTarget, bool needTarget)
		{
			foreach (var cand in ScanEnemyUnits(owner, 20))
			{
				if (NearToPosSafely(owner, cand.CenterPosition))
				{
					detectedEnemyTarget = cand;
					return cand.Location;
				}
			}

			detectedEnemyTarget = null;
			return null;

			/*
			// do old full search

			detectedEnemyTarget = null;
			var x = (map.MapSize.X % DangerRadius) == 0 ? map.MapSize.X : map.MapSize.X + DangerRadius;
			var y = (map.MapSize.Y % DangerRadius) == 0 ? map.MapSize.Y : map.MapSize.Y + DangerRadius;

			for (var i = 0; i < x; i += DangerRadius * 2)
			{
				for (var j = 0; j < y; j += DangerRadius * 2)
				{
					var pos2 = new CPos(i, j);
					if (NearToPosSafely(owner, map.CenterOfCell(pos2), out detectedEnemyTarget))
					{
						if (needTarget && detectedEnemyTarget == null)
							continue;

						return pos2;
					}
				}
			}

			return null;
			*/
		}

		public static bool NearToPosSafely(Squad owner, WPos loc)
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
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
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
			var aas = EnemyStaticAAs(owner);
			if (aas.Any(a => (a.Location - owner.CenterLocation).LengthSquared < 144))
				return true;
			return base.ShouldFlee(owner, enemies => CountAntiAirUnits(enemies) * MissileUnitMultiplier > owner.Units.Count);
		}

		protected IEnumerable<Actor> EnemyStaticAAs(Squad owner)
		{
			var anyUnit = owner.Units.First();
			return owner.World.ActorsHavingTrait<Building>().Where(b => // from buildings,
				b.AppearsHostileTo(anyUnit) && // enemy building and
				owner.Bot.Info.BuildingCommonNames.StaticAntiAir.Contains(b.Info.Name)); // registered in Static AA.
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

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

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
						Flee(owner, a);
						continue;
					}

					if (IsRearm(a))
						continue;
				}

				if (owner.TargetActor.Info.HasTraitInfo<ITargetableInfo>() && CanAttackTarget(a, owner.TargetActor))
					owner.Bot.QueueOrder(new Order("Attack", a, false) { TargetActor = owner.TargetActor });
			}
		}

		void Flee(Squad owner, Actor a)
		{
			var safePoint = AirFleeState.CalcSafePoint(owner, owner.CenterLocation, EnemyStaticAAs(owner));
			owner.Bot.QueueOrder(new Order("Move", a, false) { TargetLocation = safePoint });
			owner.Bot.QueueOrder(new Order("ReturnToBase", a, true));
		}

		public void Deactivate(Squad owner) { }
	}

	class AirFleeState : AirStateBase, IState
	{
		CPos escapeDest;
		CPos safePoint; // to move to escapeDest safely, we take a detour to this point.

		public void Activate(Squad owner)
		{
			escapeDest = RandomBuildingLocation(owner);
			safePoint = CalcSafePoint(owner, escapeDest, EnemyStaticAAs(owner));
		}

		public static CPos CalcSafePoint(Squad owner, CPos dest, IEnumerable<Actor> enemyStaticAAs)
		{
			if (!owner.World.Map.Contains(owner.CenterLocation))
				return RandomBuildingLocation(owner);

			CPos currentPos = owner.CenterLocation;
			List<CPos> cands = new List<CPos>();
			cands.Add(dest); // direct movement

			var within20 = owner.World.Map.FindTilesInAnnulus(dest, 2, 20);
			if (within20.Any())
				for (int i = 0; i < 32; i++)
					cands.Add(within20.Random(owner.Bot.Random));

			int best_score = -1;
			CPos best = CPos.Zero;
			foreach (var cand in cands)
			{
				var score = EvaluateSafePoint(currentPos, cand, enemyStaticAAs);
				if (best_score == -1 || score < best_score)
				{
					best = cand;
					best_score = score;
				}
			}

			System.Diagnostics.Debug.Assert(owner.World.Map.Contains(best), "What? Unit out of map?");
			return best;
		}

		static int EvaluateSafePoint(CPos safePoint, CPos currentPos, IEnumerable<Actor> enemyStaticAAs)
		{
			// Already there. No need to move, no threat
			if (safePoint == currentPos)
				return 0;

			var x1 = safePoint.X;
			var x2 = currentPos.X;
			var y1 = safePoint.Y;
			var y2 = currentPos.Y;

			var a = y1 - y2;
			var b = x2 - x1;
			var c = -y1 * (x2 - x1) + x2 * (y2 - y1);
			var denom = (new CVec(a, b)).Length;

			int score = 0;

			foreach (var aa in enemyStaticAAs)
			{
				score += Math.Abs(a * aa.Location.X + b * aa.Location.Y + c) / denom;
			}

			return score;
		}

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

					owner.Bot.QueueOrder(new Order("Move", a, false) { TargetLocation = safePoint });
					owner.Bot.QueueOrder(new Order("ReturnToBase", a, true));
					continue;
				}

				owner.Bot.QueueOrder(new Order("Move", a, false) { TargetLocation = safePoint });
				owner.Bot.QueueOrder(new Order("Move", a, true) { TargetLocation = escapeDest });
			}

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleState(), true);
		}

		public void Deactivate(Squad owner) { }
	}
}
