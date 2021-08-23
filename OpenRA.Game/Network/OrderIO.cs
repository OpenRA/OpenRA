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

namespace OpenRA.Network
{
	public class OrderPacket
	{
		readonly Order[] orders;
		readonly MemoryStream data;
		public OrderPacket(Order[] orders)
		{
			this.orders = orders;
			data = null;
		}

		public OrderPacket(MemoryStream data)
		{
			orders = null;
			this.data = data;
		}

		public IEnumerable<Order> GetOrders(World world)
		{
			return orders ?? ParseData(world);
		}

		IEnumerable<Order> ParseData(World world)
		{
			if (data == null)
				yield break;

			// Order deserialization depends on the current world state,
			// so must be deferred until we are ready to consume them.
			var reader = new BinaryReader(data);
			while (data.Position < data.Length)
			{
				var o = Order.Deserialize(world, reader);
				if (o != null)
					yield return o;
			}
		}

		public byte[] Serialize(int frame)
		{
			if (data != null)
				return data.ToArray();

			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frame));
			foreach (var o in orders)
				ms.WriteArray(o.Serialize());
			return ms.ToArray();
		}
	}

	public static class OrderIO
	{
		static readonly OrderPacket NoOrders = new OrderPacket(Array.Empty<Order>());

		public static byte[] SerializeSync(int frame, int syncHash, ulong defeatState)
		{
			var ms = new MemoryStream(4 + Order.SyncHashOrderLength);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteByte((byte)OrderType.SyncHash);
			ms.WriteArray(BitConverter.GetBytes(syncHash));
			ms.WriteArray(BitConverter.GetBytes(defeatState));
			return ms.GetBuffer();
		}

		public static byte[] SerializeOrders(int frame, IEnumerable<Order> orders)
		{
			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frame));
			foreach (var o in orders)
				ms.WriteArray(o.Serialize());
			return ms.ToArray();
		}

		public static bool TryParseDisconnect(byte[] packet, out int clientId)
		{
			if (packet.Length == Order.DisconnectOrderLength + 4 && packet[4] == (byte)OrderType.Disconnect)
			{
				clientId = BitConverter.ToInt32(packet, 5);
				return true;
			}

			clientId = 0;
			return false;
		}

		public static bool TryParseSync(byte[] packet, out int frame, out int syncHash, out ulong defeatState)
		{
			if (packet.Length != 4 + Order.SyncHashOrderLength || packet[4] != (byte)OrderType.SyncHash)
			{
				frame = syncHash = 0;
				defeatState = 0;
				return false;
			}

			frame = BitConverter.ToInt32(packet, 0);
			syncHash = BitConverter.ToInt32(packet, 5);
			defeatState = BitConverter.ToUInt64(packet, 9);
			return true;
		}

		public static bool TryParseOrderPacket(byte[] packet, out int frame, out OrderPacket orders)
		{
			// Not a valid packet
			if (packet.Length < 4)
			{
				frame = 0;
				orders = null;
				return false;
			}

			// Wrong packet type
			if (packet.Length >= 5 && (packet[4] == (byte)OrderType.Disconnect || packet[4] == (byte)OrderType.SyncHash))
			{
				frame = 0;
				orders = null;
				return false;
			}

			frame = BitConverter.ToInt32(packet, 0);

			// PERF: Skip empty order frames, often per client each frame
			orders = packet.Length > 4 ? new OrderPacket(new MemoryStream(packet, 4, packet.Length - 4)) : NoOrders;
			return true;
		}
	}
}
