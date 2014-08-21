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
	public class ReplayParser
	{
		public List<Pair<int, byte[]>> Packets = new List<Pair<int, byte[]>>();

		public bool ReplayDone { get { return Packets.Count == 0 && IsValid; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }

		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly ReplayMetadata Info;
		public readonly Dictionary<int, int> IndexConverter;
		public readonly bool Resume;

		public ReplayParser(string replayFilename, bool resume)
		{
			Info = ReplayMetadata.Read(replayFilename);
			IndexConverter = new Dictionary<int, int>();
			Resume = resume;

			var ignore = new List<byte[]> {
				new ServerOrder("Simulate", "Enable").Serialize(),
				new ServerOrder("Simulate", "Disable").Serialize() };

			var clients = new List<int>();
			var skip = false;

			using (var rs = File.OpenRead(replayFilename))
			{
				while (rs.Position < rs.Length)
				{
					var client = rs.ReadInt32();
					if (client == ReplayMetadata.MetaStartMarker)
						break;
					var packetLen = rs.ReadInt32();
					var packet = rs.ReadBytes(packetLen);
					var frame = BitConverter.ToInt32(packet, 0);
					
					if (packet.Length == 5 && packet[4] == 0xBF)
						continue; // disconnect
					else if (packet.Length >= 5 && packet[4] == 0x65)
						continue; // sync
					else if (frame == 0)
					{
						var orders = packet.ToOrderList(null);
						foreach (var o in orders)
						{
							if (o.IsImmediate)
							{
								skip = true;
								if (o.OrderString == "StartGame")
									IsValid = true;
							}
						}

						if (skip)
						{
							skip = false;
							continue;
						}
					}

					foreach (var i in ignore)
						packet = CheckIgnore(i, packet);

					if (frame != 0 && Packets.Any(p2 => p2.First == client))
					{
						var packet2 = Packets.First(p2 => p2.First == client);

						if (BitConverter.ToInt32(packet2.Second, 0) == frame)
						{
							byte[] newpacket = new byte[packet.Length - 4];
							Buffer.BlockCopy(packet, 4, newpacket, 0, newpacket.Length);

							Packets.Remove(packet2);
							Packets.Add(Pair.New(client, sumArrays(packet2.Second, newpacket)));
							continue;
						}
					}

					if (!clients.Contains(client))
					{
						IndexConverter.Add(client, client);
						clients.Add(client);
					}

					TickCount = Math.Max(TickCount, frame);
					Packets.Add(Pair.New(client, packet));
				}
			}

			Packets = Packets.OrderBy(p => BitConverter.ToInt32(p.Second, 0)).ToList();

			if (Resume)
			{
				//Remove last frame of replay since it is unneeded
				for (var i = Packets.Count - 1; i >= 0; i--)
				{
					if (BitConverter.ToInt32(Packets[i].Second, 0) != TickCount)
						break;
					Packets.Remove(Packets[i]);
				}

				var newPackets = new List<Pair<int, byte[]>>();

				var startOrders = BitConverter.GetBytes(0);
				startOrders = sumArrays(startOrders, new ServerOrder("Simulate", "Enable").Serialize());
				newPackets.Add(new Pair<int, byte[]>(0, startOrders));

				var endOrders = BitConverter.GetBytes(TickCount);
				endOrders = sumArrays(endOrders, new ServerOrder("Simulate", "Disable").Serialize());
				endOrders = sumArrays(endOrders, new ServerOrder("PauseGame", "UnPause").Serialize());

				clients.Do(c =>
				{
					newPackets.Add(new Pair<int, byte[]>(c, endOrders));
				});

				newPackets.Do(p => Packets.Add(p));
			}

			Packets = Packets.OrderBy(p => BitConverter.ToInt32(p.Second, 0)).ToList();
		}

		byte[] CheckIgnore(byte[] ignore, byte[] packet)
		{
			var check = false;
			var startingIndex = 0;
			var x = 0;

			if (ignore.Length <= packet.Length - 4)
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

		internal List<Pair<int, byte[]>> GetData()
		{
			var CompressedPackets = new List<Pair<int, byte[]>>();

			for (var i = 0; i < Packets.Count; )
			{
				var client = IndexConverter[Packets[i].First];
				var frame = BitConverter.ToInt32(Packets[i].Second, 0);

				if (frame != 0 && CompressedPackets.Any(p2 => p2.First == client))
				{
					var packet2 = CompressedPackets.FirstOrDefault(p2 => p2.First == client && BitConverter.ToInt32(p2.Second, 0) == frame);

					if (packet2.Second != null)
					{
						byte[] newpacket = new byte[Packets[i].Second.Length - 4];
						Buffer.BlockCopy(Packets[i].Second, 4, newpacket, 0, newpacket.Length);

						CompressedPackets.Remove(packet2);
						CompressedPackets.Add(Pair.New(Packets[i].First, sumArrays(packet2.Second, newpacket)));
						Packets.Remove(Packets[i]);
						continue;
					}
				}

				CompressedPackets.Add(new Pair<int, byte[]>(client, Packets[i].Second));
				Packets.Remove(Packets[i]);
			}

			Packets = CompressedPackets.OrderBy(p => BitConverter.ToInt32(p.Second, 0)).ToList();
			return Packets;
		}
	}
}
