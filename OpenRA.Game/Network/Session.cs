#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Network
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();
		public List<ClientPing> ClientPings = new List<ClientPing>();

		// Keyed by the PlayerReference id that the slot corresponds to
		public Dictionary<string, Slot> Slots = new Dictionary<string, Slot>();

		public Global GlobalSettings = new Global();

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

						case "ClientPing":
							session.ClientPings.Add(ClientPing.Deserialize(node.Value));
							break;

						case "GlobalSettings":
							session.GlobalSettings = Global.Deserialize(node.Value);
							break;

						case "Slot":
							var s = Slot.Deserialize(node.Value);
							session.Slots.Add(s.PlayerReference, s);
							break;
					}
				}

				return session;
			}
			catch (YamlException)
			{
				throw new YamlException("Session deserialized invalid MiniYaml:\n{0}".F(data));
			}
			catch (InvalidOperationException)
			{
				throw new YamlException("Session deserialized invalid MiniYaml:\n{0}".F(data));
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

		public bool IsSinglePlayer
		{
			get { return Clients.Count(c => c.Bot == null) == 1; }
		}

		public enum ClientState { NotReady, Invalid, Ready, Disconnected = 1000 }

		public class Client
		{
			public static Client Deserialize(MiniYaml data)
			{
				return FieldLoader.Load<Client>(data);
			}

			public int Index;
			public HSLColor PreferredColor; // Color that the client normally uses from settings.yaml.
			public HSLColor Color; // Actual color that the client is using. Usually the same as PreferredColor but can be different on maps with locked colors.
			public string Faction;
			public int SpawnPoint;
			public string Name;
			public string IpAddress;
			public ClientState State = ClientState.Invalid;
			public int Team;
			public string Slot;	// Slot ID, or null for observer
			public string Bot; // Bot type, null for real clients
			public int BotControllerClientIndex; // who added the bot to the slot
			public bool IsAdmin;
			public bool IsReady { get { return State == ClientState.Ready; } }
			public bool IsInvalid { get { return State == ClientState.Invalid; } }
			public bool IsObserver { get { return Slot == null; } }

			public MiniYamlNode Serialize()
			{
				return new MiniYamlNode("Client@{0}".F(Index), FieldSaver.Save(this));
			}
		}

		public ClientPing PingFromClient(Client client)
		{
			return ClientPings.SingleOrDefault(p => p.Index == client.Index);
		}

		public class ClientPing
		{
			public int Index;
			public long Latency = -1;
			public long LatencyJitter = -1;
			public long[] LatencyHistory = { };

			public static ClientPing Deserialize(MiniYaml data)
			{
				return FieldLoader.Load<ClientPing>(data);
			}

			public MiniYamlNode Serialize()
			{
				return new MiniYamlNode("ClientPing@{0}".F(Index), FieldSaver.Save(this));
			}
		}

		public class Slot
		{
			public string PlayerReference;	// PlayerReference to bind against.
			public bool Closed;	// Host has explicitly closed this slot.

			public bool AllowBots;
			public bool LockFaction;
			public bool LockColor;
			public bool LockTeam;
			public bool LockSpawn;
			public bool Required;

			public static Slot Deserialize(MiniYaml data)
			{
				return FieldLoader.Load<Slot>(data);
			}

			public MiniYamlNode Serialize()
			{
				return new MiniYamlNode("Slot@{0}".F(PlayerReference), FieldSaver.Save(this));
			}
		}

		public class LobbyOptionState
		{
			public bool Locked;
			public string Value;
			public string PreferredValue;

			public LobbyOptionState() { }

			public bool Enabled { get { return Value == "True"; } }
		}

		public class Global
		{
			public string ServerName;
			public string Map;
			public int Timestep = 40;
			public int OrderLatency = 3; // net tick frames (x 120 = ms)
			public int RandomSeed = 0;
			public bool AllowSpectators = true;
			public bool AllowVersionMismatch;
			public string GameUid;
			public bool EnableSingleplayer;

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
				LobbyOptionState option;
				if (LobbyOptions.TryGetValue(id, out option))
					return option.Enabled;

				return def;
			}

			public string OptionOrDefault(string id, string def)
			{
				LobbyOptionState option;
				if (LobbyOptions.TryGetValue(id, out option))
					return option.Value;

				return def;
			}
		}

		public string Serialize()
		{
			var sessionData = new List<MiniYamlNode>();

			foreach (var client in Clients)
				sessionData.Add(client.Serialize());

			foreach (var clientPing in ClientPings)
				sessionData.Add(clientPing.Serialize());

			foreach (var slot in Slots)
				sessionData.Add(slot.Value.Serialize());

			sessionData.Add(GlobalSettings.Serialize());

			return sessionData.WriteToString();
		}
	}
}
