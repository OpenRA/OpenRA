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

		public void Send( byte[] packet )
		{
			inner.Send( packet );
		}

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
