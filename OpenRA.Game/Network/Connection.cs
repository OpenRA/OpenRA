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
using System.Net.Sockets;
using System.Threading;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	enum ConnectionState
	{
		PreConnecting,
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
			get { return ConnectionState.PreConnecting; }
		}

		public virtual void Send( byte[] packet )
		{
			if( packet.Length == 0 )
				throw new NotImplementedException();
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
					socket.NoDelay = true;
					var reader = new BinaryReader( socket.GetStream() );
					var serverProtocol = reader.ReadInt32();

					if (ProtocolVersion.Version != serverProtocol)
						throw new InvalidOperationException(
							"Protocol version mismatch. Server={0} Client={1}"
								.F(serverProtocol, ProtocolVersion.Version));

					clientId = reader.ReadInt32();
					connectionState = ConnectionState.Connected;

					for( ; ; )
					{
						var len = reader.ReadInt32();
						var client = reader.ReadInt32();
						var buf = reader.ReadBytes( len );
						if( len == 0 )
							throw new NotImplementedException();
						lock( this )
							receivedPackets.Add( new ReceivedPacket { FromClient = client, Data = buf } );
					}
				}
				catch( SocketException )
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

	class ReplayConnection : IConnection
	{
		//uint nextFrame = 1;
		FileStream replayStream;

		public ReplayConnection( string replayFilename )
		{
			replayStream = File.OpenRead( replayFilename );
		}

		public int LocalClientId
		{
			get { return 0; }
		}

		public ConnectionState ConnectionState
		{
			get { return ConnectionState.Connected; }
		}

		public void Send( byte[] packet )
		{
			// do nothing; ignore locally generated orders
		}

		public void Receive( Action<int, byte[]> packetFn )
		{
			if( replayStream == null ) return;

			var reader = new BinaryReader( replayStream );
			while( replayStream.Position < replayStream.Length )
			{
				var client = reader.ReadInt32();
				var packetLen = reader.ReadInt32();
				var packet = reader.ReadBytes( packetLen );
				packetFn( client, packet );

				if( !Game.orderManager.GameStarted )
					return;
			}
			replayStream = null;
		}
	}
}
