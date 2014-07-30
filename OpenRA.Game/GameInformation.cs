#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA
{
	public class GameInformation
	{
		public string MapUid;
		public string MapTitle;
		public DateTime StartTimeUtc;
		// Game end timestamp (when the recoding stopped).
		public DateTime EndTimeUtc;

		// Gets the game's duration, from the time the game started until the
		// replay recording stopped.
		public TimeSpan Duration { get { return EndTimeUtc > StartTimeUtc ? EndTimeUtc - StartTimeUtc : TimeSpan.Zero; } }
		public IList<Player> Players { get; private set; }
		public MapPreview MapPreview { get { return Game.modData.MapCache[MapUid]; } }
		public IEnumerable<Player> HumanPlayers { get { return Players.Where(p => p.IsHuman); } }
		public bool IsSinglePlayer { get { return HumanPlayers.Count() == 1; } }

		Dictionary<OpenRA.Player, Player> playersByRuntime;

		public GameInformation()
		{
			Players = new List<Player>();
			playersByRuntime = new Dictionary<OpenRA.Player, Player>();
		}

		public static GameInformation Deserialize(string data)
		{
			try
			{
				var info = new GameInformation();

				var nodes = MiniYaml.FromString(data);
				foreach (var node in nodes)
				{
					var keyParts = node.Key.Split('@');

					switch (keyParts[0])
					{
						case "Root":
							FieldLoader.Load(info, node.Value);
							break;

						case "Player":
							info.Players.Add(FieldLoader.Load<Player>(node.Value));
							break;
					}
				}

				return info;
			}
			catch (InvalidOperationException)
			{
				Log.Write("debug", "GameInformation deserialized invalid MiniYaml:\n{0}".F(data));
				throw;
			}
		}

		public string Serialize()
		{
			var nodes = new List<MiniYamlNode>();

			nodes.Add(new MiniYamlNode("Root", FieldSaver.Save(this)));

			for (var i = 0; i < Players.Count; i++)
				nodes.Add(new MiniYamlNode("Player@{0}".F(i), FieldSaver.Save(Players[i])));

			return nodes.WriteToString();
		}

		// Adds the player information at start-up.
		public void AddPlayer(OpenRA.Player runtimePlayer, Session lobbyInfo)
		{
			if (runtimePlayer == null)
				throw new ArgumentNullException("runtimePlayer");

			if (lobbyInfo == null)
				throw new ArgumentNullException("lobbyInfo");

			// We don't care about spectators and map players
			if (runtimePlayer.NonCombatant || !runtimePlayer.Playable)
				return;

			// Find the lobby client that created the runtime player
			var client = lobbyInfo.ClientWithIndex(runtimePlayer.ClientIndex);
			if (client == null)
				return;

			var player = new Player
			{
				ClientIndex = runtimePlayer.ClientIndex,
				Name = runtimePlayer.PlayerName,
				IsHuman = !runtimePlayer.IsBot,
				IsBot = runtimePlayer.IsBot,
				FactionName = runtimePlayer.Country.Name,
				FactionId = runtimePlayer.Country.Race,
				Color = runtimePlayer.Color,
				Team = client.Team,
				SpawnPoint = runtimePlayer.SpawnPoint,
				IsRandomFaction = runtimePlayer.Country.Race != client.Country,
				IsRandomSpawnPoint = runtimePlayer.SpawnPoint != client.SpawnPoint
			};

			playersByRuntime.Add(runtimePlayer, player);
			Players.Add(player);
		}

		// Gets the player information for the specified runtime player instance.
		public Player GetPlayer(OpenRA.Player runtimePlayer)
		{
			Player player;

			playersByRuntime.TryGetValue(runtimePlayer, out player);

			return player;
		}

		public class Player
		{
			//
			// Start-up information 
			//

			public int ClientIndex;
			// The player name, not guaranteed to be unique.
			public string Name;
			public bool IsHuman;
			public bool IsBot;
			// The faction name (aka Country)
			public string FactionName;
			// The faction id (aka Country, aka Race)
			public string FactionId;
			public HSLColor Color;
			// The team id on start-up, or 0 if the player is not part of the team.
			public int Team;
			public int SpawnPoint;
			// True if the faction was chosen at random; otherwise, false
			public bool IsRandomFaction;
			// True if the spawn point was chosen at random; otherwise, false.</summary>
			public bool IsRandomSpawnPoint;

			//
			// Information gathered at a later stage
			//

			// The game outcome for this player
			public WinState Outcome;
			// The time when this player won or lost the game
			public DateTime OutcomeTimestampUtc;
		}
	}
}
