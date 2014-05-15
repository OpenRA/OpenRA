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
	/// <summary>
	/// Contains information about a finished game
	/// </summary>
	public class GameInformation
	{
		/// <summary>The map identifier.</summary>
		public string MapUid;
		/// <summary>The map title.</summary>
		public string MapTitle;
		/// <summary>Game start timestamp.</summary>
		public DateTime StartTimeUtc;
		/// <summary>Game end timestamp (when the recoding stopped).</summary>
		public DateTime EndTimeUtc;

		/// <summary>
		/// Gets the game's duration, from the time the game started until the
		/// replay recording stopped.
		/// </summary>
		/// <value>The game's duration.</value>
		public TimeSpan Duration { get { return EndTimeUtc > StartTimeUtc ? EndTimeUtc - StartTimeUtc : TimeSpan.Zero; } }
		/// <summary>
		/// Gets the list of players.
		/// </summary>
		/// <value>The players.</value>
		public IList<Player> Players { get; private set; }
		/// <summary>
		/// Gets the map preview, using <see cref="Game.modData.MapCache"/> and the <see cref="MapUid"/>.
		/// </summary>
		/// <value>The map preview.</value>
		public MapPreview MapPreview { get { return Game.modData.MapCache[MapUid]; } }
		/// <summary>
		/// Gets the human players.
		/// </summary>
		/// <value>The human players.</value>
		public IEnumerable<Player> HumanPlayers { get { return Players.Where(p => p.IsHuman); } }
		/// <summary>
		/// Gets a value indicating whether this instance has just one human player.
		/// </summary>
		/// <value><c>true</c> if this instance has just one human player; otherwise, <c>false</c>.</value>
		public bool IsSinglePlayer { get { return HumanPlayers.Count() == 1; } }

		Dictionary<OpenRA.Player, Player> playersByRuntime;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public GameInformation()
		{
			Players = new List<Player>();
			playersByRuntime = new Dictionary<OpenRA.Player, Player>();
		}

		/// <summary>
		/// Deserialize the specified data into a new instance.
		/// </summary>
		/// <param name="data">Data.</param>
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

		/// <summary>
		/// Serialize this instance.
		/// </summary>
		public string Serialize()
		{
			var nodes = new List<MiniYamlNode>();

			nodes.Add(new MiniYamlNode("Root", FieldSaver.Save(this)));

			for (var i=0; i<Players.Count; i++)
				nodes.Add(new MiniYamlNode("Player@{0}".F(i), FieldSaver.Save(Players[i])));

			return nodes.WriteToString();
		}

		/// <summary>
		/// Adds the start-up player information.
		/// </summary>
		/// <param name="runtimePlayer">Runtime player.</param>
		/// <param name="lobbyInfo">Lobby info.</param>
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

		/// <summary>
		/// Gets the player information for the specified runtime player instance.
		/// </summary>
		/// <returns>The player, or <c>null</c>.</returns>
		/// <param name="runtimePlayer">Runtime player.</param>
		public Player GetPlayer(OpenRA.Player runtimePlayer)
		{
			Player player;

			playersByRuntime.TryGetValue(runtimePlayer, out player);

			return player;
		}

		/// <summary>Specifies whether the player was defeated, victorious, or there was no outcome defined.</summary>
		public enum GameOutcome
		{
			/// <summary>Unknown outcome.</summary>
			Undefined,
			/// <summary>The player was defeated</summary>
			Defeat,
			/// <summary>The player was victorious</summary>
			Victory
		}

		///<summary>
		/// Information about a player
		/// </summary>
		public class Player
		{
			//
			// Start-up information 
			//

			/// <summary>The client index.</summary>
			public int ClientIndex;
			/// <summary>The player name, not guaranteed to be unique.</summary>
			public string Name;
			/// <summary><c>true</c> if the player is a human player; otherwise, <c>false</c>.</summary>
			public bool IsHuman;
			/// <summary><c>true</c> if the player is a bot; otherwise, <c>false</c>.</summary>
			public bool IsBot;
			/// <summary>The faction name (aka Country).</summary>
			public string FactionName;
			/// <summary>The faction id (aka Country, aka Race).</summary>
			public string FactionId;
			/// <summary>The color used by the player in the game.</summary>
			public HSLColor Color;
			/// <summary>The team id on start-up, or 0 if the player is not part of the team.</summary>
			public int Team;
			/// <summary>The index of the spawn point on the map, or 0 if the player is not part of the team.</summary>
			public int SpawnPoint;
			/// <summary><c>true</c> if the faction was chosen at random; otherwise, <c>false</c>.</summary>
			public bool IsRandomFaction;
			/// <summary><c>true</c> if the spawn point was chosen at random; otherwise, <c>false</c>.</summary>
			public bool IsRandomSpawnPoint;

			//
			// Information gathered at a later stage
			//

			/// <summary>The game outcome for this player.</summary>
			public GameOutcome Outcome;
			/// <summary>The time when this player won or lost the game.</summary>
			public DateTime OutcomeTimestampUtc;
		}
	}
}
