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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Allows the map to have working spawnpoints. Also controls the 'Separate Team Spawns' checkbox in the lobby options.")]
	public class MapStartingLocationsInfo : TraitInfo, ILobbyOptions, IAssignSpawnPointsInfo
	{
		public readonly WDist InitialExploreRange = WDist.FromCells(5);

		[TranslationReference]
		[Desc("Descriptive label for the spawn positions checkbox in the lobby.")]
		public readonly string SeparateTeamSpawnsCheckboxLabel = "checkbox-separate-team-spawns.label";

		[TranslationReference]
		[Desc("Tooltip description for the spawn positions checkbox in the lobby.")]
		public readonly string SeparateTeamSpawnsCheckboxDescription = "checkbox-separate-team-spawns.description";

		[Desc("Default value of the spawn positions checkbox in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxEnabled = true;

		[Desc("Prevent the spawn positions state from being changed in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxLocked = false;

		[Desc("Whether to display the spawn positions checkbox in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxVisible = true;

		[Desc("Display order for the spawn positions checkbox in the lobby.")]
		public readonly int SeparateTeamSpawnsCheckboxDisplayOrder = 0;

		public override object Create(ActorInitializer init) { return new MapStartingLocations(this); }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(
				"separateteamspawns",
				SeparateTeamSpawnsCheckboxLabel,
				SeparateTeamSpawnsCheckboxDescription,
				SeparateTeamSpawnsCheckboxVisible,
				SeparateTeamSpawnsCheckboxDisplayOrder,
				SeparateTeamSpawnsCheckboxEnabled,
				SeparateTeamSpawnsCheckboxLocked);
		}

		class AssignSpawnLocationsState
		{
			public CPos[] SpawnLocations;
			public List<int> AvailableSpawnPoints;
			public readonly Dictionary<int, Session.Client> OccupiedSpawnPoints = new Dictionary<int, Session.Client>();
		}

		object IAssignSpawnPointsInfo.InitializeState(MapPreview map, Session lobbyInfo)
		{
			var state = new AssignSpawnLocationsState
			{
				// Initialize the list of unoccupied spawn points for AssignSpawnLocations to pick from
				SpawnLocations = map.SpawnPoints,
				AvailableSpawnPoints = LobbyUtils.AvailableSpawnPoints(map.SpawnPoints.Length, lobbyInfo)
			};

			foreach (var kv in lobbyInfo.Slots)
			{
				var client = lobbyInfo.ClientInSlot(kv.Key);
				if (client == null || client.SpawnPoint == 0)
					continue;

				state.AvailableSpawnPoints.Remove(client.SpawnPoint);
				state.OccupiedSpawnPoints.Add(client.SpawnPoint, client);
			}

			return state;
		}

		int IAssignSpawnPointsInfo.AssignSpawnPoint(object stateObject, Session lobbyInfo, Session.Client client, MersenneTwister playerRandom)
		{
			var state = (AssignSpawnLocationsState)stateObject;
			var separateTeamSpawns = lobbyInfo.GlobalSettings.OptionOrDefault("separateteamspawns", SeparateTeamSpawnsCheckboxEnabled);

			if (client.SpawnPoint > 0 && client.SpawnPoint <= state.SpawnLocations.Length)
				return client.SpawnPoint;

			var spawnPoint = state.OccupiedSpawnPoints.Count == 0 || !separateTeamSpawns
				? state.AvailableSpawnPoints.Random(playerRandom)
				: state.AvailableSpawnPoints // pick the most distant spawnpoint from everyone else
					.Select(s => (Cell: state.SpawnLocations[s - 1], Index: s))
					.MaxBy(s => state.OccupiedSpawnPoints.Sum(kv => (state.SpawnLocations[kv.Key - 1] - s.Cell).LengthSquared)).Index;

			state.AvailableSpawnPoints.Remove(spawnPoint);
			state.OccupiedSpawnPoints.Add(spawnPoint, client);
			return spawnPoint;
		}
	}

	public class MapStartingLocations : IWorldLoaded, INotifyCreated, IAssignSpawnPoints
	{
		readonly MapStartingLocationsInfo info;
		readonly Dictionary<int, Session.Client> occupiedSpawnPoints = new Dictionary<int, Session.Client>();
		bool separateTeamSpawns;
		CPos[] spawnLocations;
		List<int> availableSpawnPoints;

		public MapStartingLocations(MapStartingLocationsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			separateTeamSpawns = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("separateteamspawns", info.SeparateTeamSpawnsCheckboxEnabled);

			var spawns = new List<CPos>();
			foreach (var n in self.World.Map.ActorDefinitions)
				if (n.Value.Value == "mpspawn")
					spawns.Add(new ActorReference(n.Key, n.Value.ToDictionary()).GetValue<LocationInit, CPos>());

			spawnLocations = spawns.ToArray();

			// Initialize the list of unoccupied spawn points for AssignSpawnLocations to pick from
			availableSpawnPoints = LobbyUtils.AvailableSpawnPoints(spawnLocations.Length, self.World.LobbyInfo);
			foreach (var kv in self.World.LobbyInfo.Slots)
			{
				var client = self.World.LobbyInfo.ClientInSlot(kv.Key);
				if (client == null || client.SpawnPoint == 0)
					continue;

				availableSpawnPoints.Remove(client.SpawnPoint);
				occupiedSpawnPoints.Add(client.SpawnPoint, client);
			}
		}

		CPos IAssignSpawnPoints.AssignHomeLocation(World world, Session.Client client, MersenneTwister playerRandom)
		{
			if (client.SpawnPoint > 0 && client.SpawnPoint <= spawnLocations.Length)
				return spawnLocations[client.SpawnPoint - 1];

			var spawnPoint = occupiedSpawnPoints.Count == 0 || !separateTeamSpawns
				? availableSpawnPoints.Random(playerRandom)
				: availableSpawnPoints // pick the most distant spawnpoint from everyone else
					.Select(s => (Cell: spawnLocations[s - 1], Index: s))
					.MaxBy(s => occupiedSpawnPoints.Sum(kv => (spawnLocations[kv.Key - 1] - s.Cell).LengthSquared)).Index;

			availableSpawnPoints.Remove(spawnPoint);
			occupiedSpawnPoints.Add(spawnPoint, client);
			return spawnLocations[spawnPoint - 1];
		}

		int IAssignSpawnPoints.SpawnPointForPlayer(Player player)
		{
			foreach (var kv in occupiedSpawnPoints)
				if (kv.Value.Index == player.ClientIndex)
					return kv.Key;

			return 0;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var p in world.Players)
			{
				if (!p.Playable)
					continue;

				if (p == world.LocalPlayer)
					wr.Viewport.Center(world.Map.CenterOfCell(p.HomeLocation));

				var cells = Shroud.ProjectedCellsInRange(world.Map, p.HomeLocation, info.InitialExploreRange)
					.ToList();

				foreach (var q in world.Players)
					if (p.IsAlliedWith(q))
						q.Shroud.ExploreProjectedCells(cells);
			}
		}
	}
}
