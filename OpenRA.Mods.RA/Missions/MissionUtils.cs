#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using System.Drawing;

namespace OpenRA.Mods.RA.Missions
{
	public static class MissionUtils
	{
		public static IEnumerable<Actor> FindAliveCombatantActorsInCircle(this World world, PPos location, int range)
		{
			return world.FindUnitsInCircle(location, Game.CellSize * range)
				.Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && !a.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> FindAliveNonCombatantActorsInCircle(this World world, PPos location, int range)
		{
			return world.FindUnitsInCircle(location, Game.CellSize * range)
				.Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && a.Owner.NonCombatant);
		}

		public static Actor ExtractUnitWithChinook(World world, Player owner, Actor unit, CPos entry, CPos lz, CPos exit)
		{
			var chinook = world.CreateActor("tran", new TypeDictionary { new OwnerInit(owner), new LocationInit(entry) });
			chinook.QueueActivity(new HeliFly(Util.CenterOfCell(lz)));
			chinook.QueueActivity(new Turn(0));
			chinook.QueueActivity(new HeliLand(true, 0));
			chinook.QueueActivity(new WaitFor(() => chinook.Trait<Cargo>().Passengers.Contains(unit)));
			chinook.QueueActivity(new Wait(150));
			chinook.QueueActivity(new HeliFly(Util.CenterOfCell(exit)));
			chinook.QueueActivity(new RemoveSelf());
			return chinook;
		}

		public static Pair<Actor, Actor> InsertUnitWithChinook(World world, Player owner, string unitName, CPos entry, CPos lz, CPos exit, Action<Actor> afterUnload)
		{
			var unit = world.CreateActor(false, unitName, new TypeDictionary { new OwnerInit(owner) });
			var chinook = world.CreateActor("tran", new TypeDictionary { new OwnerInit(owner), new LocationInit(entry) });
			chinook.Trait<Cargo>().Load(chinook, unit);
			chinook.QueueActivity(new HeliFly(Util.CenterOfCell(lz)));
			chinook.QueueActivity(new Turn(0));
			chinook.QueueActivity(new HeliLand(true, 0));
			chinook.QueueActivity(new UnloadCargo(true));
			chinook.QueueActivity(new CallFunc(() => afterUnload(unit)));
			chinook.QueueActivity(new Wait(150));
			chinook.QueueActivity(new HeliFly(Util.CenterOfCell(exit)));
			chinook.QueueActivity(new RemoveSelf());
			return Pair.New(chinook, unit);
		}

		public static bool AreaSecuredWithUnits(World world, Player player, PPos location, int range)
		{
			var units = world.FindAliveCombatantActorsInCircle(location, range).Where(a => a.HasTrait<IMove>());
			return units.Any() && units.All(a => a.Owner == player);
		}

		public static Actor ClosestPlayerUnit(World world, Player player, PPos location, int range)
		{
			return world.FindAliveCombatantActorsInCircle(location, range)
				.Where(a => a.Owner == player && a.HasTrait<IMove>())
				.OrderBy(a => (location - a.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		public static Actor ClosestPlayerBuilding(World world, Player player, PPos location, int range)
		{
			return world.FindAliveCombatantActorsInCircle(location, range)
				.Where(a => a.Owner == player && a.HasTrait<Building>() && !a.HasTrait<Wall>())
				.OrderBy(a => (location - a.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		public static IEnumerable<ProductionQueue> FindQueues(World world, Player player, string category)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category)
				.Select(a => a.Trait);
		}

		public static T AddFlag<T>(T flags, T flag)
		{
			var fs = Convert.ToInt32(flags);
			var f = Convert.ToInt32(flag);
			return (T)(object)(fs | f);
		}

		public static T RemoveFlag<T>(T flags, T flag)
		{
			var fs = Convert.ToInt32(flags);
			var f = Convert.ToInt32(flag);
			return (T)(object)(fs & ~f);
		}

		public static bool HasFlag<T>(T flags, T flag)
		{
			var fs = Convert.ToInt32(flags);
			var f = Convert.ToInt32(flag);
			return (fs & f) == f;
		}
	}
}
