#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public sealed class OrderManager : IDisposable
	{
		readonly SyncReport syncReport;
		readonly FrameData frameData = new FrameData();

		public Session LobbyInfo = new Session();
		public Session.Client LocalClient { get { return LobbyInfo.ClientWithIndex(Connection.LocalClientId); } }
		public World world;

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

		public readonly int SyncHeaderSize = 9;

		List<Order> localOrders = new List<Order>();

		public void StartGame()
		{
			if (GameStarted) return;

			NetFrameNumber = 1;
			for (var i = NetFrameNumber ; i <= FramesAhead ; i++)
				Connection.Send(i, new List<byte[]>());
		}

		public OrderManager(string host, int port, string password, IConnection conn)
		{
			Host = host;
			Port = port;
			Password = password;
			Connection = conn;
			syncReport = new SyncReport(this);
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

		public void TickImmediate()
		{
			var immediateOrders = localOrders.Where( o => o.IsImmediate ).ToList();
			if( immediateOrders.Count != 0 )
				Connection.SendImmediate( immediateOrders.Select( o => o.Serialize() ).ToList() );
			localOrders.RemoveAll( o => o.IsImmediate );

			var immediatePackets = new List<Pair<int, byte[]>>();

			Connection.Receive(
				( clientId, packet ) =>
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
				} );

			foreach (var p in immediatePackets)
				foreach (var o in p.Second.ToOrderList(world))
					UnitOrders.ProcessOrder(this, world, p.First, o);
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();

		void CheckSync(byte[] packet)
		{
			var frame = BitConverter.ToInt32(packet, 0);
			byte[] existingSync;
			if (syncForFrame.TryGetValue(frame, out existingSync))
			{
				if (packet.Length != existingSync.Length)
				{
					syncReport.DumpSyncReport(frame);
					OutOfSync(frame);
				}
				else
				{
					for (var i = 0; i < packet.Length; i++)
					{
						if (packet[i] != existingSync[i])
						{
							syncReport.DumpSyncReport(frame);

							if (i < SyncHeaderSize)
								OutOfSync(frame, "Tick");
							else
								OutOfSync(frame, (i - SyncHeaderSize) / 4);
						}
					}
				}
			}
			else
				syncForFrame.Add(frame, packet);
		}

		void OutOfSync(int frame, int index)
		{
			var orders = frameData.OrdersForFrame(world, frame);

			// Invalid index
			if (index >= orders.Count())
				OutOfSync(frame);

			throw new InvalidOperationException("Out of sync in frame {0}.\n {1}\n Compare syncreport.log with other players.".F(frame, orders.ElementAt(index).Order.ToString()));
		}

		static void OutOfSync(int frame)
		{
			throw new InvalidOperationException("Out of sync in frame {0}.\n Compare syncreport.log with other players.".F(frame));
		}

		static void OutOfSync(int frame, string blame)
		{
			throw new InvalidOperationException("Out of sync in frame {0}: Blame {1}.\n Compare syncreport.log with other players.".F(frame, blame));
		}

		public bool IsReadyForNextFrame
		{
			get { return NetFrameNumber >= 1 && frameData.IsReadyForFrame(NetFrameNumber); }
		}

		static readonly IEnumerable<Session.Client> NoClients = new Session.Client[] {};
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

			var sync = new List<int>();
			sync.Add(world.SyncHash());

			foreach (var order in frameData.OrdersForFrame(world, NetFrameNumber))
			{
				UnitOrders.ProcessOrder(this, world, order.Client, order.Order);
				sync.Add(world.SyncHash());
			}

			var ss = sync.SerializeSync();
			Connection.SendSync(NetFrameNumber, ss);

			syncReport.UpdateSyncReport();

			++NetFrameNumber;
		}

		public void Dispose()
		{
			if (Connection != null)
				Connection.Dispose();
		}
	}
}
