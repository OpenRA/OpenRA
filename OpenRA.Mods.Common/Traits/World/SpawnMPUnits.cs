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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawn base actor at the spawnpoint and support units in an annulus around the base actor. Both are defined at MPStartUnits. Attach this to the world actor.")]
	public class SpawnMPUnitsInfo : ITraitInfo, Requires<MPStartLocationsInfo>, Requires<MPStartUnitsInfo>, ILobbyOptions
	{
		public readonly string StartingUnitsClass = "none";

		[Desc("Prevent the starting units option from being changed in the lobby.")]
		public bool Locked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var startingUnits = new Dictionary<string, string>();

			// Duplicate classes are defined for different race variants
			foreach (var t in rules.Actors["world"].TraitInfos<MPStartUnitsInfo>())
				startingUnits[t.Class] = t.ClassName;

			if (startingUnits.Any())
				yield return new LobbyOption("startingunits", "Starting Units", new ReadOnlyDictionary<string, string>(startingUnits), StartingUnitsClass, Locked);
		}

		public object Create(ActorInitializer init) { return new SpawnMPUnits(this); }
	}

	public class SpawnMPUnits : IWorldLoaded
	{
		readonly SpawnMPUnitsInfo info;

		public SpawnMPUnits(SpawnMPUnitsInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var s in world.WorldActor.Trait<MPStartLocations>().Start)
				SpawnUnitsForPlayer(world, s.Key, s.Value);
		}

		void SpawnUnitsForPlayer(World w, Player p, CPos sp)
		{
			var spawnClass = p.PlayerReference.StartingUnitsClass ?? w.LobbyInfo.GlobalSettings
				.OptionOrDefault("startingunits", info.StartingUnitsClass);

			var unitGroup = w.Map.Rules.Actors["world"].TraitInfos<MPStartUnitsInfo>()
				.Where(g => g.Class == spawnClass && g.Factions != null && g.Factions.Contains(p.Faction.InternalName))
				.RandomOrDefault(w.SharedRandom);

			if (unitGroup == null)
				throw new InvalidOperationException("No starting units defined for faction {0} with class {1}".F(p.Faction.InternalName, spawnClass));

			if (unitGroup.BaseActor != null)
			{
				w.CreateActor(unitGroup.BaseActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(sp),
					new OwnerInit(p),
					new SkipMakeAnimsInit(),
					new FacingInit(128),
				});
			}

			if (!unitGroup.SupportActors.Any())
				return;

			var supportSpawnCells = w.Map.FindTilesInAnnulus(sp, unitGroup.InnerSupportRadius + 1, unitGroup.OuterSupportRadius);

			foreach (var s in unitGroup.SupportActors)
			{
				var mi = w.Map.Rules.Actors[s.ToLowerInvariant()].TraitInfo<MobileInfo>();
				var validCells = supportSpawnCells.Where(c => mi.CanEnterCell(w, null, c));
				if (!validCells.Any())
					throw new InvalidOperationException("No cells available to spawn starting unit {0}".F(s));

				var cell = validCells.Random(w.SharedRandom);
				var subCell = mi.SharesCell ? w.ActorMap.FreeSubCell(cell) : 0;

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
