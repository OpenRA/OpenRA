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
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Network
{
	public sealed class OrderManager : IDisposable
	{
		readonly SyncReport syncReport;

		// These are the clients who we expect to receive orders / sync from before we can simulate the next tick
		readonly HashSet<int> activeClients = new HashSet<int>();

		readonly Dictionary<int, Queue<byte[]>> pendingPackets = new Dictionary<int, Queue<byte[]>>();

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient => LobbyInfo.ClientWithIndex(Connection.LocalClientId);
		public World World;

		public readonly ConnectionTarget Endpoint;
		public readonly string Password = "";

		public string ServerError = null;
		public bool AuthenticationFailed = false;
		public ExternalMod ServerExternalMod = null;

		public int NetFrameNumber { get; private set; }
		public int LocalFrameNumber;
		public int FramesAhead = 0;

		public long LastTickTime = Game.RunTime;

		public bool GameStarted => NetFrameNumber != 0;
		public IConnection Connection { get; private set; }

		internal int GameSaveLastFrame = -1;
		internal int GameSaveLastSyncFrame = -1;

		readonly List<Order> localOrders = new List<Order>();
		readonly List<Order> localImmediateOrders = new List<Order>();
		readonly List<(int ClientId, byte[] Packet)> immediatePackets = new List<(int ClientId, byte[] Packet)>();

		readonly List<ChatLine> chatCache = new List<ChatLine>();

		public IReadOnlyList<ChatLine> ChatCache => chatCache;

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

			// Generating sync reports is expensive, so only do it if we have
			// other players to compare against if a desync did occur
			generateSyncReport = !(Connection is ReplayConnection) && LobbyInfo.GlobalSettings.EnableSyncReports;

			NetFrameNumber = 1;

			if (GameSaveLastFrame < 0)
				for (var i = NetFrameNumber; i <= FramesAhead; i++)
					Connection.Send(i, new List<byte[]>());
		}

		public OrderManager(ConnectionTarget endpoint, string password, IConnection conn)
		{
			Endpoint = endpoint;
			Password = password;
			Connection = conn;
			syncReport = new SyncReport(this);
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
			if (localImmediateOrders.Count != 0 && GameSaveLastFrame < NetFrameNumber + FramesAhead)
				Connection.SendImmediate(localImmediateOrders.Select(o => o.Serialize()));
			localImmediateOrders.Clear();

			Connection.Receive(
				(clientId, packet) =>
				{
					var frame = BitConverter.ToInt32(packet, 0);
					if (packet.Length == 5 && packet[4] == (byte)OrderType.Disconnect)
						activeClients.Remove(clientId);
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
						immediatePackets.Add((clientId, packet));
					else
					{
						activeClients.Add(clientId);
						pendingPackets.GetOrAdd(clientId).Enqueue(packet);
					}
				});

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

		public bool IsReadyForNextFrame => GameStarted && activeClients.All(client => pendingPackets[client].Count > 0);

		public void Tick()
		{
			if (!IsReadyForNextFrame)
				throw new InvalidOperationException();

			if (GameSaveLastFrame < NetFrameNumber + FramesAhead)
				Connection.Send(NetFrameNumber + FramesAhead, localOrders.Select(o => o.Serialize()).ToList());

			localOrders.Clear();

			var clientOrders = new List<ClientOrder>();

			foreach (var clientId in activeClients)
			{
				// The IsReadyForNextFrame check above guarantees that all clients have sent a packet
				var frameData = pendingPackets[clientId].Dequeue();

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
