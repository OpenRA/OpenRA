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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Network
{
	public sealed class OrderManager : IDisposable
	{
		static readonly IEnumerable<Session.Client> NoClients = new Session.Client[] { };

		readonly SyncReport syncReport;
		readonly FrameData frameData = new FrameData();

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient { get { return LobbyInfo.ClientWithIndex(Connection.LocalClientId); } }
		public World World;

		public readonly ConnectionTarget Endpoint;
		public readonly string Password = "";

		public string ServerError = null;
		public bool AuthenticationFailed = false;
		public ExternalMod ServerExternalMod = null;

		public int NetTickScale { get { return LobbyInfo.GlobalSettings.UseNewNetcode ? Game.NewNetcodeNetTickScale : Game.DefaultNetTickScale; } }
		public bool IsNetTick { get { return LocalFrameNumber % NetTickScale == 0; } }
		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;

		public int SyncFrameScale = 1; // TODO make this based on actual time, I would suggest once per second

		public int LastSlowDownRequestTick;
		public bool ShouldUseCatchUp;
		public int OrderLatency; // Set during lobby by a "SyncInfo" packet, see UnitOrders
		public int NextOrderFrame;
		public int CatchUpFrames { get; private set; }
		public bool IsStalling { get; private set; }

		public long LastTickTime = Game.RunTime;

		public bool GameStarted { get { return NetFrameNumber != 0; } }
		public IConnection Connection { get; private set; }

		internal int GameSaveLastFrame = -1;
		internal int GameSaveLastSyncFrame = -1;

		readonly List<Order> localOrders = new List<Order>();
		readonly List<Order> localImmediateOrders = new List<Order>();
		readonly List<(int ClientId, byte[] Packet)> immediatePackets = new List<(int ClientId, byte[] Packet)>();

		readonly List<ChatLine> chatCache = new List<ChatLine>();

		public readonly ReadOnlyList<ChatLine> ChatCache;

		bool disposed;
		bool generateSyncReport = false;

		void OutOfSync(int frame)
		{
			syncReport.DumpSyncReport(frame);
			throw new InvalidOperationException("Out of sync in frame {0}.\n Compare syncreport.log with other players.".F(frame));
		}

		public void StartGame()
		{
			if (GameStarted)
				return;

			if (LobbyInfo.Clients.Count == 0)
				frameData.AddClient(Connection.LocalClientId);

			foreach (var client in LobbyInfo.Clients)
				if (!client.IsBot)
					frameData.AddClient(client.Index);

			// Generating sync reports is expensive, so only do it if we have
			// other players to compare against if a desync did occur
			generateSyncReport = !(Connection is ReplayConnection) && LobbyInfo.GlobalSettings.EnableSyncReports;

			NetFrameNumber = 1;
			NextOrderFrame = 1;

			if (LobbyInfo.GlobalSettings.UseNewNetcode)
				localImmediateOrders.Add(Order.FromTargetString("Loaded", "", true));
			else
				for (var i = 0; i <= OrderLatency; ++i)
					SendOrders();
		}

		public OrderManager(ConnectionTarget endpoint, string password, IConnection conn)
		{
			Endpoint = endpoint;
			Password = password;
			Connection = conn;
			syncReport = new SyncReport(this);
			ChatCache = new ReadOnlyList<ChatLine>(chatCache);
			AddChatLine += CacheChatLine;

			// HACK: should not rely on this
			if (conn is NetworkConnection)
				ShouldUseCatchUp = true;
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

		public Action<string, Color, string, Color> AddChatLine = (n, nc, s, tc) => { };
		void CacheChatLine(string name, Color nameColor, string text, Color textColor)
		{
			chatCache.Add(new ChatLine(name, nameColor, text, textColor));
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
				(clientId, packet) =>
				{
					var frame = BitConverter.ToInt32(packet, 0);
					if (packet.Length == 5 && packet[4] == (byte)OrderType.Disconnect)
						frameData.ClientQuit(clientId);
					else if (packet.Length >= 5 && packet[4] == (byte)OrderType.SyncHash)
						CheckSync(packet);
					else if (frame == 0)
						immediatePackets.Add((clientId, packet));
					else
						frameData.AddFrameOrders(clientId, packet);
				});
		}

		void ProcessImmediateOrders()
		{
			foreach (var p in immediatePackets)
			{
				foreach (var o in p.Packet.ToOrderList(World))
				{
					UnitOrders.ProcessOrder(this, World, p.ClientId, o);

					// A mod switch or other event has pulled the ground from beneath us
					if (disposed)
						return;
				}
			}

			immediatePackets.Clear();
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

		void CompensateForLatency()
		{
			// NOTE: subtract 1 because we are only interested in *excess* frames
			var catchUpNetFrames = frameData.BufferSizeForClient(Connection.LocalClientId) - 1;
			if (catchUpNetFrames < 0)
				catchUpNetFrames = 0;

			CatchUpFrames = ShouldUseCatchUp ? catchUpNetFrames : 0;

			if (LastSlowDownRequestTick + 5 < NetFrameNumber && (catchUpNetFrames > 5))
			{
				localImmediateOrders.Add(Order.FromTargetString("SlowDown", catchUpNetFrames.ToString(), true));
				LastSlowDownRequestTick = NetFrameNumber;
			}
		}

		IEnumerable<Session.Client> GetClientsNotReadyForNextFrame
		{
			get
			{
				return GameStarted // TODO review, compare to bleed
					? frameData.ClientsNotReadyForFrame()
						.Select(a => LobbyInfo.ClientWithIndex(a))
					: NoClients;
			}
		}

		void SendOrders()
		{
			if (!GameStarted)
				return;

			if (GameSaveLastFrame < NextOrderFrame)
			{
				Connection.Send(NextOrderFrame, localOrders.Select(o => o.Serialize()).ToList());
				localOrders.Clear();
			}

			NextOrderFrame++;
		}

		/*
		 * Only available if TickImmediate() is called first and we are ready to dispatch received orders locally.
		 * Process all incoming orders for this frame, handle sync hashes and step our net frame.
		 */
		void ProcessOrders()
		{
			var orders = frameData.OrdersForFrame(World).ToList();
			foreach (var order in orders)
				UnitOrders.ProcessOrder(this, World, order.Client, order.Order);

			if (NetFrameNumber % SyncFrameScale == 0)
			{
				if (NetFrameNumber >= GameSaveLastSyncFrame)
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
						syncReport.UpdateSyncReport(orders);
			}

			++NetFrameNumber;
		}

		public void TickPreGame()
		{
			SendImmediateOrders();

			ReceiveAllOrdersAndCheckSync();

			Sync.RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, World, ProcessImmediateOrders);
		}

		public bool TryTick()
		{
			var shouldTick = true;

			if (IsNetTick)
			{
				// Check whether or not we will be ready for a tick next frame
				// We don't need to include ourselves in the equation because we can always generate orders this frame
				shouldTick = !GetClientsNotReadyForNextFrame.Except(new[] { LocalClient }).Any();

				// Send orders only if we are currently ready, this prevents us sending orders too soon if we are
				// stalling
				if (shouldTick)
					SendOrders();
			}

			// Sets catchup frames and asks server to slow down if they are too high
			if (LobbyInfo.GlobalSettings.UseNewNetcode)
				CompensateForLatency();

			SendImmediateOrders();

			ReceiveAllOrdersAndCheckSync();

			// Always send immediate orders
			Sync.RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, World, ProcessImmediateOrders);

			var willTick = shouldTick;
			if (willTick && IsNetTick)
			{
				willTick = frameData.IsReadyForFrame();
				if (willTick)
					ProcessOrders();

				IsStalling = !willTick;
			}

			if (willTick)
				LocalFrameNumber++;

			return willTick;
		}

		public void Dispose()
		{
			disposed = true;
			Connection?.Dispose();
		}

		public void SyncLobbyInfo()
		{
			if (OrderLatency != LobbyInfo.GlobalSettings.OrderLatency && !GameStarted)
			{
				OrderLatency = LobbyInfo.GlobalSettings.OrderLatency;
				Log.Write("server", "Order lag is now {0} frames.", LobbyInfo.GlobalSettings.OrderLatency);
			}

			if (Connection is NetworkConnection c)
				c.UseNewNetcode = LobbyInfo.GlobalSettings.UseNewNetcode;
		}
	}

	public class ChatLine
	{
		public readonly Color Color;
		public readonly string Name;
		public readonly string Text;
		public readonly Color TextColor;

		public ChatLine(string name, Color nameColor, string text, Color textColor)
		{
			Color = nameColor;
			Name = name;
			Text = text;
			TextColor = textColor;
		}
	}
}
