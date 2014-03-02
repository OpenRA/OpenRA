#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Network
{
	public class ReplayConnection : IConnection
	{
		FileStream replayStream;
		List<byte[]> sync = new List<byte[]>();

		public int LocalClientId { get { return 0; } }
		public ConnectionState ConnectionState { get { return ConnectionState.Connected; } }

		public ReplayConnection(string replayFilename)
		{
			replayStream = File.OpenRead(replayFilename);
		}

		// do nothing; ignore locally generated orders
		public void Send(int frame, List<byte[]> orders) { }
		public void SendImmediate(List<byte[]> orders) { }

		public void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			ms.Write(syncData);
			sync.Add(ms.ToArray());
		}

		public void Receive(Action<int, byte[]> packetFn)
		{
			while (sync.Count != 0)
			{
				packetFn(LocalClientId, sync[0]);
				sync.RemoveAt(0);
			}

			if (replayStream == null)
				return;

			var reader = new BinaryReader(replayStream);

			while (replayStream.Position < replayStream.Length)
			{
				var client = reader.ReadInt32();
				var packetLen = reader.ReadInt32();
				var packet = reader.ReadBytes(packetLen);
				packetFn(client, packet);
			}

			replayStream = null;
		}

		public void Dispose() { }
	}
}
