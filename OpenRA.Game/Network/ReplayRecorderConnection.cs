#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRA.Network
{
	class ReplayRecorderConnection : IConnection
	{
		IConnection inner;
		BinaryWriter writer;
		Func<string> chooseFilename;

		public ReplayRecorderConnection( IConnection inner, Func<string> chooseFilename )
		{
			this.chooseFilename = chooseFilename;
			this.inner = inner;

			StartSavingReplay();
		}

		void StartSavingReplay()
		{
			var filename = chooseFilename();
			var replayPath = Path.Combine( Platform.SupportDir, "Replays" );

			if (!Directory.Exists(replayPath))
				Directory.CreateDirectory(replayPath);

			this.writer = new BinaryWriter(File.Create(Path.Combine(replayPath, filename)));
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
			inner.Dispose();
			disposed = true;
		}

		~ReplayRecorderConnection()
		{
			Dispose();
		}
	}
}

