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
using System.Net;
using System.Net.Sockets;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();

		// Keyed by the PlayerReference id that the slot corresponds to
		public Dictionary<string, Slot> Slots = new Dictionary<string, Slot>();

		public HashSet<int> DisabledSpawnPoints = new HashSet<int>();

		public Global GlobalSettings = new Global();

		public static string AnonymizeIP(IPAddress ip)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				// Follow convention used by Google Analytics: remove last octet
				var b = ip.GetAddressBytes();
				return $"{b[0]}.{b[1]}.{b[2]}.*";
			}

			return null;
		}

		public static Session Deserialize(string data)
		{
			try
			{
				var session = new Session();

				var nodes = MiniYaml.FromString(data);
				foreach (var node in nodes)
				{
					var strings = node.Key.Split('@');

					switch (strings[0])
					{
						case "Client":
							session.Clients.Add(Client.Deserialize(node.Value));
							break;

						case "GlobalSettings":
							session.GlobalSettings = Global.Deserialize(node.Value);
							break;

						case "Slot":
							var s = Slot.Deserialize(node.Value);
							session.Slots.Add(s.PlayerReference, s);
							break;
						case "DisabledSpawnPoints":
							session.DisabledSpawnPoints = FieldLoader.GetValue<HashSet<int>>("DisabledSpawnPoints", node.Value.Value);
							break;
					}
				}

				return session;
			}
			catch (YamlException)
			{
				throw new YamlException($"Session deserialized invalid MiniYaml:\n{data}");
			}
			catch (InvalidOperationException)
			{
				throw new YamlException($"Session deserialized invalid MiniYaml:\n{data}");
			}
		}

		public Client ClientWithIndex(int clientID)
		{
			return Clients.SingleOrDefault(c => c.Index == clientID);
		}

		public Client ClientInSlot(string slot)
		{
			return Clients.SingleOrDefault(c => c.Slot == slot);
		}

		public string FirstEmptySlot()
		{
			return Slots.FirstOrDefault(s => !s.Value.Closed && ClientInSlot(s.Key) == null).Key;
		}

		public string FirstEmptyBotSlot()
		{
			return Slots.FirstOrDefault(s => !s.Value.Closed && ClientInSlot(s.Key) == null && s.Value.AllowBots).Key;
		}

		public IEnumerable<Client> NonBotClients
		{
			get { return Clients.Where(c => c.Bot == null); }
		}

		public IEnumerable<Client> NonBotPlayers
		{
			get { return Clients.Where(c => c.Bot == null && c.Slot != null); }
		}

		public enum ClientState { NotReady, Invalid, Ready, Disconnected = 1000 }

		public enum ConnectionQuality { Good, Moderate, Poor }

		public class Client
		{
			public static Client Deserialize(MiniYaml data)
			{
				return FieldLoader.Load<Client>(data);
			}

			public int Index;
			public Color PreferredColor; // Color that the client normally uses from settings.yaml.
			public Color Color; // Actual color that the client is using. Usually the same as PreferredColor but can be different on maps with locked colors.
			public string Faction;
			public int SpawnPoint;
			public string Name;

			// The full IP address is required for the IP banning moderation feature
			// but we must not share the un-anonymized address with other players.
			[FieldLoader.Ignore]
			public string IPAddress;
			public string AnonymizedIPAddress;
			public string Location;
			public ConnectionQuality ConnectionQuality = ConnectionQuality.Good;

			public ClientState State = ClientState.Invalid;
			public int Team;
			public int Handicap;
			public string Slot; // Slot ID, or null for observer
			public string Bot; // Bot type, null for real clients
			public int BotControllerClientIndex; // who added the bot to the slot
			public bool IsAdmin;
			public bool IsReady => State == ClientState.Ready;
			public bool IsInvalid => State == ClientState.Invalid;
			public bool IsObserver => Slot == null;
			public bool IsBot => Bot != null;

			// Linked to the online player database
			public string Fingerprint;

			public MiniYamlNode Serialize()
			{
				return new MiniYamlNode($"Client@{Index}", FieldSaver.Save(this));
			}
		}

		public class Slot
		{
			public string PlayerReference; // PlayerReference to bind against.
			public bool Closed; // Host has explicitly closed this slot.

			public bool AllowBots;
			public bool LockFaction;
			public bool LockColor;
			public bool LockTeam;
			public bool LockHandicap;
			public bool LockSpawn;
			public bool Required;

			public static Slot Deserialize(MiniYaml data)
			{
				return FieldLoader.Load<Slot>(data);
			}

			public MiniYamlNode Serialize()
			{
				return new MiniYamlNode($"Slot@{PlayerReference}", FieldSaver.Save(this));
			}
		}

		public class LobbyOptionState
		{
			public string Value;
			public string PreferredValue;

			public bool IsLocked;
			public bool IsEnabled => Value == "True";
		}

		[Flags]
		public enum MapStatus
		{
			Unknown = 0,
			Validating = 1,
			Playable = 2,
			Incompatible = 4,
			UnsafeCustomRules = 8,
		}

		public class Global
		{
			public string ServerName;
			public string Map;
			public MapStatus MapStatus;
			public int RandomSeed = 0;
			public bool AllowSpectators = true;
			public string GameUid;
			public bool EnableSingleplayer;
			public bool EnableSyncReports;
			public bool Dedicated;
			public bool GameSavesEnabled;

			// 120ms network frame interval for 40ms local tick
			public int NetFrameInterval = 3;

			[FieldLoader.Ignore]
			public Dictionary<string, LobbyOptionState> LobbyOptions = new Dictionary<string, LobbyOptionState>();

			public static Global Deserialize(MiniYaml data)
			{
				var gs = FieldLoader.Load<Global>(data);

				var optionsNode = data.Nodes.FirstOrDefault(n => n.Key == "Options");
				if (optionsNode != null)
					foreach (var n in optionsNode.Value.Nodes)
						gs.LobbyOptions[n.Key] = FieldLoader.Load<LobbyOptionState>(n.Value);

				return gs;
			}

			public MiniYamlNode Serialize()
			{
				var data = new MiniYamlNode("GlobalSettings", FieldSaver.Save(this));
				var options = LobbyOptions.Select(kv => new MiniYamlNode(kv.Key, FieldSaver.Save(kv.Value))).ToList();
				data.Value.Nodes.Add(new MiniYamlNode("Options", new MiniYaml(null, options)));
				return data;
			}

			public bool OptionOrDefault(string id, bool def)
			{
				if (LobbyOptions.TryGetValue(id, out var option))
					return option.IsEnabled;

				return def;
			}

			public string OptionOrDefault(string id, string def)
			{
				if (LobbyOptions.TryGetValue(id, out var option))
					return option.Value;

				return def;
			}
		}

		public string Serialize()
		{
			var sessionData = new List<MiniYamlNode>()
			{
				new MiniYamlNode("DisabledSpawnPoints", FieldSaver.FormatValue(DisabledSpawnPoints))
			};

			foreach (var client in Clients)
				sessionData.Add(client.Serialize());

			foreach (var slot in Slots)
				sessionData.Add(slot.Value.Serialize());

			sessionData.Add(GlobalSettings.Serialize());

			return sessionData.WriteToString();
		}
	}
}
