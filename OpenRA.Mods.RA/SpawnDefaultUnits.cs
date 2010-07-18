#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SpawnDefaultUnitsInfo : TraitInfo<SpawnDefaultUnits>
	{
		public readonly int InitialExploreRange = 5;
	}

	class SpawnDefaultUnits : IGameStarted
	{
		public void GameStarted(World world)
		{
			var taken = Game.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0)
				.Select(c => world.Map.SpawnPoints.ElementAt(c.SpawnPoint - 1)).ToList();

			var available = world.Map.SpawnPoints.Except(taken).ToList();

			foreach (var client in Game.LobbyInfo.Clients)
			{
				SpawnUnitsForPlayer(world.players[client.Index],
					(client.SpawnPoint == 0)
					? ChooseSpawnPoint(world, available, taken)
					: world.Map.SpawnPoints.ElementAt(client.SpawnPoint - 1));
			}	
		}

		void SpawnUnitsForPlayer(Player p, int2 sp)
		{
			p.World.CreateActor("mcv", sp, p);

			if (p == p.World.LocalPlayer || p.Stances[p.World.LocalPlayer] == Stance.Ally)
				p.World.WorldActor.traits.Get<Shroud>().Explore(p.World, sp,
					p.World.WorldActor.Info.Traits.Get<SpawnDefaultUnitsInfo>().InitialExploreRange);
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
