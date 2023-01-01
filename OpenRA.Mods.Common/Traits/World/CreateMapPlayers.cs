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
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Attach this to the world actor.")]
	public class CreateMapPlayersInfo : TraitInfo<CreateMapPlayers>, ICreatePlayersInfo
	{
		/// <summary>
		/// Returns a list of GameInformation.Players that matches the indexing of ICreatePlayers.CreatePlayers.
		/// Non-playable players appear as null in the list.
		/// </summary>
		void ICreatePlayersInfo.CreateServerPlayers(MapPreview map, Session lobbyInfo, List<GameInformation.Player> players, MersenneTwister playerRandom)
		{
			var factions = map.WorldActorInfo.TraitInfos<FactionInfo>().ToArray();
			var assignSpawnLocations = map.WorldActorInfo.TraitInfoOrDefault<IAssignSpawnPointsInfo>();
			var spawnState = assignSpawnLocations?.InitializeState(map, lobbyInfo);

			// Create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var p in map.Players.Players.Where(p => !p.Value.Playable))
			{
				// We need to resolve the faction, even though we don't use it, to match the RNG state with clients
				Player.ResolveFaction(p.Value.Faction, factions, playerRandom, false);
				players.Add(null);
			}

			// Create the regular playable players.
			var bots = map.PlayerActorInfo.TraitInfos<IBotInfo>().ToArray();

			foreach (var kv in lobbyInfo.Slots)
			{
				var client = lobbyInfo.ClientInSlot(kv.Key);
				if (client == null)
					continue;

				var clientFaction = factions.First(f => client.Faction == f.InternalName);
				var resolvedFaction = Player.ResolveFaction(client.Faction, factions, playerRandom, !kv.Value.LockFaction);
				var resolvedSpawnPoint = assignSpawnLocations?.AssignSpawnPoint(spawnState, lobbyInfo, client, playerRandom) ?? 0;
				var player = new GameInformation.Player
				{
					ClientIndex = client.Index,
					Name = Player.ResolvePlayerName(client, lobbyInfo.Clients, bots),
					IsHuman = client.Bot == null,
					IsBot = client.Bot != null,
					FactionName = resolvedFaction.Name,
					FactionId = resolvedFaction.InternalName,
					DisplayFactionName = clientFaction.Name,
					DisplayFactionId = clientFaction.InternalName,
					Color = client.Color,
					Team = client.Team,
					Handicap = client.Handicap,
					SpawnPoint = resolvedSpawnPoint,
					IsRandomFaction = clientFaction.RandomFactionMembers.Count > 0,
					IsRandomSpawnPoint = client.SpawnPoint == 0,
					Fingerprint = client.Fingerprint,
				};

				players.Add(player);
			}

			// Create a player that is allied with everyone for shared observer shroud.
			// We need to resolve the faction, even though we don't use it, to match the RNG state with clients
			Player.ResolveFaction("Random", factions, playerRandom, false);
			players.Add(null);
		}
	}

	public class CreateMapPlayers : ICreatePlayers
	{
		void ICreatePlayers.CreatePlayers(World w, MersenneTwister playerRandom)
		{
			var players = new MapPlayers(w.Map.PlayerDefinitions).Players;
			var worldPlayers = new List<Player>();
			var worldOwnerFound = false;

			// Create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var kv in players.Where(p => !p.Value.Playable))
			{
				var player = new Player(w, null, kv.Value, playerRandom);
				worldPlayers.Add(player);

				if (kv.Value.OwnsWorld)
				{
					worldOwnerFound = true;
					w.SetWorldOwner(player);
				}
			}

			if (!worldOwnerFound)
				throw new InvalidOperationException($"Map {w.Map.Title} does not define a player actor owning the world.");

			Player localPlayer = null;

			// Create the regular playable players.
			foreach (var kv in w.LobbyInfo.Slots)
			{
				var client = w.LobbyInfo.ClientInSlot(kv.Key);
				if (client == null)
					continue;

				var player = new Player(w, client, players[kv.Value.PlayerReference], playerRandom);
				worldPlayers.Add(player);

				if (client.Index == Game.LocalClientId)
					localPlayer = player;
			}

			// Create a player that is allied with everyone for shared observer shroud.
			worldPlayers.Add(new Player(w, null, new PlayerReference
			{
				Name = "Everyone",
				NonCombatant = true,
				Spectating = true,
				Faction = "Random",
				Allies = worldPlayers.Where(p => !p.NonCombatant && p.Playable).Select(p => p.InternalName).ToArray()
			}, playerRandom));

			w.SetPlayers(worldPlayers, localPlayer);

			foreach (var p in w.Players)
				foreach (var q in w.Players)
					SetupPlayerMasks(p, q);
		}

		static void SetupPlayerMasks(Player p, Player q)
		{
			if (!p.Spectating)
				p.World.AllPlayersMask = p.World.AllPlayersMask.Union(p.PlayerMask);

			if (p == q || p.PlayerReference.Allies.Contains(q.InternalName))
			{
				p.AlliedPlayersMask = p.AlliedPlayersMask.Union(q.PlayerMask);
				return;
			}

			if (p.PlayerReference.Enemies.Contains(q.InternalName))
			{
				p.EnemyPlayersMask = p.EnemyPlayersMask.Union(q.PlayerMask);
				return;
			}

			// HACK: Map players share a ClientID with the host, so would
			// otherwise take the host's team stance instead of being neutral
			if (p.PlayerReference.Playable && q.PlayerReference.Playable)
			{
				// Stances set via lobby teams
				var pc = GetClientForPlayer(p);
				var qc = GetClientForPlayer(q);
				if (pc != null && qc != null)
				{
					if (pc.Team != 0 && pc.Team == qc.Team)
						p.AlliedPlayersMask = p.AlliedPlayersMask.Union(q.PlayerMask);
					else
						p.EnemyPlayersMask = p.EnemyPlayersMask.Union(q.PlayerMask);
				}
			}
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
		}
	}
}
