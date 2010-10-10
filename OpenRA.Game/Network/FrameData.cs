using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Network
{
	class FrameData
	{
		public struct ClientOrder
		{
			public int Client;
			public Order Order;
		}

		readonly Dictionary<int, int> clientQuitTimes = new Dictionary<int, int>();
		readonly Dictionary<int, Dictionary<int, byte[]>> framePackets = new Dictionary<int, Dictionary<int, byte[]>>();

		public IEnumerable<int> ClientsPlayingInFrame( int frame )
		{
			return clientQuitTimes
				.Where( x => frame <= x.Value )
				.Select( x => x.Key )
				.OrderBy( x => x );
		}

		public void ClientQuit( int clientId, int lastClientFrame )
		{
			clientQuitTimes.Add( clientId, lastClientFrame );
		}

		public void AddFrameOrders( int clientId, int frame, byte[] orders )
		{
			var frameData = framePackets.GetOrAdd( frame );
			frameData.Add( clientId, orders );
		}

		public bool IsReadyForFrame( int frame )
		{
			var frameData = framePackets.GetOrAdd( frame );
			return ClientsPlayingInFrame( frame )
				.All( client => frameData.ContainsKey( client ) );
		}

		public IEnumerable<ClientOrder> OrdersForFrame( World world, int frame )
		{
			var frameData = framePackets[ frame ];
			var clientData = ClientsPlayingInFrame( frame )
				.ToDictionary( k => k, v => frameData[ v ] );

			return clientData
				.SelectMany( x => x.Value
					.ToOrderList( world )
					.Select( o => new ClientOrder { Client = x.Key, Order = o } ) );
		}
	}
}
