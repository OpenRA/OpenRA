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
using OpenRA.Graphics;

namespace OpenRA.Network
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();

		// Keyed by the PlayerReference id that the slot corresponds to
		public Dictionary<string, Slot> Slots = new Dictionary<string, Slot>();

		public Global GlobalSettings = new Global();

		public static Session Deserialize(string data)
		{
			try
			{
				var session = new Session();

				var ys = MiniYaml.FromString(data);
				foreach (var y in ys)
				{
					var yy = y.Key.Split('@');

					switch (yy[0])
					{
						case "GlobalSettings":
							FieldLoader.Load(session.GlobalSettings, y.Value);
							break;

							case "Client":
							session.Clients.Add(FieldLoader.Load<Client>(y.Value));
							break;

							case "Slot":
							var s = FieldLoader.Load<Slot>(y.Value);
							session.Slots.Add(s.PlayerReference, s);
							break;
					}
				}

				return session;
			}
			catch (InvalidOperationException)
			{
				Log.Write("exception", "Session deserialized invalid MiniYaml:\n{0}".F(data));
				throw;
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

		public bool IsSinglePlayer
		{
			get { return Clients.Count(c => c.Bot == null) == 1; }
		}

		public enum ClientState { NotReady, Invalid, Ready, Disconnected = 1000 }

		public class Client
		{
			public int Index;
			public HSLColor PreferredColor; // Color that the client normally uses from settings.yaml.
			public HSLColor Color; // Actual color that the client is using.
								   // Usually the same as PreferredColor but can be different on maps with locked colors.
			public string Country;
			public int SpawnPoint;
			public string Name;
			public string IpAddress;
			public ClientState State = ClientState.Invalid;
			public int Team;
			public string Slot;	// slot ID, or null for observer
			public string Bot; // Bot type, null for real clients
			public int BotControllerClientIndex; // who added the bot to the slot
			public bool IsAdmin;
			public bool IsReady { get { return State == ClientState.Ready; } }
			public bool IsInvalid { get { return State == ClientState.Invalid; } }
			public bool IsObserver { get { return Slot == null; } }
			public int Latency = -1;
			public int LatencyJitter = -1;
			public int[] LatencyHistory = { };
		}

		public class Slot
		{
			public string PlayerReference;	// playerReference to bind against.
			public bool Closed;	// host has explicitly closed this slot.

			public bool AllowBots;
			public bool LockRace;
			public bool LockColor;
			public bool LockTeam;
			public bool LockSpawn;
			public bool Required;
		}

		public class Global
		{
			public string ServerName;
			public string Map;
			public int OrderLatency = 3; // net tick frames (x 120 = ms)
			public int RandomSeed = 0;
			public bool FragileAlliances = false; // Allow diplomatic stance changes after game start.
			public bool AllowCheats = false;
			public bool AllowSpectators = true;
			public bool Dedicated;
			public string Difficulty;
			public bool Crates = true;
			public bool Shroud = true;
			public bool Fog = true;
			public bool AllyBuildRadius = true;
			public int StartingCash = 5000;
			public string StartingUnitsClass = "none";
			public bool AllowVersionMismatch;
			public string GameUid;
		}

		public string Serialize()
		{
			var clientData = new List<MiniYamlNode>();

			foreach (var client in Clients)
				clientData.Add(new MiniYamlNode("Client@{0}".F(client.Index), FieldSaver.Save(client)));

			foreach (var slot in Slots)
				clientData.Add(new MiniYamlNode("Slot@{0}".F(slot.Key), FieldSaver.Save(slot.Value)));

			clientData.Add(new MiniYamlNode("GlobalSettings", FieldSaver.Save(GlobalSettings)));

			return clientData.WriteToString();
		}
	}
}
