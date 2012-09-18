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
		public static IEnumerable<Actor> UnitsNearLocation(this World world, PPos location, int range)
		{
			return world.FindUnitsInCircle(location, Game.CellSize * range)
				.Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && !a.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> BuildingsNearLocation(this World world, PPos location, int range)
		{
			return UnitsNearLocation(world, location, range).Where(a => a.HasTrait<Building>() && !a.HasTrait<Wall>());
		}

		public static IEnumerable<Actor> ForcesNearLocation(this World world, PPos location, int range)
		{
			return UnitsNearLocation(world, location, range).Where(a => a.HasTrait<IMove>());
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

		public static Actor InsertUnitWithChinook(World world, Player owner, string unitName, CPos entry, CPos lz, CPos exit, Action<Actor> afterUnload)
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
			return unit;
		}
	}
}
