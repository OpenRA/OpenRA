using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace OpenRa.Network
{
	enum ConnectionState
	{
		NotConnected,
		Connecting,
		Connected,
	}

	interface IConnection
	{
		int LocalClientId { get; }
		ConnectionState ConnectionState { get; }
		void Send( byte[] packet );
		void Receive( Action<int, byte[]> packetFn );
	}

	class EchoConnection : IConnection
	{
		protected struct ReceivedPacket
		{
			public int FromClient;
			public byte[] Data;
		}
		protected List<ReceivedPacket> receivedPackets = new List<ReceivedPacket>();

		public virtual int LocalClientId
		{
			get { return 1; }
		}

		public virtual ConnectionState ConnectionState
		{
			get { return ConnectionState.Connected; }
		}

		public virtual void Send( byte[] packet )
		{
			lock( this )
				receivedPackets.Add( new ReceivedPacket { FromClient = LocalClientId, Data = packet } );
		}

		public virtual void Receive( Action<int, byte[]> packetFn )
		{
			List<ReceivedPacket> packets;
			lock( this )
			{
				packets = receivedPackets;
				receivedPackets = new List<ReceivedPacket>();
			}

			foreach( var p in packets )
				packetFn( p.FromClient, p.Data );
		}
	}

	class NetworkConnection : EchoConnection
	{
		TcpClient socket;
		int clientId;
		ConnectionState connectionState = ConnectionState.Connecting;

		public NetworkConnection( string host, int port )
		{
			new Thread( _ =>
			{
				try
				{
					socket = new TcpClient( host, port );
					var reader = new BinaryReader( socket.GetStream() );
					clientId = reader.ReadInt32();
					connectionState = ConnectionState.Connected;

					for( ; ; )
					{
						var len = reader.ReadInt32();
						var buf = reader.ReadBytes( len );
						lock( this )
							receivedPackets.Add( new ReceivedPacket { FromClient = -1, Data = buf } );
					}
				}
				catch
				{
					connectionState = ConnectionState.NotConnected;
				}
			}
			) { IsBackground = true }.Start();
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		public override void Send( byte[] packet )
		{
			base.Send( packet );

			var ms = new MemoryStream();
			ms.Write( BitConverter.GetBytes( (int)packet.Length ) );
			ms.Write( packet );
			ms.WriteTo( socket.GetStream() );
		}
	}
}
