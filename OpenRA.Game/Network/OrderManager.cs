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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Network
{
	public sealed class OrderManager : IDisposable
	{
		readonly SyncReport syncReport;

		readonly Dictionary<int, Queue<byte[]>> pendingPackets = new Dictionary<int, Queue<byte[]>>();

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient => LobbyInfo.ClientWithIndex(Connection.LocalClientId);
		public World World;

		public string ServerError = null;
		public bool AuthenticationFailed = false;

		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;
		public int FramesAhead = 0;

		public TickTime LastTickTime;

		public bool GameStarted => NetFrameNumber != 0;
		public IConnection Connection { get; private set; }

		internal int GameSaveLastFrame = -1;
		internal int GameSaveLastSyncFrame = -1;

		readonly List<Order> localOrders = new List<Order>();
		readonly List<Order> localImmediateOrders = new List<Order>();

		readonly List<TextNotification> notificationsCache = new List<TextNotification>();

		public IReadOnlyList<TextNotification> NotificationsCache => notificationsCache;

		bool disposed;
		bool generateSyncReport = false;

		public struct ClientOrder
		{
			public int Client;
			public Order Order;

			public override string ToString()
			{
				return $"ClientId: {Client} {Order}";
			}
		}

		void OutOfSync(int frame)
		{
			syncReport.DumpSyncReport(frame);
			throw new InvalidOperationException($"Out of sync in frame {frame}.\n Compare syncreport.log with other players.");
		}

		public void StartGame()
		{
			if (GameStarted)
				return;

			foreach (var client in LobbyInfo.Clients)
				if (!client.IsBot)
					pendingPackets.Add(client.Index, new Queue<byte[]>());

			// Generating sync reports is expensive, so only do it if we have
			// other players to compare against if a desync did occur
			generateSyncReport = !(Connection is ReplayConnection) && LobbyInfo.GlobalSettings.EnableSyncReports;

			NetFrameNumber = 1;
			LocalFrameNumber = 0;
			LastTickTime.Value = Game.RunTime;

			if (GameSaveLastFrame < 0)
				for (var i = NetFrameNumber; i <= FramesAhead; i++)
					Connection.Send(i, new List<byte[]>());
		}

		public OrderManager(IConnection conn)
		{
			Connection = conn;
			syncReport = new SyncReport(this);
			AddTextNotification += CacheTextNotification;

			LastTickTime = new TickTime(() => SuggestedTimestep, Game.RunTime);
		}

		public void IssueOrders(Order[] orders)
		{
			foreach (var order in orders)
				IssueOrder(order);
		}

		public void IssueOrder(Order order)
		{
			if (order.IsImmediate)
				localImmediateOrders.Add(order);
			else
				localOrders.Add(order);
		}

		public Action<TextNotification> AddTextNotification = (notification) => { };
		void CacheTextNotification(TextNotification notification)
		{
			notificationsCache.Add(notification);
		}

		void SendImmediateOrders()
		{
			if (localImmediateOrders.Count != 0 && GameSaveLastFrame < NetFrameNumber + FramesAhead)
				Connection.SendImmediate(localImmediateOrders.Select(o => o.Serialize()));
			localImmediateOrders.Clear();
		}

		void ReceiveAllOrdersAndCheckSync()
		{
			Connection.Receive(
				(clientId, packet) =>
				{
					// HACK: The shellmap relies on ticking a disposed OM
					if (disposed && World.Type != WorldType.Shellmap)
						return;

					var frame = BitConverter.ToInt32(packet, 0);
					if (packet.Length == Order.DisconnectOrderLength + 4 && packet[4] == (byte)OrderType.Disconnect)
					{
						pendingPackets.Remove(BitConverter.ToInt32(packet, 5));
					}
					else if (packet.Length > 4 && packet[4] == (byte)OrderType.SyncHash)
					{
						if (packet.Length != 4 + Order.SyncHashOrderLength)
						{
							Log.Write("debug", $"Dropped sync order with length {packet.Length}. Expected length {4 + Order.SyncHashOrderLength}.");
							return;
						}

						CheckSync(packet);
					}
					else if (frame == 0)
					{
						foreach (var o in packet.ToOrderList(World))
						{
							UnitOrders.ProcessOrder(this, World, clientId, o);

							// A mod switch or other event has pulled the ground from beneath us
							if (disposed)
								return;
						}
					}
					else
					{
						if (pendingPackets.TryGetValue(clientId, out var queue))
							queue.Enqueue(packet);
						else
							Log.Write("debug", $"Received packet from disconnected client '{clientId}'");
					}
				});
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();

		void CheckSync(byte[] packet)
		{
			var frame = BitConverter.ToInt32(packet, 0);
			if (syncForFrame.TryGetValue(frame, out var existingSync))
			{
				if (packet.Length != existingSync.Length)
					OutOfSync(frame);
				else
					for (var i = 0; i < packet.Length; i++)
						if (packet[i] != existingSync[i])
							OutOfSync(frame);
			}
			else
				syncForFrame.Add(frame, packet);
		}

		bool IsReadyForNextFrame => GameStarted && pendingPackets.All(p => p.Value.Count > 0);

		int SuggestedTimestep
		{
			get
			{
				if (World == null)
					return Ui.Timestep;

				if (World.IsLoadingGameSave)
					return 1;

				if (World.IsReplay)
					return World.ReplayTimestep;

				return World.Timestep;
			}
		}

		void SendOrders()
		{
			if (!GameStarted)
				return;

			if (GameSaveLastFrame < NetFrameNumber + FramesAhead)
			{
				Connection.Send(NetFrameNumber + FramesAhead, localOrders.Select(o => o.Serialize()).ToList());
				localOrders.Clear();
			}
		}

		void ProcessOrders()
		{
			var clientOrders = new List<ClientOrder>();

			foreach (var (clientId, clientPackets) in pendingPackets)
			{
				// The IsReadyForNextFrame check above guarantees that all clients have sent a packet
				var frameData = clientPackets.Dequeue();

				// Orders are synchronised by sending an initial FramesAhead set of empty packets
				// and then making sure that we enqueue and process exactly one packet for each player each tick.
				// This may change in the future, so sanity check that the orders are for the frame we expect
				// and crash early instead of risking desyncs.
				var frameNumber = BitConverter.ToInt32(frameData, 0);
				if (frameNumber != NetFrameNumber)
					throw new InvalidDataException($"Attempted to process orders from client {clientId} for frame {frameNumber} on frame {NetFrameNumber}");

				foreach (var order in frameData.ToOrderList(World))
				{
					UnitOrders.ProcessOrder(this, World, clientId, order);
					clientOrders.Add(new ClientOrder { Client = clientId, Order = order });
				}
			}

			if (NetFrameNumber + FramesAhead >= GameSaveLastSyncFrame)
			{
				var defeatState = 0UL;
				for (var i = 0; i < World.Players.Length; i++)
					if (World.Players[i].WinState == WinState.Lost)
						defeatState |= 1UL << i;

				Connection.SendSync(NetFrameNumber, OrderIO.SerializeSync(World.SyncHash(), defeatState));
			}
			else
				Connection.SendSync(NetFrameNumber, OrderIO.SerializeSync(0, 0));

			if (generateSyncReport)
				using (new PerfSample("sync_report"))
					syncReport.UpdateSyncReport(clientOrders);

			++NetFrameNumber;
		}

		public void Dispose()
		{
			disposed = true;
			Connection?.Dispose();
		}

		public void TickImmediate()
		{
			SendImmediateOrders();

			ReceiveAllOrdersAndCheckSync();
		}

		public bool TryTick()
		{
			var shouldTick = true;

			if (IsNetTick)
			{
				// Check whether or not we will be ready for a tick next frame
				// We don't need to include ourselves in the equation because we can always generate orders this frame
				shouldTick = pendingPackets.All(p => p.Key == Connection.LocalClientId || p.Value.Count > 0);

				// Send orders only if we are currently ready, this prevents us sending orders too soon if we are
				// stalling
				if (shouldTick)
					SendOrders();
			}

			var willTick = shouldTick;
			if (willTick && IsNetTick)
			{
				willTick = IsReadyForNextFrame;
				if (willTick)
					ProcessOrders();
			}

			if (willTick)
				LocalFrameNumber++;

			return willTick;
		}

		bool IsNetTick => LocalFrameNumber % Game.NetTickScale == 0;
	}
}
