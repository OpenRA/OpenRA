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
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Primitives;

namespace OpenRA.Server
{
	public class ReplayViewer
	{
		class Chunk
		{
			public int Frame;
			public List<Pair<int, byte[]>> Packets = new List<Pair<int, byte[]>>();
		}

		Queue<Chunk> chunks = new Queue<Chunk>();
		int ordersFrame;

		public bool ReplayDone { get { return chunks.Count == 0 && IsValid; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }
		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;
		public readonly ReplayMetadata Info;
		public readonly Dictionary<int, int> IndexConverter;

		public ReplayViewer(string replayFilename)
		{
			Info = ReplayMetadata.Read(replayFilename);
			IndexConverter = new Dictionary<int, int>();
			Info.GameInfo.Players.Do(p => IndexConverter.Add(p.ClientIndex, p.ClientIndex));

			var lastChunk = new Chunk();

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

					if (chunks.Any() && chunk.Packets.Any(p2 => p2.First == client))
					{
						var packet2 = chunk.Packets.First(p2 => p2.First == client);

						if (BitConverter.ToInt32(packet2.Second, 0) == frame)
						{
							//Deleting ClientID from the packet since we add a new one on the server
							byte[] newpacket = new byte[packet.Length - 4];
							Buffer.BlockCopy(packet, 4, newpacket, 0, newpacket.Length);

							chunk.Packets.Remove(packet2);
							var array = new byte[packet2.Second.Length + newpacket.Length];
							Buffer.BlockCopy(packet2.Second, 0, array, 0, packet2.Second.Length);
							Buffer.BlockCopy(newpacket, 0, array, packet2.Second.Length, newpacket.Length);
							chunk.Packets.Add(Pair.New(client, array));
						}
						else
							chunk.Packets.Add(Pair.New(client, packet));
					}
					else
						chunk.Packets.Add(Pair.New(client, packet));


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
						if (lastChunk.Packets.Count != 0)
							chunks.Enqueue(lastChunk);
						lastChunk = chunk;
						chunk = new Chunk();

						TickCount = Math.Max(TickCount, frame);
					}
				}
			}

			//Remove last ping of replay since we will be sending an order instead
			lastChunk.Packets.Remove(lastChunk.Packets.First(p => BitConverter.ToInt32(p.Second, 0) == lastChunk.Frame));
			chunks.Enqueue(lastChunk);
			ordersFrame = LobbyInfo.GlobalSettings.OrderLatency;
		}

		internal List<Pair<int, byte[]>> GetNextData(int frame)
		{
			var packets = new List<Pair<int, byte[]>>();

			while (chunks.Count != 0 && chunks.Peek().Frame <= frame)
				packets.AddRange(chunks.Dequeue().Packets);

			packets.Do(pair => pair.First = IndexConverter[pair.First]);

			return packets;
		}
	}
}
