using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using IjwFramework.Types;

namespace OpenRa.Game
{
	class OrderManager
	{
		BinaryWriter savingReplay;
		List<OrderSource> players;
		int frameNumber = 0;

		public OrderManager( IEnumerable<OrderSource> players )
		{
			this.players = players.ToList();
		}

		public OrderManager( IEnumerable<OrderSource> players, string replayFilename )
			: this( players )
		{
			savingReplay = new BinaryWriter( new FileStream( replayFilename, FileMode.Create ) );
		}

		public void Tick()
		{
			var localOrders = Game.controller.GetRecentOrders();
			foreach( var p in players )
				p.Tick( localOrders );

			if( savingReplay != null )
				savingReplay.Write( frameNumber );

			foreach( var p in players )
			{
				foreach( var order in p.OrdersForFrame( frameNumber ) )
				{
					UnitOrders.ProcessOrder( order );
					if( savingReplay != null )
						savingReplay.Write( order.Serialize() );
				}
			}
			++frameNumber;
			// sanity check on the framenumber. This is 2^31 frames maximum, or multiple *years* at 40ms/frame.
			if( ( frameNumber & 0x80000000 ) != 0 )
				throw new InvalidOperationException( "(OrderManager) Frame number too large" );
		}
	}

	interface OrderSource
	{
		void Tick( List<Order> localOrders );
		List<Order> OrdersForFrame( int frameNumber );
	}

	class LocalOrderSource : OrderSource
	{
		List<Order> orders;

		public void Tick( List<Order> localOrders )
		{
			orders = localOrders;
		}

		public List<Order> OrdersForFrame( int frameNumber )
		{
			return orders;
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

		public void Tick( List<Order> localOrders )
		{
		}

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
	}
}
