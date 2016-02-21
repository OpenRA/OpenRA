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
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public sealed class ReplayConnection : IConnection
	{
		class Chunk
		{
			public int Frame;
			public List<Pair<int, byte[]>> Packets = new List<Pair<int, byte[]>>();
		}

		Queue<Chunk> chunks = new Queue<Chunk>();
		List<byte[]> sync = new List<byte[]>();
		int ordersFrame;
		Dictionary<int, int> lastClientsFrame = new Dictionary<int, int>();

		public int LocalClientId { get { return 0; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }
		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;
		public readonly string Filename;

		public ReplayConnection(string replayFilename)
		{
			Filename = replayFilename;

			// Parse replay data into a struct that can be fed to the game in chunks
			// to avoid issues with all immediate orders being resolved on the first tick.
			using (var rs = File.OpenRead(replayFilename))
			{
				var chunk = new Chunk();

				while (rs.Position < rs.Length)
				{
					var client = rs.ReadInt32();
					if (client == ReplayMetadata.MetaStartMarker)
						break;
					var packetLen = rs.ReadInt32();
					var packet = rs.ReadBytes(packetLen);
					var frame = BitConverter.ToInt32(packet, 0);
					chunk.Packets.Add(Pair.New(client, packet));

					if (frame != int.MaxValue &&
						(!lastClientsFrame.ContainsKey(client) || frame > lastClientsFrame[client]))
						lastClientsFrame[client] = frame;

					if (packet.Length == 5 && packet[4] == 0xBF)
						continue; // disconnect
					else if (packet.Length >= 5 && packet[4] == 0x65)
						continue; // sync
					else if (frame == 0)
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
						var client = tmpPacketPair.First;

						// Don't replace the final disconnection packet - we still want this to end the replay.
						if (client == lastClientToDisconnect)
							continue;

						var packet = tmpPacketPair.Second;
						if (packet.Length == 5 && packet[4] == 0xBF)
						{
							var lastClientFrame = lastClientsFrame[client];
							var lastFramePacket = BitConverter.GetBytes(lastClientFrame);
							Array.Copy(lastFramePacket, packet, lastFramePacket.Length);
						}
					}
				}
			}

			ordersFrame = LobbyInfo.GlobalSettings.OrderLatency;
		}

		// Do nothing: ignore locally generated orders
		public void Send(int frame, List<byte[]> orders) { }
		public void SendImmediate(List<byte[]> orders) { }

		public void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			ms.Write(syncData);
			sync.Add(ms.ToArray());

			// Store the current frame so Receive() can return the next chunk of orders.
			ordersFrame = frame + LobbyInfo.GlobalSettings.OrderLatency;
		}

		public void Receive(Action<int, byte[]> packetFn)
		{
			while (sync.Count != 0)
			{
				packetFn(LocalClientId, sync[0]);
				sync.RemoveAt(0);
			}

			while (chunks.Count != 0 && chunks.Peek().Frame <= ordersFrame)
				foreach (var o in chunks.Dequeue().Packets)
					packetFn(o.First, o.Second);
		}

		public void Dispose() { }
	}
}
