#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

		Queue<Chunk> chunks = new Queue<Chunk>();
		Queue<byte[]> sync = new Queue<byte[]>();
		int ordersFrame;
		Dictionary<int, int> lastClientsFrame = new Dictionary<int, int>();

		public int LocalClientId { get { return -1; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }
		public IPEndPoint EndPoint
		{
			get { throw new NotSupportedException("A replay connection doesn't have an endpoint"); }
		}

		public string ErrorMessage { get { return null; } }

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

					if (packet.Length == 5 && packet[4] == (byte)OrderType.Disconnect)
						continue; // disconnect
					if (packet.Length >= 5 && packet[4] == (byte)OrderType.SyncHash)
						continue; // sync
					if (frame == 0)
					{
						// Parse replay metadata from orders stream
						var orders = packet.ToOrderList(null);
						foreach (var o in orders)
						{
							if (o.OrderString == "StartGame")
								IsValid = true;
							else if (o.OrderString == "SyncInfo" && !IsValid)
								LobbyInfo = Session.Deserialize(o.TargetString);
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

				// 2nd parse : replace all disconnect packets without frame with real
				// disconnect frame
				// NOTE: to modify/remove if a reconnect feature is set
				foreach (var tmpChunk in chunks)
				{
					foreach (var tmpPacketPair in tmpChunk.Packets)
					{
						var client = tmpPacketPair.ClientId;

						// Don't replace the final disconnection packet - we still want this to end the replay.
						// TODO Ensure that this is not necessary
						/*if (client == lastClientToDisconnect)
							continue;*/

						var packet = tmpPacketPair.Packet;
						if (packet.Length == 5 && packet[4] == (byte)OrderType.Disconnect)
						{
							var lastClientFrame = lastClientsFrame[client];
							var lastFramePacket = BitConverter.GetBytes(lastClientFrame);
							Array.Copy(lastFramePacket, packet, lastFramePacket.Length);
						}
					}
				}
			}

			ordersFrame = LobbyInfo.GlobalSettings.OrderLatency; // TODO Fix when adaptive order latency is on
		}

		// Do nothing: ignore locally generated orders
		public void Send(int frame, IEnumerable<byte[]> orders) { }
		public void SendImmediate(IEnumerable<byte[]> orders) { }

		// TODO: Fix this HACK
		// so that replays are not dependent on SendSync for their timing and we can optionally reduce Sync rate
		public void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream(4 + syncData.Length);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(syncData);
			sync.Enqueue(ms.GetBuffer());

			// Store the current frame so Receive() can return the next chunk of orders.
			ordersFrame = frame + 1;
		}

		public void Receive(Action<int, byte[]> packetFn)
		{
			while (sync.Count != 0)
				packetFn(LocalClientId, sync.Dequeue());

			while (chunks.Count != 0 && chunks.Peek().Frame <= ordersFrame)
				foreach (var o in chunks.Dequeue().Packets)
					packetFn(o.ClientId, o.Packet);

			// Stream ended, disconnect everyone
			if (chunks.Count == 0)
			{
				var disconnectPacket = new byte[] { 0, 0, 0, 0, (byte)OrderType.Disconnect };
				foreach (var client in LobbyInfo.Clients)
					packetFn(client.Index, disconnectPacket);
			}
		}

		public void Dispose() { }
	}
}
