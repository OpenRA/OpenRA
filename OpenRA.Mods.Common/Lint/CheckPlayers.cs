#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckPlayers : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var players = new MapPlayers(map.PlayerDefinitions).Players;

			var playerNames = players.Values.Select(p => p.Name);
			foreach (var player in players)
				foreach (var ally in player.Value.Allies)
					if (!playerNames.Contains(ally))
						emitError("Allies contains player {0} that is not in list.".F(ally));

			foreach (var player in players)
				foreach (var enemy in player.Value.Enemies)
					if (!playerNames.Contains(enemy))
						emitError("Enemies contains player {0} that is not in list.".F(enemy));

			var worldActor = map.Rules.Actors["world"];

			var races = worldActor.Traits.WithInterface<CountryInfo>().Select(c => c.Race);
			foreach (var player in players)
				if (!string.IsNullOrWhiteSpace(player.Value.Race) && player.Value.Race != "Random" && !races.Contains(player.Value.Race))
					emitError("Invalid race {0} chosen for player {1}.".F(player.Value.Race, player.Value.Name));

			if (worldActor.Traits.Contains<MPStartLocationsInfo>())
			{
				var multiPlayers = players.Where(p => p.Value.Playable).Count();
				var spawns = map.ActorDefinitions.Where(a => a.Value.Value == "mpspawn");
				var spawnCount = spawns.Count();

				if (multiPlayers > spawnCount)
					emitError("The map allows {0} possible players, but defines only {1} spawn points".F(multiPlayers, spawnCount));

				if (map.SpawnPoints.Value.Distinct().Count() != spawnCount)
					emitError("Duplicate spawn point locations detected.");
			}

			foreach (var kv in map.ActorDefinitions)
			{
				var actorReference = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var ownerName = actorReference.InitDict.Get<OwnerInit>().PlayerName;
				if (!playerNames.Contains(ownerName))
					emitError("Actor {0} is owned by unknown player {1}.".F(actorReference.Type, ownerName));
			}
		}
	}
}