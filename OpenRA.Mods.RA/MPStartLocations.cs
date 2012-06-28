#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class MPStartLocationsInfo : TraitInfo<MPStartLocations>
	{
		public readonly int InitialExploreRange = 5;
	}

	public class MPStartLocations : IWorldLoaded
	{
		public Dictionary<Player, CPos> Start = new Dictionary<Player, CPos>();

		public void WorldLoaded(World world)
		{
			var taken = world.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0 && c.Slot != null)
				.Select(c => (CPos) world.Map.GetSpawnPoints()[c.SpawnPoint-1]).ToList();
			var available = world.Map.GetSpawnPoints().Select(c => (CPos)c).Except(taken).ToList();

			// Set spawn
			foreach (var kv in world.LobbyInfo.Slots)
			{
				var player = FindPlayerInSlot(world, kv.Key);
				if (player == null) continue;

				var client = world.LobbyInfo.ClientInSlot(kv.Key);
				var spid = (client == null || client.SpawnPoint == 0)
					? ChooseSpawnPoint(world, available, taken)
					: (CPos)world.Map.GetSpawnPoints()[client.SpawnPoint-1];

				Start.Add(player, spid);
			}

			// Explore allied shroud
			foreach (var p in Start)
				if ((world.LocalPlayer != null ) &&(p.Key == world.LocalPlayer || p.Key.Stances[world.LocalPlayer] == Stance.Ally))
					world.WorldActor.Trait<Shroud>().Explore(world, p.Value,
						world.WorldActor.Info.Traits.Get<MPStartLocationsInfo>().InitialExploreRange);

			// Set viewport
			if (world.LocalPlayer != null && Start.ContainsKey(world.LocalPlayer))
				Game.viewport.Center(Start[world.LocalPlayer].ToFloat2());
		}

		static Player FindPlayerInSlot(World world, string pr)
		{
			return world.Players.FirstOrDefault(p => p.PlayerReference.Name == pr);
		}

		static CPos ChooseSpawnPoint(World world, List<CPos> available, List<CPos> taken)
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

