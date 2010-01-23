using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRa.Network
{
	class OrderManager
	{
		Stream savingReplay;
		int frameNumber = 0;

		public int FramesAhead = 0;

		public bool GameStarted { get { return frameNumber != 0; } }
		public IConnection Connection { get; private set; }

		Dictionary<int, Dictionary<int, byte[]>> frameClientData = new Dictionary<int, Dictionary<int, byte[]>>();
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
			savingReplay = new FileStream( replayFilename, FileMode.Create );
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

			var immediatePackets = new List<byte[]>();

			Connection.Receive(
				( clientId, packet ) =>
				{
					var frame = BitConverter.ToInt32( packet, 0 );
					if( packet.Length == 5 && packet[ 4 ] == 0xEF )
						readyForFrames.Add( frame );
					else if( packet.Length >= 5 && packet[ 4 ] == 0x65 )
						CheckSync( packet );
					else if( frame == 0 )
						immediatePackets.Add( packet );
					else
						frameClientData.GetOrAdd( frame ).Add( clientId, packet );
				} );

			foreach( var p in immediatePackets )
				foreach( var o in p.ToOrderList( world ) )
					UnitOrders.ProcessOrder( o );
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

			foreach( var order in frameData.OrderBy( p => p.Key ).SelectMany( o => o.Value.ToOrderList( world ) ) )
			{
				UnitOrders.ProcessOrder( order );
				sync.Add( world.SyncHash() );
			}

			var ss = sync.SerializeSync( FrameNumber );
			Connection.Send( ss );
			CheckSync( ss );

			++frameNumber;
		}
	}
}
