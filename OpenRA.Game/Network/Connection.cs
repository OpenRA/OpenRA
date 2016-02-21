#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
	public enum ConnectionState
	{
		PreConnecting,
		NotConnected,
		Connecting,
		Connected,
	}

	public interface IConnection : IDisposable
	{
		int LocalClientId { get; }
		ConnectionState ConnectionState { get; }
		void Send(int frame, List<byte[]> orders);
		void SendImmediate(List<byte[]> orders);
		void SendSync(int frame, byte[] syncData);
		void Receive(Action<int, byte[]> packetFn);
	}

	class EchoConnection : IConnection
	{
		protected struct ReceivedPacket
		{
			public int FromClient;
			public byte[] Data;
		}

		protected List<ReceivedPacket> receivedPackets = new List<ReceivedPacket>();
		public ReplayRecorder Recorder { get; private set; }

		public virtual int LocalClientId
		{
			get { return 1; }
		}

		public virtual ConnectionState ConnectionState
		{
			get { return ConnectionState.PreConnecting; }
		}

		public virtual void Send(int frame, List<byte[]> orders)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			foreach (var o in orders)
				ms.Write(o);
			Send(ms.ToArray());
		}

		public virtual void SendImmediate(List<byte[]> orders)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(0));
			foreach (var o in orders)
				ms.Write(o);
			Send(ms.ToArray());
		}

		public virtual void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			ms.Write(syncData);
			Send(ms.ToArray());
		}

		protected virtual void Send(byte[] packet)
		{
			if (packet.Length == 0)
				throw new NotImplementedException();
			lock (this)
				receivedPackets.Add(new ReceivedPacket { FromClient = LocalClientId, Data = packet });
		}

		public virtual void Receive(Action<int, byte[]> packetFn)
		{
			List<ReceivedPacket> packets;
			lock (this)
			{
				packets = receivedPackets;
				receivedPackets = new List<ReceivedPacket>();
			}

			foreach (var p in packets)
			{
				packetFn(p.FromClient, p.Data);
				if (Recorder != null)
					Recorder.Receive(p.FromClient, p.Data);
			}
		}

		public void StartRecording(Func<string> chooseFilename)
		{
			// If we have a previous recording then save/dispose it and start a new one.
			if (Recorder != null)
				Recorder.Dispose();
			Recorder = new ReplayRecorder(chooseFilename);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && Recorder != null)
				Recorder.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	sealed class NetworkConnection : EchoConnection
	{
		TcpClient socket;
		int clientId;
		ConnectionState connectionState = ConnectionState.Connecting;
		Thread t;

		public NetworkConnection(string host, int port)
		{
			t = new Thread(_ =>
			{
				try
				{
					socket = new TcpClient(host, port);
					socket.NoDelay = true;
					var reader = new BinaryReader(socket.GetStream());
					var serverProtocol = reader.ReadInt32();

					if (ProtocolVersion.Version != serverProtocol)
						throw new InvalidOperationException(
							"Protocol version mismatch. Server={0} Client={1}"
								.F(serverProtocol, ProtocolVersion.Version));

					clientId = reader.ReadInt32();
					connectionState = ConnectionState.Connected;

					for (;;)
					{
						var len = reader.ReadInt32();
						var client = reader.ReadInt32();
						var buf = reader.ReadBytes(len);
						if (len == 0)
							throw new NotImplementedException();
						lock (this)
							receivedPackets.Add(new ReceivedPacket { FromClient = client, Data = buf });
					}
				}
				catch { }
				finally
				{
					connectionState = ConnectionState.NotConnected;
					if (socket != null)
						socket.Close();
				}
			}) { IsBackground = true };
			t.Start();
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		List<byte[]> queuedSyncPackets = new List<byte[]>();

		public override void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			ms.Write(syncData);
			queuedSyncPackets.Add(ms.ToArray());
		}

		protected override void Send(byte[] packet)
		{
			base.Send(packet);

			try
			{
				var ms = new MemoryStream();
				ms.Write(BitConverter.GetBytes(packet.Length));
				ms.Write(packet);

				foreach (var q in queuedSyncPackets)
				{
					ms.Write(BitConverter.GetBytes(q.Length));
					ms.Write(q);
					base.Send(q);
				}

				queuedSyncPackets.Clear();
				ms.WriteTo(socket.GetStream());
			}
			catch (SocketException) { /* drop this on the floor; we'll pick up the disconnect from the reader thread */ }
			catch (ObjectDisposedException) { /* ditto */ }
			catch (InvalidOperationException) { /* ditto */ }
			catch (IOException) { /* ditto */ }
		}

		bool disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			t.Abort();
			if (disposing)
				if (socket != null)
					socket.Client.Close();

			base.Dispose(disposing);
		}

		~NetworkConnection()
		{
			Dispose(false);
		}
	}
}
