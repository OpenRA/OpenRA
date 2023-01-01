#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckPlayers : ILintMapPass, ILintServerMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			var players = new MapPlayers(map.PlayerDefinitions);
			var spawns = new List<CPos>();
			foreach (var kv in map.ActorDefinitions.Where(d => d.Value.Value == "mpspawn"))
			{
				var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				spawns.Add(s.Get<LocationInit>().Value);
			}

			Run(emitError, emitWarning, players, map.Visibility, map.Rules.Actors[SystemActors.World], spawns.ToArray());
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, emitWarning, map.Players, map.Visibility, map.WorldActorInfo, map.SpawnPoints);
		}

		void Run(Action<string> emitError, Action<string> emitWarning, MapPlayers players, MapVisibility visibility, ActorInfo worldActorInfo, CPos[] spawnPoints)
		{
			if (players.Players.Count > 64)
				emitError("Defining more than 64 players is not allowed.");

			var playablePlayerFound = false;
			var worldOwnerFound = false;
			var playerNames = players.Players.Values.Select(p => p.Name).ToHashSet();
			foreach (var player in players.Players.Values)
			{
				foreach (var ally in player.Allies)
					if (!playerNames.Contains(ally))
						emitError($"Allies contains player {ally} that is not in list.");

				foreach (var enemy in player.Enemies)
					if (!playerNames.Contains(enemy))
						emitError($"Enemies contains player {enemy} that is not in list.");

				if (player.Playable)
					playablePlayerFound = true;

				if (player.OwnsWorld)
				{
					worldOwnerFound = true;
					if (player.Enemies.Length > 0 || player.Allies.Length > 0)
						emitWarning($"The player {player.Name} owning the world should not have any allies or enemies.");

					if (player.Playable)
						emitError($"The player {player.Name} owning the world can't be playable.");
				}
				else if (visibility == MapVisibility.MissionSelector && player.Playable && !player.LockFaction)
				{
					// Missions must lock the faction of the player to force the server to override the default Random faction
					emitError($"The player {player.Name} must specify LockFaction: True.");
				}
			}

			if (!playablePlayerFound && visibility != MapVisibility.Shellmap)
				emitError("Found no playable player.");

			if (!worldOwnerFound)
				emitError("Found no player owning the world.");

			var factions = worldActorInfo.TraitInfos<FactionInfo>().Select(f => f.InternalName).ToHashSet();
			foreach (var player in players.Players.Values)
				if (!string.IsNullOrWhiteSpace(player.Faction) && !factions.Contains(player.Faction))
					emitError($"Invalid faction {player.Faction} chosen for player {player.Name}.");

			if (worldActorInfo.HasTraitInfo<MapStartingLocationsInfo>())
			{
				var playerCount = players.Players.Count(p => p.Value.Playable);
				if (playerCount > spawnPoints.Length)
					emitError($"The map allows {playerCount} possible players, but defines only {spawnPoints.Length} spawn points");

				if (spawnPoints.Distinct().Count() != spawnPoints.Length)
					emitError("Duplicate spawn point locations detected.");
			}
		}
	}
}
