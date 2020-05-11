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
	[Desc("Allows the map to have working spawnpoints. Also controls the 'Separate Team Spawns' checkbox in the lobby options.")]
	public class MPStartLocationsInfo : TraitInfo, ILobbyOptions
	{
		public readonly WDist InitialExploreRange = WDist.FromCells(5);

		[Translate]
		[Desc("Descriptive label for the spawn positions checkbox in the lobby.")]
		public readonly string SeparateTeamSpawnsCheckboxLabel = "Separate Team Spawns";

		[Translate]
		[Desc("Tooltip description for the spawn positions checkbox in the lobby.")]
		public readonly string SeparateTeamSpawnsCheckboxDescription = "Players without assigned spawn points will start as far as possible from enemy players";

		[Desc("Default value of the spawn positions checkbox in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxEnabled = true;

		[Desc("Prevent the spawn positions state from being changed in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxLocked = false;

		[Desc("Whether to display the spawn positions checkbox in the lobby.")]
		public readonly bool SeparateTeamSpawnsCheckboxVisible = true;

		[Desc("Display order for the spawn positions checkbox in the lobby.")]
		public readonly int SeparateTeamSpawnsCheckboxDisplayOrder = 0;

		public override object Create(ActorInitializer init) { return new MPStartLocations(this); }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
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
	}

	public class MPStartLocations : IWorldLoaded, INotifyCreated
	{
		readonly MPStartLocationsInfo info;

		public readonly Dictionary<Player, CPos> Start = new Dictionary<Player, CPos>();

		bool separateTeamSpawns;

		public MPStartLocations(MPStartLocationsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			separateTeamSpawns = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("separateteamspawns", info.SeparateTeamSpawnsCheckboxEnabled);
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			var spawns = world.Actors.Where(a => a.Info.Name == "mpspawn")
				.Select(a => a.Location)
				.ToArray();

			var taken = world.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0 && c.Slot != null)
					.Select(c => spawns[c.SpawnPoint - 1]).ToList();
			var available = spawns.Except(taken).ToList();

			// Set spawn
			foreach (var kv in world.LobbyInfo.Slots)
			{
				var player = FindPlayerInSlot(world, kv.Key);
				if (player == null) continue;

				var client = world.LobbyInfo.ClientInSlot(kv.Key);
				var spid = (client == null || client.SpawnPoint == 0)
					? ChooseSpawnPoint(world, available, taken)
					: spawns[client.SpawnPoint - 1];

				Start.Add(player, spid);

				player.SpawnPoint = (client == null || client.SpawnPoint == 0)
					? spawns.IndexOf(spid) + 1
					: client.SpawnPoint;
			}

			// Explore allied shroud
			var map = world.Map;
			foreach (var p in Start.Keys)
			{
				var cells = Shroud.ProjectedCellsInRange(map, Start[p], info.InitialExploreRange);
				foreach (var q in world.Players)
					if (p.IsAlliedWith(q))
						q.Shroud.ExploreProjectedCells(world, cells);
			}

			// Set viewport
			if (world.LocalPlayer != null && Start.ContainsKey(world.LocalPlayer))
				wr.Viewport.Center(map.CenterOfCell(Start[world.LocalPlayer]));
		}

		static Player FindPlayerInSlot(World world, string pr)
		{
			return world.Players.FirstOrDefault(p => p.PlayerReference.Name == pr);
		}

		CPos ChooseSpawnPoint(World world, List<CPos> available, List<CPos> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			var n = taken.Count == 0 || !separateTeamSpawns
				? world.SharedRandom.Next(available.Count)
				: available // pick the most distant spawnpoint from everyone else
					.Select((k, i) => Pair.New(k, i))
					.MaxBy(a => taken.Sum(t => (t - a.First).LengthSquared)).Second;

			var sp = available[n];
			available.RemoveAt(n);
			taken.Add(sp);
			return sp;
		}
	}
}
