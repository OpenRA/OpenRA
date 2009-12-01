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
		List<OrderSource> sources;
		int frameNumber = 0;

		const int FramesAhead = 3;

		public bool GameStarted { get { return frameNumber != 0; } }

		public void StartGame()
		{
			if (GameStarted) return;

			frameNumber = 1;
			foreach (var p in this.sources)
				for (int i = frameNumber; i <= FramesAhead; i++)
					p.SendLocalOrders(i, new List<Order>());
		}

		public int FrameNumber { get { return frameNumber; } }

		public OrderManager( IEnumerable<OrderSource> sources )
		{
			this.sources = sources.ToList();
		}

		public OrderManager( IEnumerable<OrderSource> sources, string replayFilename )
			: this( sources )
		{
			savingReplay = new FileStream( replayFilename, FileMode.Create );
		}

		public bool IsReadyForNextFrame
		{
			get
			{
				foreach( var p in sources )
					if( !p.IsReadyForFrame( frameNumber ) )
						return false;
				return true;
			}
		}

		public void TickImmediate()
		{
			var localOrders = Game.controller.GetRecentOrders(true);
			if (localOrders.Count > 0)
				foreach (var p in sources)
					p.SendLocalOrders(0, localOrders);

			var immOrders = sources.SelectMany( p => p.OrdersForFrame(0) ).OrderBy(o => o.Player.Index).ToList();
			foreach (var order in immOrders)
				UnitOrders.ProcessOrder(order);
		}

		public void Tick()
		{
			var localOrders = Game.controller.GetRecentOrders(false);

			foreach( var p in sources )
				p.SendLocalOrders( frameNumber + FramesAhead, localOrders );

			var allOrders = sources.SelectMany(p => p.OrdersForFrame(frameNumber)).OrderBy(o => o.Player.Index).ToList();

			foreach (var order in allOrders)
				UnitOrders.ProcessOrder(order);

			if( savingReplay != null )
				savingReplay.WriteFrameData( allOrders, frameNumber );

			++frameNumber;

			// sanity check on the framenumber. This is 2^31 frames maximum, or multiple *years* at 40ms/frame.
			if( ( frameNumber & 0x80000000 ) != 0 )
				throw new InvalidOperationException( "(OrderManager) Frame number too large" );
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
			if (!orders.ContainsKey(currentFrame))
				return new List<Order>();
			return orders[ currentFrame ];
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders )
		{
			if (localFrame == 0) return;
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
				var len = replayReader.ReadInt32() - 4;
				var frame = replayReader.ReadInt32();
				var ret = replayReader.ReadBytes( len ).ToOrderList();

				if( frameNumber != frame )
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
		TcpClient socket;

		Dictionary<int, List<byte[]>> orderBuffers = new Dictionary<int, List<byte[]>>();

		public NetworkOrderSource( TcpClient socket )
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			var reader = new BinaryReader( socket.GetStream() );

			new Thread( () =>
			{
				for (; ; )
				{
					var len = reader.ReadInt32();
					var frame = reader.ReadInt32();
					var buf = reader.ReadBytes(len - 4);

					lock (orderBuffers)
					{
						/* accumulate this chunk */
						if (!orderBuffers.ContainsKey(frame))
							orderBuffers[frame] = new List<byte[]> { buf };
						else
							orderBuffers[frame].Add(buf);
					}
				}
			} ) { IsBackground = true }.Start();
		}

		static List<byte[]> NoOrders = new List<byte[]>();
		List<byte[]> ExtractOrders(int frame)
		{
			lock (orderBuffers)
			{
				List<byte[]> result;
				if (!orderBuffers.TryGetValue(frame, out result))
					result = NoOrders;
				orderBuffers.Remove(frame);
				return result;
			}
		}

		public List<Order> OrdersForFrame( int currentFrame )
		{
			var orderData = ExtractOrders(currentFrame);
			return orderData.SelectMany(a => a.ToOrderList()).ToList();
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders )
		{
			socket.GetStream().WriteFrameData( localOrders, localFrame );
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
		public static MemoryStream ToMemoryStream( this IEnumerable<Order> orders, int nextLocalOrderFrame )
		{
			var ms = new MemoryStream();
			ms.Write( BitConverter.GetBytes( nextLocalOrderFrame ) );
			foreach( var order in orders )
				ms.Write( order.Serialize() );
			return ms;
		}

		public static void WriteFrameData( this Stream s, IEnumerable<Order> orders, int frameNumber )
		{
			var ms = orders.ToMemoryStream( frameNumber );
			s.Write( BitConverter.GetBytes( (int)ms.Length ) );
			ms.WriteTo( s );
		}

		public static List<Order> ToOrderList( this byte[] bytes )
		{
			var ms = new MemoryStream( bytes );
			var reader = new BinaryReader( ms );
			var ret = new List<Order>();
			while( ms.Position < ms.Length )
				ret.Add( Order.Deserialize( reader ) );
			return ret;
		}
	}
}
