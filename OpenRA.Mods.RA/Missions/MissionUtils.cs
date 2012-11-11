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

namespace OpenRA.Mods.RA.Missions
{
	public static class MissionUtils
	{
		public static IEnumerable<Actor> FindAliveCombatantActorsInCircle(this World world, PPos location, int range)
		{
			return world.FindUnitsInCircle(location, Game.CellSize * range)
				.Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && !u.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> FindAliveCombatantActorsInBox(this World world, PPos a, PPos b)
		{
			return world.FindUnits(a, b).Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && !u.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> FindAliveNonCombatantActorsInCircle(this World world, PPos location, int range)
		{
			return world.FindUnitsInCircle(location, Game.CellSize * range)
				.Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && u.Owner.NonCombatant);
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

		public static void Paradrop(World world, Player owner, IEnumerable<string> units, CPos entry, CPos location)
		{
			var badger = world.CreateActor("badr", new TypeDictionary
			{
				new LocationInit(entry),
				new OwnerInit(owner),
				new FacingInit(Util.GetFacing(location - entry, 0)),
				new AltitudeInit(Rules.Info["badr"].Traits.Get<PlaneInfo>().CruiseAltitude),
			});
			badger.QueueActivity(new FlyAttack(Target.FromCell(location)));
			badger.Trait<ParaDrop>().SetLZ(location);
			var cargo = badger.Trait<Cargo>();
			foreach (var unit in units)
			{
				cargo.Load(badger, world.CreateActor(false, unit, new TypeDictionary { new OwnerInit(owner) }));
			}
		}

		public static void Parabomb(World world, Player owner, CPos entry, CPos location)
		{
			var badger = world.CreateActor("badr.bomber", new TypeDictionary
			{
				new LocationInit(entry),
				new OwnerInit(owner),
				new FacingInit(Util.GetFacing(location - entry, 0)),
				new AltitudeInit(Rules.Info["badr.bomber"].Traits.Get<PlaneInfo>().CruiseAltitude),
			});
			badger.Trait<CarpetBomb>().SetTarget(location);
			badger.QueueActivity(Fly.ToCell(location));
			badger.QueueActivity(new FlyOffMap());
			badger.QueueActivity(new RemoveSelf());
		}

		public static Actor FirstUnshroudedOrDefault(this IEnumerable<Actor> actors, World world, int shroudRange)
		{
			return actors.FirstOrDefault(u => world.FindAliveCombatantActorsInCircle(u.CenterLocation, shroudRange).All(a => !a.HasTrait<CreatesShroud>()));
		}

		public static bool AreaSecuredWithUnits(World world, Player player, PPos location, int range)
		{
			var units = world.FindAliveCombatantActorsInCircle(location, range).Where(a => a.HasTrait<IMove>());
			return units.Any() && units.All(a => a.Owner == player);
		}

		public static Actor ClosestPlayerUnit(World world, Player player, PPos location, int range)
		{
			return ClosestPlayerUnits(world, player, location, range).FirstOrDefault();
		}

		public static IEnumerable<Actor> ClosestPlayerUnits(World world, Player player, PPos location, int range)
		{
			return world.FindAliveCombatantActorsInCircle(location, range)
				.Where(a => a.Owner == player && a.HasTrait<IMove>())
				.OrderBy(a => (location - a.CenterLocation).LengthSquared);
		}

		public static Actor ClosestPlayerBuilding(World world, Player player, PPos location, int range)
		{
			return ClosestPlayerBuildings(world, player, location, range).FirstOrDefault();
		}

		public static IEnumerable<Actor> ClosestPlayerBuildings(World world, Player player, PPos location, int range)
		{
			return world.FindAliveCombatantActorsInCircle(location, range)
				.Where(a => a.Owner == player && a.HasTrait<Building>() && !a.HasTrait<Wall>())
				.OrderBy(a => (location - a.CenterLocation).LengthSquared);
		}

		public static IEnumerable<ProductionQueue> FindQueues(World world, Player player, string category)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category)
				.Select(a => a.Trait);
		}

		public static Actor UnitContaining(this World world, Actor actor)
		{
			return world.Actors.FirstOrDefault(a => a.HasTrait<Cargo>() && a.Trait<Cargo>().Passengers.Contains(actor));
		}
	}
}
