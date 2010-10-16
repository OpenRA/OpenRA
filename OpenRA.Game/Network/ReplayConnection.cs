using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRA.Network
{
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

		// do nothing; ignore locally generated orders
		public void Send( int frame, List<byte[]> orders ) { }
		public void SendImmediate( List<byte[]> orders ) { }
		public void SendSync( int frame, byte[] syncData ) { }

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
			}
			replayStream = null;
		}

		public void Dispose() { }
	}

	class ReplayRecorderConnection : IConnection
	{
		IConnection inner;
		BinaryWriter writer;

		public ReplayRecorderConnection( IConnection inner, FileStream replayFile )
		{
			this.inner = inner;
			this.writer = new BinaryWriter( replayFile );
		}

		public int LocalClientId { get { return inner.LocalClientId; } }
		public ConnectionState ConnectionState { get { return inner.ConnectionState; } }

		public void Send( int frame, List<byte[]> orders ) { inner.Send( frame, orders ); }
		public void SendImmediate( List<byte[]> orders ) { inner.SendImmediate( orders ); }
		public void SendSync( int frame, byte[] syncData ) { inner.SendSync( frame, syncData ); }

		public void Receive( Action<int, byte[]> packetFn )
		{
			inner.Receive( ( client, data ) =>
				{
					writer.Write( client );
					writer.Write( data.Length );
					writer.Write( data );
					packetFn( client, data );
				} );
		}

		bool disposed;

		public void Dispose()
		{
			if( disposed )
				return;

			writer.Close();
			disposed = true;
		}

		~ReplayRecorderConnection()
		{
			Dispose();
		}
	}
}
