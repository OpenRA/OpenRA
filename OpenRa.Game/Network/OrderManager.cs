#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Network
{
	class OrderManager
	{
		int frameNumber = 0;

		public int FramesAhead = 0;

		public bool GameStarted { get { return frameNumber != 0; } }
		public IConnection Connection { get; private set; }

		Dictionary<int, Dictionary<int, byte[]>> frameClientData = 
			new Dictionary<int, Dictionary<int, byte[]>>();
		List<int> readyForFrames = new List<int>();
		List<Order> localOrders = new List<Order>();

		public void StartGame()
		{
			if (GameStarted) return;

			frameNumber = 1;
			for( int i = frameNumber ; i <= FramesAhead ; i++ )
				Connection.Send( new List<Order>().Serialize( i ) );
		}

		public int FrameNumber { get { return frameNumber; } }

		public OrderManager( IConnection conn )
		{
			Connection = conn;
		}

		public OrderManager( IConnection conn, string replayFilename )
			: this( conn )
		{
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
					if( packet.Length == 5 && packet[ 4 ] == 0xEF )
						readyForFrames.Add( frame );
					else if( packet.Length >= 5 && packet[ 4 ] == 0x65 )
						CheckSync( packet );
					else if( frame == 0 )
						immediatePackets.Add( Pair.New( clientId, packet ) );
					else
						frameClientData.GetOrAdd( frame ).Add( clientId, packet );
				} );

			foreach( var p in immediatePackets )
				foreach( var o in p.Second.ToOrderList( world ) )
					UnitOrders.ProcessOrder( world, p.First, o );
		}

		Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();

		void CheckSync( byte[] packet )
		{
			var frame = BitConverter.ToInt32( packet, 0 );
			byte[] existingSync;
			if( syncForFrame.TryGetValue( frame, out existingSync ) )
			{
				if( packet.Length != existingSync.Length )
					OutOfSync( frame );
				else
					for( int i = 0 ; i < packet.Length ; i++ )
						if( packet[ i ] != existingSync[ i ] )
							OutOfSync( frame );
			}
			else
				syncForFrame.Add( frame, packet );
		}

		void OutOfSync( int frame )
		{
			throw new InvalidOperationException( "out of sync in frame {0}".F( frame ) );
		}

		public bool IsReadyForNextFrame
		{
			get { return readyForFrames.Contains( FrameNumber ); }
		}

		public void Tick( World world )
		{
			if( !IsReadyForNextFrame )
				throw new InvalidOperationException();
			readyForFrames.RemoveAll( f => f <= FrameNumber );

			Connection.Send( localOrders.Serialize( FrameNumber + FramesAhead ) );
			localOrders.Clear();

			var frameData = frameClientData[ FrameNumber ];
			var sync = new List<int>();
			sync.Add( world.SyncHash() );

			foreach( var order in frameData.OrderBy( p => p.Key ).SelectMany( o => o.Value.ToOrderList( world ).Select( a => new { Client = o.Key, Order = a } ) ) )
			{
				UnitOrders.ProcessOrder( world, order.Client, order.Order );
				sync.Add( world.SyncHash() );
			}

			var ss = sync.SerializeSync( FrameNumber );
			Connection.Send( ss );
			CheckSync( ss );

			++frameNumber;
		}
	}
}
