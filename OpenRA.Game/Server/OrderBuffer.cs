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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Server
{
	/*
	 * OpenRA orders do not need to be frame-specific, as long as each client resolves the same orders in the same order
	 * they should be able to maintain a deterministic simulation with a mostly expected result.
	 *
	 * This allows the server to act as a dumb "master order synchronizer", simply acting as the authority and central
	 * point of synchronization on which orders to resolve each frame.
	 *
	 * The buffer is fairly simple, it maintains a list of all incoming order packets, minus their associated frames,
	 * so that all orders can be batched for a single frame to all clients at a time of the server's choosing.
	 *
	 * A mechanism for clients to inform/request slowdown should be provided so that clients which are overloaded do
	 * not fall behind indefinitely.
	 */
	public class OrderBuffer
	{
		// Maintaining a queue of the serialized order lists prevents us from having to deserialize the orders
		// or copy them on receive. They should be able to be written the TCP stream without being copied.
		// The count of the byte[] queue is used as the number of frames to ack.
		// Since the connection is reliable, clients will understand what the ack count means.
		readonly Dictionary<int, List<byte[]>> clientOrdersBuffer = new Dictionary<int, List<byte[]>>();

		public void BufferOrders(int client, byte[] serializedOrderList)
		{
			if (!clientOrdersBuffer.TryGetValue(client, out var orderQueue))
				throw new InvalidOperationException("Tried to buffer orders for client that wasn't added");

			orderQueue.Add(serializedOrderList);
		}

		public void AddClient(int client)
		{
			clientOrdersBuffer.Add(client, new List<byte[]>());
		}

		public void DropClient(int client)
		{
			clientOrdersBuffer.Remove(client);
		}

		// Calls the given action with all necessary communications in the form:
		// From, To, EnumerableData
		// Then clears the buffer
		// TODO allow server to optionally store buffered frames and enable client joins and re-connects
		public void DispatchOrders(IFrameOrderDispatcher dispatcher)
		{
			foreach (var fromPair in clientOrdersBuffer)
			{
				var fromClient = fromPair.Key;
				var orders = fromPair.Value;

				// Ack the frames sent to be applied on this frame
				dispatcher.DispatchBufferedOrderAcks(fromClient, orders.Count);

				// Send each client's order buffer (because they were queued, order is preserved)
				dispatcher.DispatchBufferedOrdersToOtherClients(fromClient, orders);

				orders.Clear();
			}
		}
	}

	public interface IFrameOrderDispatcher
	{
		void DispatchBufferedOrdersToOtherClients(int fromClient, List<byte[]> allData);
		void DispatchBufferedOrderAcks(int forClient, int ackCount);
	}
}
