﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SpawnMPUnitsInfo : TraitInfo<SpawnMPUnits>, Requires<MPStartLocationsInfo>, Requires<MPStartUnitsInfo> { }

	public class SpawnMPUnits : IWorldLoaded
	{
		public void WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var s in world.WorldActor.Trait<MPStartLocations>().Start)
				SpawnUnitsForPlayer(world, s.Key, s.Value);
		}

		void SpawnUnitsForPlayer(World w, Player p, CPos sp)
		{
			var spawnClass = p.PlayerReference.StartingUnitsClass ?? w.LobbyInfo.GlobalSettings.StartingUnitsClass;
			var unitGroup = Rules.Info["world"].Traits.WithInterface<MPStartUnitsInfo>()
				.Where(g => g.Class == spawnClass && g.Races != null && g.Races.Contains(p.Country.Race))
				.RandomOrDefault(w.SharedRandom);

			if (unitGroup == null)
				throw new InvalidOperationException("No starting units defined for country {0} with class {1}".F(p.Country.Race, spawnClass));

			// Spawn base actor at the spawnpoint
			if (unitGroup.BaseActor != null)
			{
				w.CreateActor(unitGroup.BaseActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(sp),
					new OwnerInit(p),
					new SkipMakeAnimsInit(),
				});
			}

			if (!unitGroup.SupportActors.Any())
				return;

			// Spawn support units in an annulus around the base actor
			var supportSpawnCells = w.FindTilesInCircle(sp, unitGroup.OuterSupportRadius)
				.Except(w.FindTilesInCircle(sp, unitGroup.InnerSupportRadius));

			foreach (var s in unitGroup.SupportActors)
			{
				var mi = Rules.Info[s.ToLowerInvariant()].Traits.Get<MobileInfo>();
				var validCells = supportSpawnCells.Where(c => mi.CanEnterCell(w, c));
				if (!validCells.Any())
					throw new InvalidOperationException("No cells available to spawn starting unit {0}".F(s));

				var cell = validCells.Random(w.SharedRandom);
				var subCell = w.ActorMap.FreeSubCell(cell).Value;

				w.CreateActor(s.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit(p),
					new LocationInit(cell),
					new SubCellInit(subCell),
					new FacingInit(w.SharedRandom.Next(256))
				});
			}
		}
	}
}
