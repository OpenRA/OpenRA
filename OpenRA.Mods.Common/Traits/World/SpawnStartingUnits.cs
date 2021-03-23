#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class SpawnStartingUnitsInfo : TraitInfo, Requires<StartingUnitsInfo>, ILobbyOptions
	{
		public readonly string StartingUnitsClass = "none";

		[Desc("Descriptive label for the starting units option in the lobby.")]
		public readonly string DropdownLabel = "Starting Units";

		[Desc("Tooltip description for the starting units option in the lobby.")]
		public readonly string DropdownDescription = "Change the units that you start the game with";

		[Desc("Prevent the starting units option from being changed in the lobby.")]
		public readonly bool DropdownLocked = false;

		[Desc("Whether to display the starting units option in the lobby.")]
		public readonly bool DropdownVisible = true;

		[Desc("Display order for the starting units option in the lobby.")]
		public readonly int DropdownDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var startingUnits = new Dictionary<string, string>();

			// Duplicate classes are defined for different race variants
			foreach (var t in rules.Actors["world"].TraitInfos<StartingUnitsInfo>())
				startingUnits[t.Class] = t.ClassName;

			if (startingUnits.Any())
				yield return new LobbyOption("startingunits", DropdownLabel, DropdownDescription, DropdownVisible, DropdownDisplayOrder,
					new ReadOnlyDictionary<string, string>(startingUnits), StartingUnitsClass, DropdownLocked);
		}

		public override object Create(ActorInitializer init) { return new SpawnStartingUnits(this); }
	}

	public class SpawnStartingUnits : IWorldLoaded
	{
		readonly SpawnStartingUnitsInfo info;

		public SpawnStartingUnits(SpawnStartingUnitsInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var p in world.Players)
				if (p.Playable)
					SpawnUnitsForPlayer(world, p);
		}

		void SpawnUnitsForPlayer(World w, Player p)
		{
			var spawnClass = p.PlayerReference.StartingUnitsClass ?? w.LobbyInfo.GlobalSettings
				.OptionOrDefault("startingunits", info.StartingUnitsClass);

			var unitGroup = w.Map.Rules.Actors["world"].TraitInfos<StartingUnitsInfo>()
				.Where(g => g.Class == spawnClass && g.Factions != null && g.Factions.Contains(p.Faction.InternalName))
				.RandomOrDefault(w.SharedRandom);

			if (unitGroup == null)
				throw new InvalidOperationException("No starting units defined for faction {0} with class {1}".F(p.Faction.InternalName, spawnClass));

			if (unitGroup.BaseActor != null)
			{
				var facing = unitGroup.BaseActorFacing.HasValue ? unitGroup.BaseActorFacing.Value : new WAngle(w.SharedRandom.Next(1024));
				w.CreateActor(unitGroup.BaseActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(p.HomeLocation + unitGroup.BaseActorOffset),
					new OwnerInit(p),
					new SkipMakeAnimsInit(),
					new FacingInit(facing),
				});
			}

			if (!unitGroup.SupportActors.Any())
				return;

			var supportSpawnCells = w.Map.FindTilesInAnnulus(p.HomeLocation, unitGroup.InnerSupportRadius + 1, unitGroup.OuterSupportRadius);

			foreach (var s in unitGroup.SupportActors)
			{
				var actorRules = w.Map.Rules.Actors[s.ToLowerInvariant()];
				var ip = actorRules.TraitInfo<IPositionableInfo>();
				var validCell = supportSpawnCells.Shuffle(w.SharedRandom).FirstOrDefault(c => ip.CanEnterCell(w, null, c));

				if (validCell == CPos.Zero)
				{
					Log.Write("debug", "No cells available to spawn starting unit {0} for player {1}".F(s, p));
					continue;
				}

				var subCell = ip.SharesCell ? w.ActorMap.FreeSubCell(validCell) : 0;
				var facing = unitGroup.SupportActorsFacing.HasValue ? unitGroup.SupportActorsFacing.Value : new WAngle(w.SharedRandom.Next(1024));

				w.CreateActor(s.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit(p),
					new LocationInit(validCell),
					new SubCellInit(subCell),
					new FacingInit(facing),
				});
			}
		}
	}
}
