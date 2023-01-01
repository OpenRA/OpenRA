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
using System.Text.RegularExpressions;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public class GameClient
	{
		public readonly string Name;
		public readonly string Fingerprint;
		public readonly Color Color;
		public readonly string Faction;
		public readonly int Team;
		public readonly int SpawnPoint;
		public readonly bool IsAdmin;
		public readonly bool IsSpectator;
		public readonly bool IsBot;

		public GameClient() { }

		public GameClient(Session.Client c)
		{
			Name = c.Name;
			Fingerprint = c.Fingerprint;
			Color = c.Color;
			Faction = c.Faction;
			Team = c.Team;
			SpawnPoint = c.SpawnPoint;
			IsAdmin = c.IsAdmin;
			IsSpectator = c.Slot == null && c.Bot == null;
			IsBot = c.Bot != null;
		}
	}

	public class GameServer
	{
		static readonly string[] SerializeFields =
		{
			// Server information
			"Name", "Address",

			// Mod information
			"Mod", "Version", "ModTitle", "ModWebsite", "ModIcon32",

			// Current server state
			"Map", "State", "MaxPlayers", "Protected", "Authentication", "DisabledSpawnPoints"
		};

		public const int ProtocolVersion = 2;

		/// <summary>Online game number or -1 for LAN games</summary>
		public readonly int Id = -1;

		/// <summary>Name of the server</summary>
		public readonly string Name = null;

		/// <summary>ip:port string to connect to.</summary>
		public readonly string Address = null;

		/// <summary>Port of the server</summary>
		public readonly int Port = 0;

		/// <summary>The current state of the server (waiting/playing/completed)</summary>
		public readonly int State = 0;

		/// <summary>The number of slots available for non-bot players</summary>
		public readonly int MaxPlayers = 0;

		/// <summary>UID of the map</summary>
		public readonly string Map = null;

		/// <summary>Mod ID</summary>
		public readonly string Mod = "";

		/// <summary>Mod Version</summary>
		public readonly string Version = "";

		/// <summary>Human-readable mod title</summary>
		public readonly string ModTitle = "";

		/// <summary>URL to show in game listings for custom/unknown mods.</summary>
		public readonly string ModWebsite = "";

		/// <summary>URL to a 32x32 px icon for the mod.</summary>
		public readonly string ModIcon32 = "";

		/// <summary>GeoIP resolved server location.</summary>
		public readonly string Location = "";

		/// <summary>Password protected</summary>
		public readonly bool Protected = false;

		/// <summary>Players must be authenticated with the OpenRA forum</summary>
		public readonly bool Authentication = false;

		/// <summary>UTC datetime string when the game changed to the Playing state</summary>
		public readonly string Started = null;

		/// <summary>Number of non-spectator, non-bot players. Only defined if GameServer is parsed from yaml.</summary>
		public readonly int Players = 0;

		/// <summary>Number of bot players. Only defined if GameServer is parsed from yaml.</summary>
		public readonly int Bots = 0;

		/// <summary>Number of spectators. Only defined if GameServer is parsed from yaml.</summary>
		public readonly int Spectators = 0;

		/// <summary>Number of seconds that the game has been in the Playing state. Only defined if GameServer is parsed from yaml.</summary>
		public readonly int PlayTime = -1;

		/// <summary>Can we join this server (after switching mods if required)? Only defined if GameServer is parsed from yaml.</summary>
		[FieldLoader.Ignore]
		public readonly bool IsCompatible = false;

		/// <summary>Can we join this server (after switching mods if required)? Only defined if GameServer is parsed from yaml.</summary>
		[FieldLoader.Ignore]
		public readonly bool IsJoinable = false;

		[FieldLoader.LoadUsing(nameof(LoadClients))]
		public readonly GameClient[] Clients;

		/// <summary>The list of spawnpoints that are disabled for this game</summary>
		public readonly int[] DisabledSpawnPoints = Array.Empty<int>();

		public string ModLabel => $"{ModTitle} ({Version})";

		static object LoadClients(MiniYaml yaml)
		{
			var clients = new List<GameClient>();
			var clientsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Clients");
			if (clientsNode != null)
			{
				var regex = new Regex(@"Client@\d+");
				foreach (var client in clientsNode.Value.Nodes)
					if (regex.IsMatch(client.Key))
						clients.Add(FieldLoader.Load<GameClient>(client.Value));
			}

			return clients.ToArray();
		}

		public GameServer(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);

			// Games advertised using the old API used a single Mods field
			if (Mod == null || Version == null)
			{
				var modsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Mods");
				if (modsNode != null)
				{
					var modVersion = modsNode.Value.Value.Split('@');
					Mod = modVersion[0];
					Version = modVersion[1];
				}
			}

			// Games advertised using the old API calculated the play time locally
			if (State == 2 && PlayTime < 0)
				if (DateTime.TryParse(Started, out var startTime))
					PlayTime = (int)(DateTime.UtcNow - startTime).TotalSeconds;

			var externalKey = ExternalMod.MakeKey(Mod, Version);
			if (Game.ExternalMods.TryGetValue(externalKey, out var external) && external.Version == Version)
				IsCompatible = true;

			// Games advertised using the old API used local mod metadata
			if (string.IsNullOrEmpty(ModTitle))
			{
				if (external != null && external.Version == Version)
				{
					// Use external mod registration to populate the section header
					ModTitle = external.Title;
				}
				else if (Game.Mods.TryGetValue(Mod, out var mod))
				{
					// Use internal mod data to populate the section header, but
					// on-connect switching must use the external mod plumbing.
					ModTitle = mod.Metadata.Title;
				}
				else
				{
					// Some platforms (e.g. macOS) package each mod separately, so the Mods check above won't work.
					// Guess based on the most recent ExternalMod instead.
					var guessMod = Game.ExternalMods.Values
						.OrderByDescending(m => m.Version)
						.FirstOrDefault(m => m.Id == Mod);

					if (guessMod != null)
						ModTitle = guessMod.Title;
					else
						ModTitle = $"Unknown mod: {Mod}";
				}
			}

			var mapAvailable = Game.Settings.Game.AllowDownloading || Game.ModData.MapCache[Map].Status == MapStatus.Available;
			IsJoinable = IsCompatible && State == 1 && mapAvailable;
		}

		public GameServer(Server.Server server)
		{
			var manifest = server.ModData.Manifest;

			Name = server.Settings.Name;

			// IP address will be replaced with a real value by the master server / receiving LAN client
			Address = "0.0.0.0:" + server.Settings.ListenPort.ToString();
			State = (int)server.State;
			MaxPlayers = server.LobbyInfo.Slots.Count(s => !s.Value.Closed) - server.LobbyInfo.Clients.Count(c1 => c1.Bot != null);
			Map = server.Map.Uid;
			Mod = manifest.Id;
			Version = manifest.Metadata.Version;
			ModTitle = manifest.Metadata.Title;
			ModWebsite = manifest.Metadata.Website;
			ModIcon32 = manifest.Metadata.WebIcon32;
			Protected = !string.IsNullOrEmpty(server.Settings.Password);
			Authentication = server.Settings.RequireAuthentication || server.Settings.ProfileIDWhitelist.Length > 0;
			Clients = server.LobbyInfo.Clients.Select(c => new GameClient(c)).ToArray();
			DisabledSpawnPoints = server.LobbyInfo.DisabledSpawnPoints?.ToArray() ?? Array.Empty<int>();
		}

		public string ToPOSTData(bool lanGame)
		{
			var root = new List<MiniYamlNode>() { new MiniYamlNode("Protocol", ProtocolVersion.ToString()) };
			foreach (var field in SerializeFields)
				root.Add(FieldSaver.SaveField(this, field));

			if (lanGame)
			{
				// Add fields that are normally generated by the master server
				// LAN games overload the Id with a GUID string (rather than an ID) to allow deduplication
				root.Add(new MiniYamlNode("Id", Platform.SessionGUID.ToString()));
				root.Add(new MiniYamlNode("Players", Clients.Count(c => !c.IsBot && !c.IsSpectator).ToString()));
				root.Add(new MiniYamlNode("Spectators", Clients.Count(c => c.IsSpectator).ToString()));
				root.Add(new MiniYamlNode("Bots", Clients.Count(c => c.IsBot).ToString()));

				// Included for backwards compatibility with older clients that don't support separated Mod/Version.
				root.Add(new MiniYamlNode("Mods", Mod + "@" + Version));
			}

			var clientsNode = new MiniYaml("");
			var i = 0;
			foreach (var c in Clients)
				clientsNode.Nodes.Add(new MiniYamlNode("Client@" + (i++).ToString(), FieldSaver.Save(c)));

			root.Add(new MiniYamlNode("Clients", clientsNode));
			return new MiniYaml("", root)
				.ToLines("Game")
				.JoinWith("\n");
		}
	}
}
