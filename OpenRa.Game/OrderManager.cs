using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using IjwFramework.Types;
using System.Net.Sockets;
using System.Threading;

namespace OpenRa.Game
{
	class OrderManager
	{
		BinaryWriter savingReplay;
		List<OrderSource> players;
		int frameNumber = 0;

		const int FramesAhead = 3;

		public int FrameNumber { get { return frameNumber; } }

		public OrderManager( IEnumerable<OrderSource> players )
		{
			this.players = players.ToList();

			foreach( var p in this.players )
				for( int i = 0 ; i < FramesAhead ; i++ )
					p.SendLocalOrders( i, new List<Order>() );
		}

		public OrderManager( IEnumerable<OrderSource> players, string replayFilename )
			: this( players )
		{
			savingReplay = new BinaryWriter( new FileStream( replayFilename, FileMode.Create ) );
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

			if( savingReplay != null )
				savingReplay.Write( frameNumber );

			var allOrders = players.SelectMany(p => p.OrdersForFrame(frameNumber)).OrderBy(o => o.Player.Index);
			foreach (var order in allOrders)
			{
				UnitOrders.ProcessOrder(order);
				if (savingReplay != null)
					savingReplay.Write(order.Serialize());
			}

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
			replayReader.ReadUInt32();
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders ) { }

		public List<Order> OrdersForFrame( int frameNumber )
		{
			var ret = new List<Order>();
			while( true )
			{
				try
				{
					var first = replayReader.ReadUInt32();
					var order = Order.Deserialize( replayReader, first );
					if( order == null )
					{
						if( (uint)frameNumber + 1 != first )
							throw new NotImplementedException();
						return ret;
					}
					ret.Add( order );
				}
				catch( EndOfStreamException )
				{
					return ret;
				}
			}
		}

		public bool IsReadyForFrame( int frameNumber )
		{
			return true;
		}
	}

	class NetworkOrderSource : OrderSource
	{
		int nextLocalOrderFrame = 0;
		TcpClient socket;

		Dictionary<int, List<Order>> orders = new Dictionary<int,List<Order>>();

		public NetworkOrderSource( TcpClient socket )
		{
			this.socket = socket;
			var reader = new BinaryReader( socket.GetStream() );

			var nextFrameId = System.BitConverter.GetBytes( nextLocalOrderFrame );
			socket.GetStream().Write( nextFrameId, 0, nextFrameId.Length );

			new Thread( () =>
			{
				var firstFrameNum = reader.ReadInt32();
				if( firstFrameNum != 0 )
					throw new InvalidOperationException( "Wrong frame number at start of stream" );

				var currentFrame = 0;
				var first = reader.ReadUInt32();
				while( true )
				{
					var ret = new List<Order>();
					while( true )
					{
						if( first == currentFrame + 1 )
						{
							lock( orders )
								orders[ currentFrame ] = ret;
							ret = new List<Order>();
							++currentFrame;
							break;
						}
						ret.Add( Order.Deserialize( reader, first ) );
					}
				}
			} ) { IsBackground = true }.Start();
			
		}

		public List<Order> OrdersForFrame( int currentFrame )
		{
			// TODO: prune `orders` based on currentFrame.
			lock( orders )
				return orders[ currentFrame ];
		}

		public void SendLocalOrders( int localFrame, List<Order> localOrders )
		{
			if( nextLocalOrderFrame != localFrame )
				throw new InvalidOperationException( "Attempted time-travel in NetworkOrderSource.SendLocalOrders()" );

			foreach( var order in localOrders )
			{
				var bytes = order.Serialize();
				socket.GetStream().Write( bytes, 0, bytes.Length );
			}

			++nextLocalOrderFrame;
			var nextFrameId = System.BitConverter.GetBytes( nextLocalOrderFrame );
			socket.GetStream().Write( nextFrameId, 0, nextFrameId.Length );
		}

		public bool IsReadyForFrame( int frameNumber )
		{
			lock( orders )
				return orders.ContainsKey( frameNumber );
		}
	}
}
