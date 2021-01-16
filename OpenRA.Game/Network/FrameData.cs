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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Network
{
	class FrameData
	{
		public struct ClientOrder
		{
			public int Client;
			public Order Order;

			public override string ToString()
			{
				return "ClientId: {0} {1}".F(Client, Order);
			}
		}

		readonly HashSet<int> quitClients = new HashSet<int>();
		readonly Dictionary<int, Queue<byte[]>> framePackets = new Dictionary<int, Queue<byte[]>>();
		readonly Queue<int> timestepData = new Queue<int>();

		public IEnumerable<int> ClientsPlayingInFrame()
		{
			return framePackets.Keys.Where(x => !quitClients.Contains(x)).OrderBy(x => x);
		}

		public void AddClient(int clientId)
		{
			if (!framePackets.ContainsKey(clientId))
				framePackets.Add(clientId, new Queue<byte[]>());
		}

		public void ClientQuit(int clientId)
		{
			quitClients.Add(clientId);
		}

		public void AddFrameOrders(int clientId, byte[] orders, int timestep)
		{
			// HACK: Due to design we can actually receive client orders before the game start order
			// has been acted on, since immediate orders are buffered, so not all clients will have
			// been added yet. However, all clients are guaranteed to be added before the first
			// frame is stepped since they are added in OrderManager.StartGame()
			AddClient(clientId);

			var frameData = framePackets[clientId];
			frameData.Enqueue(orders);

			if (timestep != 0)
				timestepData.Enqueue(timestep);
		}

		public bool IsReadyForFrame()
		{
			return !ClientsNotReadyForFrame().Any();
		}

		public IEnumerable<int> ClientsNotReadyForFrame()
		{
			return ClientsPlayingInFrame()
				.Where(client => framePackets[client].Count == 0);
		}

		public IEnumerable<ClientOrder> OrdersForFrame(World world)
		{
			return ClientsPlayingInFrame()
				.SelectMany(x => framePackets[x].Dequeue().ToOrderList(world)
					.Select(y => new ClientOrder { Client = x, Order = y }));
		}

		public int BufferSizeForClient(int client)
		{
			return framePackets[client].Count;
		}

		public bool TryPeekTimestep(out int timestep)
		{
			return timestepData.TryPeek(out timestep);
		}

		public void AdvanceFrame()
		{
			timestepData.TryDequeue(out _);
		}

		public int BufferTimeRemaining()
		{
			return timestepData.Sum();
		}
	}
}
