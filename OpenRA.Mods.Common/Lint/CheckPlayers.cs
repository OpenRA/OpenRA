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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckPlayers : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			var players = new MapPlayers(map.PlayerDefinitions).Players;
			if (players.Count > 64)
				emitError("Defining more than 64 players is not allowed.");

			var worldOwnerFound = false;
			var playerNames = players.Values.Select(p => p.Name).ToHashSet();
			foreach (var player in players.Values)
			{
				foreach (var ally in player.Allies)
					if (!playerNames.Contains(ally))
						emitError("Allies contains player {0} that is not in list.".F(ally));

				foreach (var enemy in player.Enemies)
					if (!playerNames.Contains(enemy))
						emitError("Enemies contains player {0} that is not in list.".F(enemy));

				if (player.OwnsWorld)
				{
					worldOwnerFound = true;
					if (player.Enemies.Any() || player.Allies.Any())
						emitWarning("The player {0} owning the world should not have any allies or enemies.".F(player.Name));

					if (player.Playable)
						emitError("The player {0} owning the world can't be playable.".F(player.Name));
				}
				else if (map.Visibility == MapVisibility.MissionSelector && player.Playable && !player.LockFaction)
				{
					// Missions must lock the faction of the player to force the server to override the default Random faction
					emitError("The player {0} must specify LockFaction: True.".F(player.Name));
				}
			}

			if (!worldOwnerFound)
				emitError("Found no player owning the world.");

			var worldActor = map.Rules.Actors["world"];
			var factions = worldActor.TraitInfos<FactionInfo>().Select(f => f.InternalName).ToHashSet();
			foreach (var player in players.Values)
				if (!string.IsNullOrWhiteSpace(player.Faction) && !factions.Contains(player.Faction))
					emitError("Invalid faction {0} chosen for player {1}.".F(player.Faction, player.Name));

			if (worldActor.HasTraitInfo<MapStartingLocationsInfo>())
			{
				var playerCount = players.Count(p => p.Value.Playable);
				var spawns = new List<CPos>();
				foreach (var kv in map.ActorDefinitions.Where(d => d.Value.Value == "mpspawn"))
				{
					var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
					spawns.Add(s.Get<LocationInit>().Value);
				}

				if (playerCount > spawns.Count)
					emitError("The map allows {0} possible players, but defines only {1} spawn points".F(playerCount, spawns.Count));

				if (spawns.Distinct().Count() != spawns.Count)
					emitError("Duplicate spawn point locations detected.");
			}

			// Check for actors that require specific owners
			var actorsWithRequiredOwner = map.Rules.Actors
				.Where(a => a.Value.HasTraitInfo<RequiresSpecificOwnersInfo>())
				.ToDictionary(a => a.Key, a => a.Value.TraitInfo<RequiresSpecificOwnersInfo>());

			foreach (var kv in map.ActorDefinitions)
			{
				var actorReference = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var ownerInit = actorReference.GetOrDefault<OwnerInit>();
				if (ownerInit == null)
					emitError("Actor {0} is not owned by any player.".F(kv.Key));
				else
				{
					var ownerName = ownerInit.InternalName;
					if (!playerNames.Contains(ownerName))
						emitError("Actor {0} is owned by unknown player {1}.".F(kv.Key, ownerName));

					if (actorsWithRequiredOwner.TryGetValue(kv.Value.Value, out var info))
						if (!info.ValidOwnerNames.Contains(ownerName))
							emitError("Actor {0} owner {1} is not one of ValidOwnerNames: {2}".F(kv.Key, ownerName, info.ValidOwnerNames.JoinWith(", ")));
				}
			}
		}
	}
}
