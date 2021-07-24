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
		const double CatchupFactor = 0.1;
		const double CatchUpLimit = 1.5;
		const int SimLagThreshold = 500;
		const int SimLagLimit = 1000;

		readonly SyncReport syncReport;

		readonly Dictionary<int, Queue<byte[]>> pendingPackets = new Dictionary<int, Queue<byte[]>>();
		readonly Queue<int> timestepData = new Queue<int>();

		int targetTimestep;
		int actualTimestep;
		double averageSimLag;
		long tickStartRuntime = Game.RunTime;
		long lastTickStartRuntime = Game.RunTime;
		int lastSlowDownRequestTick;
		int nextOrderFrame;

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient => LobbyInfo.ClientWithIndex(Connection.LocalClientId);
		public World World;

		public string ServerError = null;
		public bool AuthenticationFailed = false;

		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;
		public int FramesAhead = 0;
		public bool ShouldUseCatchUp;
		public volatile bool IsStalling;
		public int SimLag = 0;
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
			nextOrderFrame = 1;
			LocalFrameNumber = 0;
			LastTickTime.Value = Game.RunTime;

			if (orderLatency != World.OrderLatency && !GameStarted)
			{
				orderLatency = World.OrderLatency;
				Log.Write("server", "Order lag is now {0} frames.", World.OrderLatency);
			}

			if (Connection is NetworkConnection c)
				c.UseNewNetcode = LobbyInfo.GlobalSettings.UseNewNetcode;

			if (LobbyInfo.GlobalSettings.UseNewNetcode)
				localImmediateOrders.Add(Order.FromTargetString("Loaded", "", true));
			else
				for (var i = 0; i <= orderLatency; ++i)
					SendOrders();
		}

		public OrderManager(IConnection conn)
		{
			Connection = conn;
			syncReport = new SyncReport(this);
			AddTextNotification += CacheTextNotification;

			LastTickTime = new TickTime(() => SuggestedTimestep, Game.RunTime);

			ShouldUseCatchUp = conn.ShouldUseCatchUp;
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
			if (localImmediateOrders.Count != 0 && GameSaveLastFrame < NetFrameNumber)
				Connection.SendImmediate(localImmediateOrders.Select(o => o.Serialize()));
			localImmediateOrders.Clear();
		}

		void ReceiveAllOrdersAndCheckSync()
		{
			Connection.Receive(
				(clientId, packet, timestep) =>
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

						if (timestep != 0)
							timestepData.Enqueue(timestep);
					}
				});
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();
		int orderLatency;

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

				if (!ShouldUseCatchUp)
					return World.Timestep;

				if (IsStalling || World.IsLoadingGameSave)
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

			if (GameSaveLastFrame < nextOrderFrame)
			{
				Connection.Send(nextOrderFrame, localOrders.Select(o => o.Serialize()).ToList());
				localOrders.Clear();
			}

			nextOrderFrame++;
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

			if (timestepData.TryDequeue(out var timestep))
				actualTimestep = timestep;

			++NetFrameNumber;
		}

		public void Dispose()
		{
			disposed = true;
			Connection?.Dispose();
		}

		public void TickImmediate(long tick)
		{
			tickStartRuntime = tick;

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

			IsStalling = !willTick;

			CompensateForLatency();

			return willTick;
		}

		bool IsNetTick => LocalFrameNumber % (LobbyInfo.GlobalSettings.UseNewNetcode ? Game.NewNetcodeNetTickScale : Game.DefaultNetTickScale) == 0;

		void CompensateForLatency()
		{
			if (!LobbyInfo.GlobalSettings.UseNewNetcode || !ShouldUseCatchUp)
				return;

			if (LocalFrameNumber == 0 || IsStalling)
			{
				LastTickTime.Value = tickStartRuntime;
				lastTickStartRuntime = tickStartRuntime;
				targetTimestep = actualTimestep;

				// We are happy to stall after a lag spike
				SimLag = (SimLag - 1).Clamp(-World.Timestep, SimLagLimit);
				return;
			}

			var bufferRemaining = pendingPackets[LocalClient.Index].Count * World.Timestep;
			var realTimestep = (int)(tickStartRuntime - lastTickStartRuntime);
			SimLag = (SimLag + realTimestep - targetTimestep).Clamp(-World.Timestep, SimLagLimit);
			var catchup = (int)Math.Ceiling(bufferRemaining * CatchupFactor).Clamp(0, World.Timestep / CatchUpLimit);
			var simLagDelta = realTimestep - World.Timestep + catchup;
			averageSimLag = averageSimLag * 0.95 + simLagDelta * 0.05;
			var slowDownRequired = (int)Math.Ceiling(averageSimLag);

			if (slowDownRequired > 0 && SimLag > SimLagThreshold && NetFrameNumber > lastSlowDownRequestTick + 5)
			{
				localImmediateOrders.Add(Order.FromTargetString("SlowDown", slowDownRequired.ToString(), true));
				lastSlowDownRequestTick = NetFrameNumber;
			}

			targetTimestep = (actualTimestep - catchup).Clamp(1, 1000);

			LastTickTime.Value = tickStartRuntime - SimLag;

			lastTickStartRuntime = tickStartRuntime;

			PerfHistory.Increment("net_buffer", bufferRemaining);
			PerfHistory.Increment("net_slowdown", slowDownRequired < 0 ? slowDownRequired : 0);
			PerfHistory.Increment("net_simlag", 100.0 * (double)SimLag / (double)SimLagLimit);
			PerfHistory.Increment("net_speed", 100.0 * (double)World.Timestep / (double)actualTimestep);
		}
	}
}
