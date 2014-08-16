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

		public bool ReplayDone { get { return chunks.Count == 0 && IsValid; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }

		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;
		public readonly ReplayMetadata Info;
		public readonly Dictionary<int, int> IndexConverter;
		public readonly bool Resume;

		public ReplayViewer(string replayFilename, bool resume)
		{
			Info = ReplayMetadata.Read(replayFilename);
			IndexConverter = new Dictionary<int, int>();
			Info.GameInfo.Players.Do(p => IndexConverter.Add(p.ClientIndex, p.ClientIndex));
			Resume = resume;

			var ignore = new byte[][] {
				new ServerOrder("SendPermission", "Enable").Serialize(),
				new ServerOrder("SendPermission", "DisableSim").Serialize(),
				new ServerOrder("SendPermission", "DisableNoSim").Serialize() };

			var clients = new List<int>();
			
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
					if (!clients.Contains(client))
						clients.Add(client);
					var packetLen = rs.ReadInt32();
					var packet = rs.ReadBytes(packetLen);
					var frame = BitConverter.ToInt32(packet, 0);

					foreach (var i in ignore)
						packet = CheckIgnore(i, packet);

					if (chunks.Any() && chunk.Packets.Any(p2 => p2.First == client))
					{
						var packet2 = chunk.Packets.First(p2 => p2.First == client);

						if (BitConverter.ToInt32(packet2.Second, 0) == frame)
						{
							//Deleting ClientID from the packet since we add a new one on the server
							byte[] newpacket = new byte[packet.Length - 4];
							Buffer.BlockCopy(packet, 4, newpacket, 0, newpacket.Length);

							chunk.Packets.Remove(packet2);
							chunk.Packets.Add(Pair.New(client, sumArrays(packet2.Second, newpacket)));
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

			if (Resume)
			{
				//Remove last ping of replay since the ping will come from the client
				lastChunk.Packets.Remove(lastChunk.Packets.First(p => BitConverter.ToInt32(p.Second, 0) == lastChunk.Frame));

				var newPackets = new List<Pair<int, byte[]>>();
				var extraOrders = BitConverter.GetBytes(0);
				extraOrders = sumArrays(extraOrders, new ServerOrder("SendPermission", "Enable").Serialize());
				extraOrders = sumArrays(extraOrders, new ServerOrder("PauseGame", "UnPause").Serialize());

				clients.Do(c =>
				{
					newPackets.Add(new Pair<int, byte[]>(c, extraOrders));
				});

				newPackets.Do(p => lastChunk.Packets.Add(p));
			}

			chunks.Enqueue(lastChunk);
		}

		byte[] CheckIgnore(byte[] ignore, byte[] packet)
		{
			var check = false;
			var startingIndex = 0;
			var x = 0;

			if (ignore.Length < packet.Length - 4)
				for (var i = 4; i < packet.Length; i++)
				{
					if (x < ignore.Length && i < packet.Length)
					{
						if (!check && ignore[x] == packet[i])
						{
							startingIndex = i;
							check = true;
						}
						else if (check && ignore[x] != packet[i])
						{
							x = 0;
							check = false;
						}
					}

					if (check)
						x++;
				}

			if (check)
			{
				var newPacket = new byte[packet.Length - ignore.Length];
				Buffer.BlockCopy(packet, 0, newPacket, 0, startingIndex);
				Buffer.BlockCopy(packet, startingIndex + ignore.Length, newPacket, startingIndex, packet.Length - (startingIndex + ignore.Length));
				return newPacket;
			}
			else
				return packet;
		}

		byte[] sumArrays(byte[] array1, byte[] array2)
		{
			var array = new byte[array1.Length + array2.Length];
			Buffer.BlockCopy(array1, 0, array, 0, array1.Length);
			Buffer.BlockCopy(array2, 0, array, array1.Length, array2.Length);
			return array;
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
