#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		readonly int orderLatency;

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

					if (packet.Length > 4 && (packet[4] == (byte)OrderType.Disconnect || packet[4] == (byte)OrderType.SyncHash))
						continue;

					if (frame == 0)
					{
						// Parse replay metadata from orders stream
						if (OrderIO.TryParseOrderPacket(packet, out var orders))
						{
							foreach (var o in orders.Orders.GetOrders(null))
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
			}

			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();
			var gameSpeedName = LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);
			orderLatency = gameSpeeds.Speeds[gameSpeedName].OrderLatency;
		}

		void IConnection.StartGame() { }

		// Do nothing: ignore locally generated orders
		void IConnection.Send(int frame, IEnumerable<Order> orders) { }
		void IConnection.SendImmediate(IEnumerable<Order> orders) { }

		void IConnection.SendSync(int frame, int syncHash, ulong defeatState)
		{
			sync.Enqueue((frame, syncHash, defeatState));
		}

		void IConnection.Receive(OrderManager orderManager)
		{
			while (sync.Count != 0)
				orderManager.ReceiveSync(sync.Dequeue());

			while (chunks.Count != 0 && chunks.Peek().Frame <= orderManager.NetFrameNumber + orderLatency)
			{
				foreach (var o in chunks.Dequeue().Packets)
				{
					if (OrderIO.TryParseDisconnect(o, out var disconnect))
						orderManager.ReceiveDisconnect(disconnect.ClientId, disconnect.Frame);
					else if (OrderIO.TryParseSync(o.Packet, out var sync))
						orderManager.ReceiveSync(sync);
					else if (OrderIO.TryParseOrderPacket(o.Packet, out var orders))
					{
						if (orders.Frame == 0)
							orderManager.ReceiveImmediateOrders(o.ClientId, orders.Orders);
						else
							orderManager.ReceiveOrders(o.ClientId, orders);
					}
					else
						throw new InvalidDataException($"Received unknown packet from client {o.ClientId} with length {o.Packet.Length}");
				}
			}
		}

		int IConnection.LocalClientId => -1;

		void IDisposable.Dispose() { }
	}
}
