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
		private static readonly List<byte[]> BLANK = new List<byte[]>();
		static readonly IEnumerable<Session.Client> NoClients = new Session.Client[] { };

		readonly SyncReport syncReport;
		readonly FrameData frameData = new FrameData();

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient { get { return LobbyInfo.ClientWithIndex(Connection.LocalClientId); } }
		public World World;

		public readonly string Host;
		public readonly int Port;
		public readonly string Password = "";

		public string ServerError = "Server is not responding";
		public bool AuthenticationFailed = false;
		public ExternalMod ServerExternalMod = null;

		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;

		public int OrderLatency = 0; // Set during lobby by a "SyncInfo" packet, see UnitOrders
		public int NextOrderFrame;
		private bool compensate = false;
		private int latencyTickup = 0;

		public long LastTickTime = Game.RunTime;

		public bool GameStarted { get { return NetFrameNumber != 0; } }
		public IConnection Connection { get; private set; }

		internal int GameSaveLastFrame = -1;
		internal int GameSaveLastSyncFrame = -1;

		List<Order> localOrders = new List<Order>();
		List<Order> localImmediateOrders = new List<Order>();

		List<ChatLine> chatCache = new List<ChatLine>();

		public readonly ReadOnlyList<ChatLine> ChatCache;

		bool disposed;
		bool generateSyncReport = false;

		void OutOfSync(int frame)
		{
			syncReport.DumpSyncReport(frame, frameData.OrdersForFrame(World, frame));
			throw new InvalidOperationException("Out of sync in frame {0}.\n Compare syncreport.log with other players.".F(frame));
		}

		public void StartGame()
		{
			if (GameStarted)
				return;

			// Generating sync reports is expensive, so only do it if we have
			// other players to compare against if a desync did occur
			generateSyncReport = !(Connection is ReplayConnection) && LobbyInfo.GlobalSettings.EnableSyncReports;

			NetFrameNumber = 1;
			NextOrderFrame = 1;

			SendOrders();
		}

		public OrderManager(string host, int port, string password, IConnection conn)
		{
			Host = host;
			Port = port;
			Password = password;
			Connection = conn;
			syncReport = new SyncReport(this);
			ChatCache = new ReadOnlyList<ChatLine>(chatCache);
			AddChatLine += CacheChatLine;
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

		public void TickImmediate()
		{
			if (localImmediateOrders.Count != 0 && GameSaveLastFrame < NetFrameNumber + OrderLatency)
				Connection.SendImmediate(localImmediateOrders.Select(o => o.Serialize()));
			localImmediateOrders.Clear();

			var immediatePackets = new List<Pair<int, byte[]>>();

			Connection.Receive(
				(clientId, packet) =>
				{
					var frame = BitConverter.ToInt32(packet, 0);
					if (packet.Length == 5 && packet[4] == (byte)OrderType.Disconnect)
						frameData.ClientQuit(clientId, frame);
					else if (packet.Length >= 5 && packet[4] == (byte)OrderType.SyncHash)
						CheckSync(packet);
					else if (frame == 0)
						immediatePackets.Add(Pair.New(clientId, packet));
					else
						frameData.AddFrameOrders(clientId, frame, packet);
				});

			foreach (var p in immediatePackets)
			{
				foreach (var o in p.Second.ToOrderList(World))
				{
					UnitOrders.ProcessOrder(this, World, p.First, o);

					// A mod switch or other event has pulled the ground from beneath us
					if (disposed)
						return;
				}
			}
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();

		void CheckSync(byte[] packet)
		{
			var frame = BitConverter.ToInt32(packet, 0);
			byte[] existingSync;
			if (syncForFrame.TryGetValue(frame, out existingSync))
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

		public bool IsReadyForNextFrame()
		{
			if (NetFrameNumber >= 1 && frameData.IsReadyForFrame(NetFrameNumber))
			{
				return true;
			}
			else
			{
				// Raise order latency if we were the probably the reason stuff was delayed
				if (!compensate && frameData.BufferSizeForClient(NetFrameNumber, Connection.LocalClientId) == 0)
					compensate = true;
				return false;
			}
		}

		public IEnumerable<Session.Client> GetClientsNotReadyForNextFrame
		{
			get
			{
				return NetFrameNumber >= 1
					? frameData.ClientsNotReadyForFrame(NetFrameNumber)
						.Select(a => LobbyInfo.ClientWithIndex(a))
					: NoClients;
			}
		}

		public void TryDecreaseOrderLatency()
		{
			if (OrderLatency <= 1) // Minimum
				return;

			// If our own buffer is fuller than necessary, tick
			if (frameData.BufferSizeForClient(NetFrameNumber, Connection.LocalClientId) > 1)
				latencyTickup++;
			else
				latencyTickup = 0;

			if (latencyTickup > 1)
			{
				if (OrderLatency > 1)
					OrderLatency--;
				latencyTickup = 0;
			}
		}

		private void SendOrders()
		{
			// When we are lowering latency, we buffer orders
			if (NextOrderFrame > NetFrameNumber + OrderLatency)
				return;

			// When we are increasing latency, we send extra packets
			while (NextOrderFrame < NetFrameNumber + OrderLatency)
			{
				if (GameSaveLastFrame < NextOrderFrame)
					Connection.Send(NextOrderFrame, BLANK);
				NextOrderFrame++;
			}

			Connection.Send(NextOrderFrame, localOrders.Select(o => o.Serialize()).ToList());
			localOrders.Clear();
			NextOrderFrame++;
		}

		public void Tick()
		{
			if (!IsReadyForNextFrame())
				throw new InvalidOperationException();

			if (compensate)
			{
				OrderLatency++;
				compensate = false;
			}

			// Lower order latency if we received multiple orders from some client (excluding our own echo)
			// Will undo the forced compensate if a player has caused slowdown
			TryDecreaseOrderLatency();

			SendOrders();

			foreach (var order in frameData.OrdersForFrame(World, NetFrameNumber))
				UnitOrders.ProcessOrder(this, World, order.Client, order.Order);

			if (NetFrameNumber + OrderLatency >= GameSaveLastSyncFrame)
				Connection.SendSync(NetFrameNumber, OrderIO.SerializeSync(World.SyncHash()));
			else
				Connection.SendSync(NetFrameNumber, OrderIO.SerializeSync(0));

			if (generateSyncReport)
				using (new PerfSample("sync_report"))
					syncReport.UpdateSyncReport();

			++NetFrameNumber;
		}

		public void Dispose()
		{
			disposed = true;
			if (Connection != null)
				Connection.Dispose();
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
