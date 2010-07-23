#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using OpenRA.Server;

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

	class NetworkConnection : EchoConnection, IDisposable
	{
		TcpClient socket;
		int clientId;
		ConnectionState connectionState = ConnectionState.Connecting;
		Thread t;

		public NetworkConnection( string host, int port )
		{
			t = new Thread( _ =>
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
			) { IsBackground = true };
			t.Start();
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		public override void Send( byte[] packet )
		{
			base.Send( packet );

			try
			{

				var ms = new MemoryStream();
				ms.Write(BitConverter.GetBytes((int)packet.Length));
				ms.Write(packet);
				ms.WriteTo(socket.GetStream());

			}
			catch (SocketException) { /* drop this on the floor; we'll pick up the disconnect from the reader thread */ }
		}
		bool disposed = false;
		public void Dispose ()
		{
			if (disposed) return;
			disposed = true;
			GC.SuppressFinalize( this );

			socket.Close();
			t.Abort();
		}
		
		~NetworkConnection() { Dispose(); }
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
