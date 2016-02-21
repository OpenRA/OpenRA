#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;

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

		public readonly string Host;
		public readonly int Port;
		public readonly string Password = "";

		public string ServerError = "Server is not responding";
		public bool AuthenticationFailed = false;

		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;
		public int FramesAhead = 0;

		public int LastTickTime = Game.RunTime;

		public bool GameStarted { get { return NetFrameNumber != 0; } }
		public IConnection Connection { get; private set; }

		List<Order> localOrders = new List<Order>();

		List<ChatLine> chatCache = new List<ChatLine>();

		public readonly ReadOnlyList<ChatLine> ChatCache;

		bool disposed;

		void OutOfSync(int frame)
		{
			syncReport.DumpSyncReport(frame, frameData.OrdersForFrame(World, frame));
			throw new InvalidOperationException("Out of sync in frame {0}.\n Compare syncreport.log with other players.".F(frame));
		}

		public void StartGame()
		{
			if (GameStarted) return;

			NetFrameNumber = 1;
			for (var i = NetFrameNumber; i <= FramesAhead; i++)
				Connection.Send(i, new List<byte[]>());
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
			localOrders.Add(order);
		}

		public Action<Color, string, string> AddChatLine = (c, n, s) => { };
		void CacheChatLine(Color color, string name, string text)
		{
			chatCache.Add(new ChatLine(color, name, text));
		}

		public void TickImmediate()
		{
			var immediateOrders = localOrders.Where(o => o.IsImmediate).ToList();
			if (immediateOrders.Count != 0)
				Connection.SendImmediate(immediateOrders.Select(o => o.Serialize()).ToList());
			localOrders.RemoveAll(o => o.IsImmediate);

			var immediatePackets = new List<Pair<int, byte[]>>();

			Connection.Receive(
				(clientId, packet) =>
				{
					var frame = BitConverter.ToInt32(packet, 0);
					if (packet.Length == 5 && packet[4] == 0xBF)
						frameData.ClientQuit(clientId, frame);
					else if (packet.Length >= 5 && packet[4] == 0x65)
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

		public bool IsReadyForNextFrame
		{
			get { return NetFrameNumber >= 1 && frameData.IsReadyForFrame(NetFrameNumber); }
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

		public void Tick()
		{
			if (!IsReadyForNextFrame)
				throw new InvalidOperationException();

			Connection.Send(NetFrameNumber + FramesAhead, localOrders.Select(o => o.Serialize()).ToList());
			localOrders.Clear();

			foreach (var order in frameData.OrdersForFrame(World, NetFrameNumber))
				UnitOrders.ProcessOrder(this, World, order.Client, order.Order);

			Connection.SendSync(NetFrameNumber, OrderIO.SerializeSync(World.SyncHash()));

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

		public ChatLine(Color c, string n, string t)
		{
			Color = c;
			Name = n;
			Text = t;
		}
	}
}
