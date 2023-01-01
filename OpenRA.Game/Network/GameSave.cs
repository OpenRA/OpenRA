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
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Primitives;
using OpenRA.Server;

namespace OpenRA.Network
{
	public class SlotClient
	{
		public readonly Color Color;
		public readonly string Faction;
		public readonly int SpawnPoint;
		public readonly int Team;
		public readonly int Handicap;
		public readonly string Slot;
		public readonly string Bot;
		public readonly bool IsAdmin;

		public readonly string BotName;

		public SlotClient() { }

		public SlotClient(Session.Client client)
		{
			Color = client.Color;
			Faction = client.Faction;
			SpawnPoint = client.SpawnPoint;
			Team = client.Team;
			Handicap = client.Handicap;
			Slot = client.Slot;
			Bot = client.Bot;
			IsAdmin = client.IsAdmin;

			if (client.Bot != null)
				BotName = client.Name;
		}

		public void ApplyTo(Session.Client client)
		{
			client.Color = Color;
			client.Faction = Faction;
			client.SpawnPoint = SpawnPoint;
			client.Team = Team;
			client.Handicap = Handicap;
			client.Slot = Slot;
			client.Bot = Bot;
			client.IsAdmin = IsAdmin;

			if (Bot != null)
				client.Name = BotName;
		}

		public static SlotClient Deserialize(MiniYaml data)
		{
			return FieldLoader.Load<SlotClient>(data);
		}

		public MiniYamlNode Serialize(string key)
		{
			return new MiniYamlNode($"SlotClient@{key}", FieldSaver.Save(this));
		}
	}

	public class GameSave
	{
		public const int EOFMarker = -2;
		public const int MetadataMarker = -1;
		public const int TraitDataMarker = -3;

		readonly MemoryStream ordersStream = new MemoryStream();

		// Loaded from file and updated during gameplay
		public int LastOrdersFrame { get; private set; }
		public int LastSyncFrame { get; private set; }
		byte[] lastSyncPacket = Array.Empty<byte>();

		// Loaded from file or set on game start
		public Session.Global GlobalSettings { get; private set; }
		public Dictionary<string, Session.Slot> Slots { get; private set; }
		public Dictionary<string, SlotClient> SlotClients { get; private set; }
		public Dictionary<int, MiniYaml> TraitData = new Dictionary<int, MiniYaml>();

		// Set on game start
		int[] clientsBySlotIndex = Array.Empty<int>();
		int firstBotSlotIndex = -1;

		public GameSave()
		{
			LastOrdersFrame = -1;
			Slots = new Dictionary<string, Session.Slot>();
		}

		public GameSave(string filepath)
		{
			using (var rs = File.OpenRead(filepath))
			{
				rs.Seek(-12, SeekOrigin.End);
				var metadataOffset = rs.ReadInt32();
				var traitDataOffset = rs.ReadInt32();
				if (rs.ReadInt32() != EOFMarker)
					throw new InvalidDataException("Invalid orasav file");

				rs.Seek(metadataOffset, SeekOrigin.Begin);
				if (rs.ReadInt32() != MetadataMarker)
					throw new InvalidDataException("Invalid orasav file");

				LastOrdersFrame = rs.ReadInt32();
				LastSyncFrame = rs.ReadInt32();
				lastSyncPacket = rs.ReadBytes(Order.SyncHashOrderLength);

				var globalSettings = MiniYaml.FromString(rs.ReadString(Encoding.UTF8, Connection.MaxOrderLength));
				GlobalSettings = Session.Global.Deserialize(globalSettings[0].Value);

				var slots = MiniYaml.FromString(rs.ReadString(Encoding.UTF8, Connection.MaxOrderLength));
				Slots = new Dictionary<string, Session.Slot>();
				foreach (var s in slots)
				{
					var slot = Session.Slot.Deserialize(s.Value);
					Slots.Add(slot.PlayerReference, slot);
				}

				var slotClients = MiniYaml.FromString(rs.ReadString(Encoding.UTF8, Connection.MaxOrderLength));
				SlotClients = new Dictionary<string, SlotClient>();
				foreach (var s in slotClients)
				{
					var slotClient = SlotClient.Deserialize(s.Value);
					SlotClients.Add(slotClient.Slot, slotClient);
				}

				if (rs.Position != traitDataOffset || rs.ReadInt32() != TraitDataMarker)
					throw new InvalidDataException("Invalid orasav file");

				var traitData = MiniYaml.FromString(rs.ReadString(Encoding.UTF8, Connection.MaxOrderLength));
				foreach (var td in traitData)
					TraitData.Add(int.Parse(td.Key), td.Value);

				rs.Seek(0, SeekOrigin.Begin);
				ordersStream.Write(rs.ReadBytes(metadataOffset), 0, metadataOffset);
			}
		}

		public void StartGame(Session lobbyInfo, MapPreview map)
		{
			// Game orders are mapped from a client index to the slot that they occupy
			// Orders from spectators are ignored, which is not a problem in practice
			// because all immediate orders are also ignored
			clientsBySlotIndex = lobbyInfo.Slots.Keys.Select(s =>
			{
				var client = lobbyInfo.ClientInSlot(s);
				return client != null ? client.Index : -1;
			}).ToArray();

			// Perform a deep clone by round-tripping the data
			GlobalSettings = Session.Global.Deserialize(lobbyInfo.GlobalSettings.Serialize().Value);
			Slots = new Dictionary<string, Session.Slot>();
			SlotClients = new Dictionary<string, SlotClient>();
			foreach (var s in lobbyInfo.Slots)
			{
				Slots[s.Key] = Session.Slot.Deserialize(s.Value.Serialize().Value);

				var playerReference = map.Players.Players[s.Value.PlayerReference];
				var client = lobbyInfo.ClientInSlot(s.Key);

				// Only save the client state relevant to the game (faction, team, etc).
				// Admin and bot controller state is inherited and/or reassigned by the server at load time
				if (playerReference.Playable && client != null)
				{
					SlotClients[s.Key] = new SlotClient(client);

					// See HACK comment in DispatchOrders about reassigning bot orders
					if (client.Bot != null && firstBotSlotIndex < 0)
						firstBotSlotIndex = clientsBySlotIndex.IndexOf(client.Index);
				}
			}
		}

