#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System;
using OpenRA.FileFormats;
using OpenRA.Network;

namespace OpenRA.Traits
{
	public class MPStartLocationsInfo : TraitInfo<MPStartLocations>
	{
		public readonly int InitialExploreRange = 5;
	}
	
	public class MPStartLocations : IGameStarted
	{
		public Dictionary<Player, int2> Start = new Dictionary<Player, int2>();
		public void GameStarted(World world)
		{
			var taken = Game.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0)
				.Select(c => world.Map.SpawnPoints.ElementAt(c.SpawnPoint - 1)).ToList();
			var available = world.Map.SpawnPoints.Except(taken).ToList();

			// Set spawn
			foreach (var slot in Game.LobbyInfo.Slots)
			{
				var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Slot == slot.Index);
				var player = FindPlayerInSlot(world, slot);

				if (player == null) continue;

				var spid = (client == null || client.SpawnPoint == 0)
					? ChooseSpawnPoint(world, available, taken)
					: world.Map.SpawnPoints.ElementAt(client.SpawnPoint - 1);

				Start.Add(player, spid);
			}
			
			// Explore allied shroud
			foreach (var p in Start)
				if (p.Key == world.LocalPlayer || p.Key.Stances[world.LocalPlayer] == Stance.Ally)
					world.WorldActor.Trait<Shroud>().Explore(world, p.Value,
						world.WorldActor.Info.Traits.Get<MPStartLocationsInfo>().InitialExploreRange);
			
			// Set viewport
			if (world.LocalPlayer != null && Start.ContainsKey(world.LocalPlayer))
				Game.viewport.Center(Start[world.LocalPlayer]);
		}

		static Player FindPlayerInSlot(World world, Session.Slot slot)
		{
			return world.players.Values.FirstOrDefault(p => p.PlayerRef.Name == slot.MapPlayer);
		}
		
		static int2 ChooseSpawnPoint(World world, List<int2> available, List<int2> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			var n = taken.Count == 0
				? world.SharedRandom.Next(available.Count)
				: available			// pick the most distant spawnpoint from everyone else
					.Select((k, i) => Pair.New(k, i))
					.OrderByDescending(a => taken.Sum(t => (t - a.First).LengthSquared))
					.Select(a => a.Second)
					.First();

			var sp = available[n];
			available.RemoveAt(n);
			taken.Add(sp);
			return sp;
		}
	}
}

