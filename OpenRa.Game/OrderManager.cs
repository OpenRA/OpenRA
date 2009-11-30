using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace OpenRa.Game
{
	class OrderManager
	{
		Stream savingReplay;
		List<OrderSource> players;
		int frameNumber = 1;

		const int FramesAhead = 3;

		public int FrameNumber { get { return frameNumber; } }

		public OrderManager( IEnumerable<OrderSource> players )
		{
			this.players = players.ToList();

			foreach( var p in this.players )
				for( int i = 1 ; i <= FramesAhead ; i++ )
					p.SendLocalOrders( i, new List<Order>() );
		}

		public OrderManager( IEnumerable<OrderSource> players, string replayFilename )
			: this( players )
		{
			savingReplay = new FileStream( replayFilename, FileMode.Create );
		}

		public bool IsReadyForNextFrame
		{
			get
			{
				foreach( var p in players )
					if( !p.IsReadyForFrame( frameNumber ) )
						return false;
				return true;
			}
		}

		public void Tick()
		{
			var localOrders = Game.controller.GetRecentOrders();

			foreach( var p in players )
				p.SendLocalOrders( frameNumber + FramesAhead, localOrders );

			var allOrders = players.SelectMany(p => p.OrdersForFrame(frameNumber)).OrderBy(o => o.Player.Index).ToList();
			foreach (var order in allOrders)
				UnitOrders.ProcessOrder(order);

			if( savingReplay != null )
				savingReplay.WriteFrameData( allOrders, frameNumber );

			++frameNumber;
			// sanity check on the framenumber. This is 2^31 frames maximum, or multiple *years* at 40ms/frame.
			if( ( frameNumber & 0x80000000 ) != 0 )
				throw new InvalidOperationException( "(OrderManager) Frame number too large" );

			return;
		}
	}

	interface OrderSource
	{
		void SendLocalOrders( int localFrame, List<Order> localOrders );
		List<Order> OrdersForFrame( int currentFrame );
		bool IsReadyForFrame( int frameNumber );
	}

	class LocalOrderSource : OrderSource
	{
		Dictionary<int, List<Order>> orders = new Dictionary<int,List<Order>>();

		public List<Order> OrdersForFrame( int currentFrame )
		{
			// TODO: prune `orders` based on currentFrame.
			return orders[ currentFrame ];
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders )
		{
			orders[ localFrame ] = localOrders;
		}

		public bool IsReadyForFrame( int frameNumber )
		{
			return true;
		}
	}

	class ReplayOrderSource : OrderSource
	{
		BinaryReader replayReader;
		public ReplayOrderSource( string replayFilename )
		{
			replayReader = new BinaryReader( File.Open( replayFilename, FileMode.Open ) );
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders ) { }

		public List<Order> OrdersForFrame( int frameNumber )
		{
			try
			{
				uint fn;

				var len = replayReader.ReadInt32();
				var ret = replayReader.ReadBytes( len ).ToOrderList( out fn );

				if( frameNumber != fn )
					throw new InvalidOperationException( "Attempted time-travel in OrdersForFrame (replay)" );

				return ret;
			}
			catch( EndOfStreamException )
			{
				return new List<Order>();
			}
		}

		public bool IsReadyForFrame( int frameNumber )
		{
			return true;
		}
	}

	class NetworkOrderSource : OrderSource
	{
		int nextLocalOrderFrame = 1;
		TcpClient socket;

		Dictionary<int, byte[]> orderBuffers = new Dictionary<int, byte[]>();

		public NetworkOrderSource( TcpClient socket )
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			var reader = new BinaryReader( socket.GetStream() );

			var nextFrameId = BitConverter.GetBytes( nextLocalOrderFrame );
			socket.GetStream().Write( nextFrameId, 0, nextFrameId.Length );

			new Thread( () =>
			{
				var firstFrameNum = reader.ReadInt32();
				if( firstFrameNum != 0 )
					throw new InvalidOperationException( "Wrong frame number at start of stream" );

				var currentFrame = 0;
				var ret = new List<Order>();
				while( true )
				{
					var len = reader.ReadInt32();
					var buf = reader.ReadBytes( len );

					lock( orderBuffers )
						orderBuffers[ currentFrame ] = buf;
					++currentFrame;
				}
			} ) { IsBackground = true }.Start();
		}

		public List<Order> OrdersForFrame( int currentFrame )
		{
			// TODO: prune `orderBuffers` based on currentFrame.
			byte[] orderBuffer;
			lock( orderBuffers )
				orderBuffer = orderBuffers[ currentFrame ];

			uint frameNumber;
			var ret = orderBuffer.ToOrderList( out frameNumber );

			if( frameNumber != currentFrame )
				throw new InvalidOperationException( "Attempted time-travel in OrdersForFrame (network)" );

			return ret;
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders )
		{
			if( nextLocalOrderFrame != localFrame )
				throw new InvalidOperationException( "Attempted time-travel in NetworkOrderSource.SendLocalOrders()" );

			socket.GetStream().WriteFrameData( localOrders, nextLocalOrderFrame++ );
		}

		public bool IsReadyForFrame( int frameNumber )
		{
			lock( orderBuffers )
				return orderBuffers.ContainsKey( frameNumber );
		}
	}

	static class StreamExts
	{
		public static void Write( this Stream s, byte[] buf )
		{
			s.Write( buf, 0, buf.Length );
		}
	}

	static class OrderIO
	{
		public static MemoryStream ToMemoryStream( this List<Order> orders, int nextLocalOrderFrame )
		{
			var ms = new MemoryStream();
			ms.Write( BitConverter.GetBytes( nextLocalOrderFrame ) );
			foreach( var order in orders )
				ms.Write( order.Serialize() );
			return ms;
		}

		public static void WriteFrameData( this Stream s, List<Order> orders, int frameNumber )
		{
			var ms = orders.ToMemoryStream( frameNumber );
			s.Write( BitConverter.GetBytes( (int)ms.Length ) );
			ms.WriteTo( s );
		}

		public static List<Order> ToOrderList( this byte[] bytes, out uint frameNumber )
		{
			var ms = new MemoryStream( bytes );
			var reader = new BinaryReader( ms );
			frameNumber = reader.ReadUInt32();
			var ret = new List<Order>();
			while( ms.Position < ms.Length )
				ret.Add( Order.Deserialize( reader ) );
			return ret;
		}
	}
}
