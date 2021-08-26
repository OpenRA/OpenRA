#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
using System.Net;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	public sealed class ReplayConnection : IConnection
	{
		class Chunk
		{
			public int Frame;
			public (int ClientId, byte[] Packet)[] Packets;
		}

		readonly Queue<Chunk> chunks = new Queue<Chunk>();
		readonly Queue<(int Frame, int SyncHash, ulong DefeatState)> sync = new Queue<(int, int, ulong)>();
		readonly Dictionary<int, int> lastClientsFrame = new Dictionary<int, int>();
		readonly int orderLatency;
		int ordersFrame;

		public int LocalClientId => -1;

		public readonly int TickCount;
		public readonly int FinalGameTick;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;
		public readonly string Filename;

		public ReplayConnection(string replayFilename)
		{
			Filename = replayFilename;
			FinalGameTick = ReplayMetadata.Read(replayFilename).GameInfo.FinalGameTick;

			// Parse replay data into a struct that can be fed to the game in chunks
			// to avoid issues with all immediate orders being resolved on the first tick.
			using (var rs = File.OpenRead(replayFilename))
			{
				var packets = new List<(int ClientId, byte[] Packet)>();
				var chunk = new Chunk();
				while (rs.Position < rs.Length)
				{
					var client = rs.ReadInt32();
					if (client == ReplayMetadata.MetaStartMarker)
						break;

					var packetLen = rs.ReadInt32();
					var packet = rs.ReadBytes(packetLen);
					var frame = BitConverter.ToInt32(packet, 0);
					packets.Add((client, packet));

					if (frame != int.MaxValue && (!lastClientsFrame.ContainsKey(client) || frame > lastClientsFrame[client]))
						lastClientsFrame[client] = frame;

					if (packet.Length > 4 && (packet[4] == (byte)OrderType.Disconnect || packet[4] == (byte)OrderType.SyncHash))
						continue;

					if (frame == 0)
					{
						// Parse replay metadata from orders stream
						if (OrderIO.TryParseOrderPacket(packet, out _, out var orders))
						{
							foreach (var o in orders.GetOrders(null))
							{
								if (o.OrderString == "StartGame")
									IsValid = true;
								else if (o.OrderString == "SyncInfo" && !IsValid)
									LobbyInfo = Session.Deserialize(o.TargetString);
							}
						}
					}
					else
					{
						// Regular order - finalize the chunk
						chunk.Frame = frame;
						chunk.Packets = packets.ToArray();
						packets.Clear();
						chunks.Enqueue(chunk);
						chunk = new Chunk();

						TickCount = Math.Max(TickCount, frame);
					}
				}

				var lastClientToDisconnect = lastClientsFrame.MaxBy(kvp => kvp.Value).Key;

				// 2nd parse : replace all disconnect packets without frame with real
				// disconnect frame
				// NOTE: to modify/remove if a reconnect feature is set
				foreach (var tmpChunk in chunks)
				{
					foreach (var tmpPacketPair in tmpChunk.Packets)
					{
						var client = tmpPacketPair.ClientId;

						// Don't replace the final disconnection packet - we still want this to end the replay.
						if (client == lastClientToDisconnect)
							continue;

						var packet = tmpPacketPair.Packet;
						if (packet.Length == Order.DisconnectOrderLength + 4 && packet[4] == (byte)OrderType.Disconnect)
						{
							var lastClientFrame = lastClientsFrame[client];
							var lastFramePacket = BitConverter.GetBytes(lastClientFrame);
							Array.Copy(lastFramePacket, packet, lastFramePacket.Length);
						}
					}
				}
			}

			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();
			var gameSpeedName = LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);
			orderLatency = gameSpeeds.Speeds[gameSpeedName].OrderLatency;
			ordersFrame = orderLatency;
		}

		// Do nothing: ignore locally generated orders
		public void Send(int frame, IEnumerable<Order> orders) { }
		public void SendImmediate(IEnumerable<Order> orders) { }

		public void SendSync(int frame, int syncHash, ulong defeatState)
		{
			sync.Enqueue((frame, syncHash, defeatState));

			// Store the current frame so Receive() can return the next chunk of orders.
			ordersFrame = frame + orderLatency;
		}

		public void Receive(OrderManager orderManager)
		{
			while (sync.Count != 0)
			{
				var (syncFrame, syncHash, defeatState) = sync.Dequeue();
				orderManager.ReceiveSync(syncFrame, syncHash, defeatState);
			}

			while (chunks.Count != 0 && chunks.Peek().Frame <= ordersFrame)
			{
				foreach (var o in chunks.Dequeue().Packets)
				{
					if (OrderIO.TryParseDisconnect(o.Packet, out var disconnectClient))
						orderManager.ReceiveDisconnect(disconnectClient);
					else if (OrderIO.TryParseSync(o.Packet, out var syncFrame, out var syncHash, out var defeatState))
						orderManager.ReceiveSync(syncFrame, syncHash, defeatState);
					else if (OrderIO.TryParseOrderPacket(o.Packet, out var frame, out var orders))
					{
						if (frame == 0)
							orderManager.ReceiveImmediateOrders(o.ClientId, orders);
						else
							orderManager.ReceiveOrders(o.ClientId, frame, orders);
					}
				}
			}
		}

		public void Dispose() { }
	}
}
