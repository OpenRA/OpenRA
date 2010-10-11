#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	class OrderManager : IDisposable
	{
		readonly SyncReport syncReport = new SyncReport();
		readonly FrameData frameData = new FrameData();

		public Session LobbyInfo = new Session( Game.Settings.Game.Mods );
		public Session.Client LocalClient { get { return LobbyInfo.ClientWithIndex( Connection.LocalClientId ); } }

		public readonly string Host;
		public readonly int Port;

		public int FrameNumber { get; private set; }
		public int FramesAhead = 0;

		public bool GameStarted { get { return FrameNumber != 0; } }
		public IConnection Connection { get; private set; }
		
		public readonly int SyncHeaderSize = 9;
		
		List<Order> localOrders = new List<Order>();

		public void StartGame()
		{
			if (GameStarted) return;

			FrameNumber = 1;
			for( int i = FrameNumber ; i <= FramesAhead ; i++ )
				Connection.Send( new List<Order>().Serialize( i ) );
		}

		public OrderManager( string host, int port, IConnection conn )
		{
			this.Host = host;
			this.Port = port;
			Connection = conn;
		}

		public void IssueOrders( Order[] orders )
		{
			foreach( var order in orders )
				IssueOrder( order );
		}

		public void IssueOrder( Order order )
		{
			localOrders.Add( order );
		}

		public void TickImmediate( World world )
		{
			var immediateOrders = localOrders.Where( o => o.IsImmediate ).ToList();
			if( immediateOrders.Count != 0 )
				Connection.Send( immediateOrders.Serialize( 0 ) );
			localOrders.RemoveAll( o => o.IsImmediate );

			var immediatePackets = new List<Pair<int, byte[]>>();

			Connection.Receive(
				( clientId, packet ) =>
				{
					var frame = BitConverter.ToInt32( packet, 0 );
					if( packet.Length == 5 && packet[ 4 ] == 0xBF )
						frameData.ClientQuit( clientId, frame );
					else if( packet.Length >= 5 && packet[ 4 ] == 0x65 )
						CheckSync( world, packet );
					else if( frame == 0 )
						immediatePackets.Add( Pair.New( clientId, packet ) );
					else
						frameData.AddFrameOrders( clientId, frame, packet );
				} );

			foreach( var p in immediatePackets )
				foreach( var o in p.Second.ToOrderList( world ) )
					UnitOrders.ProcessOrder( this, world, p.First, o );
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();

		void CheckSync(World world, byte[] packet)
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
					for (int i = 0; i < packet.Length; i++)
					{
						if (packet[i] != existingSync[i])
						{
							syncReport.DumpSyncReport(frame);

							if (i < SyncHeaderSize)
								OutOfSync(frame, "Tick");
							else
								OutOfSync(world, frame, (i - SyncHeaderSize) / 4);
						}
					}
				}
			}
			else
				syncForFrame.Add(frame, packet);
		}

		void OutOfSync( World world, int frame, int index)
		{
			var order = frameData.OrdersForFrame( world, frame ).ElementAt(index);
			throw new InvalidOperationException("Out of sync in frame {0}.\n {1}".F(frame, order.Order.ToString()));
		}
		
		void OutOfSync(int frame)
		{
			throw new InvalidOperationException("Out of sync in frame {0}.\n".F(frame));
		}
		
		void OutOfSync(int frame, string blame)
		{
			throw new InvalidOperationException("Out of sync in frame {0}: Blame {1}.\n".F(frame, blame));
		}

		public bool IsReadyForNextFrame
		{
			get { return FrameNumber >= 1 && frameData.IsReadyForFrame( FrameNumber ); }
		}

		public void Tick( World world )
		{
			if( !IsReadyForNextFrame )
				throw new InvalidOperationException();

			Connection.Send( localOrders.Serialize( FrameNumber + FramesAhead ) );
			localOrders.Clear();

			var sync = new List<int>();
			sync.Add( world.SyncHash() );

			foreach( var order in frameData.OrdersForFrame( world, FrameNumber) )
			{
				UnitOrders.ProcessOrder( this, world, order.Client, order.Order );
				sync.Add( world.SyncHash() );
			}

			var ss = sync.SerializeSync( FrameNumber );
			Connection.Send( ss );

			syncReport.UpdateSyncReport( world );

			CheckSync( world, ss );

			++FrameNumber;
		}

		bool disposed;
		public void Dispose()
		{
			if (disposed) return;

			Connection.Dispose();

			disposed = true;
			GC.SuppressFinalize(this);
		}

		~OrderManager() { Dispose(); }
	}
}
