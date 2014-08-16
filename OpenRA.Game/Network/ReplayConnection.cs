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
using OpenRA.FileFormats;
using OpenRA.Primitives;
using OpenRA.Server;

namespace OpenRA.Network
{
	public sealed class ReplayConnection : IConnection
	{
		public bool DisableSend { get; set; }

		class Chunk
		{
			public int Frame;
			public List<Pair<int, byte[]>> Packets = new List<Pair<int, byte[]>>();
		}

		Queue<Chunk> chunks = new Queue<Chunk>();
		List<byte[]> sync = new List<byte[]>();
		int ordersFrame;

		public int LocalClientId { get { return 0; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }
		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;

		public ReplayConnection(string replayFilename)
		{
			var ignore = new byte[][] {
				new ServerOrder("SendPermission", "Enable").Serialize(),
				new ServerOrder("SendPermission", "DisableSim").Serialize(),
				new ServerOrder("SendPermission", "DisableNoSim").Serialize() };

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

					foreach (var i in ignore)
						packet = CheckIgnore(i, packet);

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
						chunks.Enqueue(chunk);
						chunk = new Chunk();

						TickCount = Math.Max(TickCount, frame);
					}
				}
			}

			ordersFrame = LobbyInfo.GlobalSettings.OrderLatency;
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
