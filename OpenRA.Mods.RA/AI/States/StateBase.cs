#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	abstract class StateBase
	{
		protected const int DangerRadius = 10;

		protected static void GoToRandomOwnBuilding(Squad squad)
		{
			var loc = RandomBuildingLocation(squad);
			foreach (var a in squad.units)
				squad.world.IssueOrder(new Order("Move", a, false) { TargetLocation = loc });
		}

		protected static CPos RandomBuildingLocation(Squad squad)
		{
			var location = squad.bot.baseCenter;
			var buildings = squad.world.ActorsWithTrait<Building>()
				.Where(a => a.Actor.Owner == squad.bot.p).Select(a => a.Actor).ToArray();
			if (buildings.Length > 0)
				location = buildings.Random(squad.random).Location;
			return location;
		}

		protected static bool BusyAttack(Actor a)
		{
			if (a.Flagged(ActorFlag.Idle))
				return false;

			var type = a.GetCurrentActivity().GetType();
			if (type == typeof(Attack) || type == typeof(FlyAttack))
				return true;

			var next = a.GetCurrentActivity().NextActivity;
			if (next == null)
				return false;

			var nextType = a.GetCurrentActivity().NextActivity.GetType();
			if (nextType == typeof(Attack) || nextType == typeof(FlyAttack))
				return true;

			return false;
		}

		protected static bool CanAttackTarget(Actor a, Actor target)
		{
			if (!a.HasTrait<AttackBase>())
				return false;

			var targetable = target.TraitOrDefault<ITargetable>();
			if (targetable == null)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
				if (arm.Weapon.ValidTargets.Intersect(targetable.TargetTypes).Any())
					return true;

			return false;
		}

		protected virtual bool ShouldFlee(Squad squad, Func<IEnumerable<Actor>, bool> flee)
		{
			if (!squad.IsValid)
				return false;

			var u = squad.units.Random(squad.random);
			var units = squad.world.FindActorsInCircle(u.CenterPosition, WRange.FromCells(DangerRadius)).ToList();
			var ownBaseBuildingAround = units.Where(unit => unit.Owner == squad.bot.p && unit.HasTrait<Building>());
			if (ownBaseBuildingAround.Any())
				return false;

			var enemyAroundUnit = units.Where(unit => squad.bot.p.Stances[unit.Owner] == Stance.Enemy && unit.HasTrait<AttackBase>());
			if (!enemyAroundUnit.Any())
				return false;

			return flee(enemyAroundUnit);
		}
	}
}