		public void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			// Sync packet - we only care about the last value
			if (data.Length > 0 && data[0] == (byte)OrderType.SyncHash && frame > LastSyncFrame)
			{
				if (data.Length != Order.SyncHashOrderLength)
				{
					Log.Write("debug", $"Dropped sync order with length {data.Length}. Expected length {Order.SyncHashOrderLength}.");
					return;
				}

				LastSyncFrame = frame;
				lastSyncPacket = data;
			}

			if (frame <= LastOrdersFrame)
				return;

			// Ignore immediate orders
			if (data.Length > 0 && data[0] == 0xFE)
				return;

			var clientSlot = clientsBySlotIndex.IndexOf(conn.PlayerIndex);

			// Handle orders that were sent by spectators
			if (clientSlot == -1)
			{
				// HACK: Assume that this is a bot order sent by its controller client
				// who is a spectator. The network data doesn't contain enough information
				// for us to confirm this, or to know which bot this is supposed to belong to...
				//
				// For skirmish games it is sufficient to map everything to the first bot,
				// because even if the bot choice is wrong, the bot-to-client remapping in ParseOrders
				// will give the right client as there is only one human client to choose from!
				// TODO: This will need to be fixed properly before implementing multiplayer saves
				clientSlot = firstBotSlotIndex;
			}

			ordersStream.WriteArray(BitConverter.GetBytes(data.Length + 8));
			ordersStream.WriteArray(BitConverter.GetBytes(frame));
			ordersStream.WriteArray(BitConverter.GetBytes(clientSlot));
			ordersStream.WriteArray(data);
			LastOrdersFrame = frame;
		}

		public void ParseOrders(Session lobbyInfo, Action<int, int, byte[]> packetFn)
		{
			// Send the trait data first to guarantee that it is available when needed
			foreach (var kv in TraitData)
			{
				var data = new List<MiniYamlNode>() { new MiniYamlNode(kv.Key.ToString(), kv.Value) }.WriteToString();
				packetFn(0, 0, Order.FromTargetString("SaveTraitData", data, true).Serialize());
			}

			ordersStream.Seek(0, SeekOrigin.Begin);
			while (ordersStream.Position < ordersStream.Length)
			{
				var dataLength = ordersStream.ReadInt32() - 8;
				var frame = ordersStream.ReadInt32();
				var slot = ordersStream.ReadInt32();
				var data = ordersStream.ReadBytes(dataLength);

				// Remap bot orders to their controller client
				var clientIndex = clientsBySlotIndex[slot];
				var client = lobbyInfo.ClientWithIndex(clientIndex);
				if (client.Bot != null)
					clientIndex = client.BotControllerClientIndex;

				packetFn(frame, clientIndex, data);
			}

			// Send sync hash to validate restore
			packetFn(LastSyncFrame, 0, lastSyncPacket);
		}

		public void AddTraitData(int traitIndex, MiniYaml data)
		{
			TraitData[traitIndex] = data;
		}

		public void Save(string path)
		{
			// File format:
			// - List of orders in network frame format
			// - Metadata start marker
			//   - Last frame number containing orders (int32)
			//   - Last frame number containing sync hash (int32)
			//   - Last sync packet (5 x byte)
			//   - Lobby global settings (yaml)
			//   - Lobby slots (yaml)
			//   - Lobby slot-client data (yaml)
			// - Trait data start marker
			//   - Custom trait yaml
			// - File offset of metadata start marker
			// - File offset of custom trait data
			// - Metadata end marker
			var file = File.Create(path);

			ordersStream.Seek(0, SeekOrigin.Begin);
			ordersStream.CopyTo(file);
			file.Write(BitConverter.GetBytes(MetadataMarker), 0, 4);
			file.Write(BitConverter.GetBytes(LastOrdersFrame), 0, 4);
			file.Write(BitConverter.GetBytes(LastSyncFrame), 0, 4);
			file.Write(lastSyncPacket, 0, Order.SyncHashOrderLength);

			var globalSettingsNodes = new List<MiniYamlNode>() { GlobalSettings.Serialize() };
			file.WriteString(Encoding.UTF8, globalSettingsNodes.WriteToString());

			var slotNodes = Slots
				.Select(s => s.Value.Serialize())
				.ToList();
			file.WriteString(Encoding.UTF8, slotNodes.WriteToString());

			var slotClientNodes = SlotClients
				.Select(s => s.Value.Serialize(s.Key))
				.ToList();
			file.WriteString(Encoding.UTF8, slotClientNodes.WriteToString());

			var traitDataOffset = file.Length;
			file.Write(BitConverter.GetBytes(TraitDataMarker), 0, 4);

			var traitDataNodes = TraitData
				.Select(kv => new MiniYamlNode(kv.Key.ToString(), kv.Value))
				.ToList();
			file.WriteString(Encoding.UTF8, traitDataNodes.WriteToString());

			file.Write(BitConverter.GetBytes(ordersStream.Length), 0, 4);
			file.Write(BitConverter.GetBytes(traitDataOffset), 0, 4);
			file.Write(BitConverter.GetBytes(EOFMarker), 0, 4);
		}
	}
}
