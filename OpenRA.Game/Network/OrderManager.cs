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
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	class OrderManager
	{
		public int FrameNumber { get; private set; }

		public int FramesAhead = 0;

		public bool GameStarted { get { return FrameNumber != 0; } }
		public IConnection Connection { get; private set; }
		
		public readonly int SyncHeaderSize = 5;
		
		Dictionary<int, int> clientQuitTimes = new Dictionary<int, int>();

		Dictionary<int, Dictionary<int, byte[]>> frameClientData = 
			new Dictionary<int, Dictionary<int, byte[]>>();
		List<Order> localOrders = new List<Order>();

		FileStream replaySaveFile;

		public void StartGame()
		{
			if (GameStarted) return;

			FrameNumber = 1;
			for( int i = FrameNumber ; i <= FramesAhead ; i++ )
				Connection.Send( new List<Order>().Serialize( i ) );
		}

		public OrderManager( IConnection conn )
		{
			Connection = conn;
		}

		public OrderManager( IConnection conn, string replayFilename )
			: this( conn )
		{
			replaySaveFile = File.Create( replayFilename );
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
						clientQuitTimes[ clientId ] = frame;
					else if( packet.Length >= 5 && packet[ 4 ] == 0x65 )
						CheckSync( packet );
					else if( frame == 0 )
						immediatePackets.Add( Pair.New( clientId, packet ) );
					else
						frameClientData.GetOrAdd( frame ).Add( clientId, packet );
				} );

			foreach( var p in immediatePackets )
			{
				foreach( var o in p.Second.ToOrderList( world ) )
					UnitOrders.ProcessOrder( world, p.First, o );
				WriteImmediateToReplay( immediatePackets );
			}
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
				{
					for( int i = 0 ; i < packet.Length ; i++ )
					{
						if( packet[ i ] != existingSync[ i ] )
						{
							if ( i < SyncHeaderSize + sizeof(int) )
								OutOfSync(frame, "Tick");
							else
							OutOfSync( frame ,  (i - SyncHeaderSize - sizeof(int)) / 4);
						}
					}
				}
			}
			else
				syncForFrame.Add( frame, packet );
		}

		void OutOfSync( int frame , int index)
		{	
			var frameData = clientQuitTimes
				.Where( x => frame <= x.Value )
				.OrderBy( x => x.Key )
				.ToDictionary( k => k.Key, v => frameClientData[ FrameNumber ][ v.Key ] );
			
			var order = frameData.SelectMany( o => o.Value.ToOrderList( Game.world ).Select( a => new { Client = o.Key, Order = a } ) ).ElementAt(index);
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
			get
			{
				return FrameNumber > 0 &&
					clientQuitTimes
						.Where( x => FrameNumber <= x.Value )
						.All( x => frameClientData.GetOrAdd( FrameNumber ).ContainsKey( x.Key ) );
			}
		}

		public void Tick( World world )
		{
			if( !IsReadyForNextFrame )
				throw new InvalidOperationException();

			Connection.Send( localOrders.Serialize( FrameNumber + FramesAhead ) );
			localOrders.Clear();

			var frameData = clientQuitTimes
				.Where( x => FrameNumber <= x.Value )
				.OrderBy( x => x.Key )
				.ToDictionary( k => k.Key, v => frameClientData[ FrameNumber ][ v.Key ] );
			var sync = new List<int>();
			sync.Add( world.SyncHash() );

			foreach( var order in frameData.SelectMany( o => o.Value.ToOrderList( world ).Select( a => new { Client = o.Key, Order = a } ) ) )
			{
				UnitOrders.ProcessOrder( world, order.Client, order.Order );
				sync.Add( world.SyncHash() );
			}

			var ss = sync.SerializeSync( FrameNumber );
			Connection.Send( ss );
			WriteToReplay( frameData, ss );

			CheckSync( ss );

			++FrameNumber;
		}

		void WriteToReplay( Dictionary<int, byte[]> frameData, byte[] syncData )
		{
			if( replaySaveFile == null ) return;

			foreach( var f in frameData )
			{
				replaySaveFile.Write( BitConverter.GetBytes( f.Key ) );
				replaySaveFile.Write( BitConverter.GetBytes( f.Value.Length ) );
				replaySaveFile.Write( f.Value );
			}
			replaySaveFile.Write( BitConverter.GetBytes( (int)0 ) );
			replaySaveFile.Write( BitConverter.GetBytes( (int)syncData.Length ) );
			replaySaveFile.Write( syncData );
		}

		void WriteImmediateToReplay( List<Pair<int, byte[]>> immediatePackets )
		{
			if( replaySaveFile == null ) return;

			foreach( var i in immediatePackets )
			{
				replaySaveFile.Write( BitConverter.GetBytes( i.First ) );
				replaySaveFile.Write( BitConverter.GetBytes( i.Second.Length ) );
				replaySaveFile.Write( i.Second );
			}
		}
	}
}
