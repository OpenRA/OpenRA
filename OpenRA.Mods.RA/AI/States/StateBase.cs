#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	//**********************************************************************************
	// Squad AI States

	/* Include general functional for all states */

	abstract class StateBase
	{
		protected const int dangerRadius = 10;

		protected virtual bool MayBeFlee(Squad owner, Func<List<Actor>, bool> flee)
		{
			if (owner.IsEmpty) return false;
			var u = owner.units.Random(owner.random);

			var units = owner.world.FindActorsInCircle(u.CenterPosition, WRange.FromCells(dangerRadius)).ToList();
			var ownBaseBuildingAround = units.Where(unit => unit.Owner == owner.bot.p && unit.HasTrait<Building>()).ToList();
			if (ownBaseBuildingAround.Count > 0) return false;

			var enemyAroundUnit = units.Where(unit => owner.bot.p.Stances[unit.Owner] == Stance.Enemy && unit.HasTrait<AttackBase>()).ToList();
			if (!enemyAroundUnit.Any()) return false;

			return flee(enemyAroundUnit);
		}

		protected static CPos? AverageUnitsPosition(List<Actor> units)
		{
			int x = 0;
			int y = 0;
			int countUnits = 0;
			foreach (var u in units)
			{
				x += u.Location.X;
				y += u.Location.Y;
				countUnits++;
			}
			x = x / countUnits;
			y = y / countUnits;
			return (x != 0 && y != 0) ? new CPos?(new CPos(x, y)) : null;
		}

		protected static void GoToRandomOwnBuilding(Squad owner)
		{
			var loc = RandomBuildingLocation(owner);
			foreach (var a in owner.units)
				owner.world.IssueOrder(new Order("Move", a, false) { TargetLocation = loc });
		}

		protected static CPos RandomBuildingLocation(Squad owner)
		{
			var location = owner.bot.baseCenter;
			var buildings = owner.world.ActorsWithTrait<Building>()
				.Where(a => a.Actor.Owner == owner.bot.p).Select(a => a.Actor).ToArray();
			if (buildings.Length > 0)
				location = buildings.Random(owner.random).Location;
			return location;
		}

		protected static bool BusyAttack(Actor a)
		{
			if (!a.IsIdle)
				if (a.GetCurrentActivity().GetType() == typeof(OpenRA.Mods.RA.Activities.Attack) ||
					a.GetCurrentActivity().GetType() == typeof(FlyAttack) ||
					(a.GetCurrentActivity().NextActivity != null &&
					(a.GetCurrentActivity().NextActivity.GetType() == typeof(OpenRA.Mods.RA.Activities.Attack) || 
					a.GetCurrentActivity().NextActivity.GetType() == typeof(FlyAttack)) )
					)
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
				if (arm.Weapon.ValidTargets.Intersect(targetable.TargetTypes) != null)
					return true;

			return false;
		}
	}
}
