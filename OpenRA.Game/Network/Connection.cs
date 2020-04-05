#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using OpenRA.Primitives;
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
		void Send(int frame, IEnumerable<byte[]> orders);
		void SendImmediate(IEnumerable<byte[]> orders);
		void SendSync(int frame, byte[] syncData);
		void Receive(Action<int, byte[]> packetFn);
		int LastAckedFrame { get; }
		ILatencyReporter LatencyReporter { get; }
	}

	class EchoConnection : IConnection
	{
		protected struct ReceivedPacket
		{
			public int FromClient;
			public byte[] Data;
		}

		readonly List<ReceivedPacket> receivedPackets = new List<ReceivedPacket>();
		public ReplayRecorder Recorder { get; private set; }

		public virtual ILatencyReporter LatencyReporter
		{
			get { return EmptyLatencyReporter.Instance; }
		}

		public virtual int LocalClientId
		{
			get { return 1; }
		}

		public virtual ConnectionState ConnectionState
		{
			get { return ConnectionState.PreConnecting; }
		}

		public int LastAckedFrame { get; protected set; }

		public virtual void Send(int frame, IEnumerable<byte[]> orders)
		{
			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frame));
			foreach (var o in orders)
				ms.WriteArray(o);
			Send(ms.ToArray());
			LastAckedFrame = frame;
		}

		public virtual void SendImmediate(IEnumerable<byte[]> orders)
		{
			foreach (var o in orders)
			{
				var ms = new MemoryStream();
				ms.WriteArray(BitConverter.GetBytes(0));
				ms.WriteArray(o);
				Send(ms.ToArray());
			}
		}

		public virtual void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream(4 + syncData.Length);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(syncData);
			Send(ms.ToArray());
		}

		protected virtual void Send(byte[] packet)
		{
			if (packet.Length == 0)
				throw new NotImplementedException();

			AddPacket(new ReceivedPacket { FromClient = LocalClientId, Data = packet });
		}

		protected void AddPacket(ReceivedPacket packet)
		{
			lock (receivedPackets)
				receivedPackets.Add(packet);
		}

		public virtual void Receive(Action<int, byte[]> packetFn)
		{
			ReceivedPacket[] packets;
			lock (receivedPackets)
			{
				packets = receivedPackets.ToArray();
				receivedPackets.Clear();
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
		readonly TcpClient tcp;
		readonly List<byte[]> queuedSyncPackets = new List<byte[]>();
		readonly ConcurrentQueue<Pair<int, byte[]>> awaitingAckPackets = new ConcurrentQueue<Pair<int, byte[]>>();
		volatile ConnectionState connectionState = ConnectionState.Connecting;
		volatile int clientId;
		bool disposed;

		readonly OrderLatencyTracker latencyTracker = new OrderLatencyTracker();

		public override ILatencyReporter LatencyReporter
		{
			get { return latencyTracker; }
		}

		public NetworkConnection(string host, int port)
		{
			try
			{
				tcp = new TcpClient(host, port) { NoDelay = true };
				new Thread(NetworkConnectionReceive)
				{
					Name = GetType().Name + " " + host + ":" + port,
					IsBackground = true
				}.Start(tcp.GetStream());
			}
			catch
			{
				connectionState = ConnectionState.NotConnected;
			}
		}

		void NetworkConnectionReceive(object networkStreamObject)
		{
			try
			{
				var networkStream = (NetworkStream)networkStreamObject;
				var reader = new BinaryReader(networkStream);
				var handshakeProtocol = reader.ReadInt32();

				if (handshakeProtocol != ProtocolVersion.Handshake)
					throw new InvalidOperationException(
						"Handshake protocol version mismatch. Server={0} Client={1}"
							.F(handshakeProtocol, ProtocolVersion.Handshake));

				clientId = reader.ReadInt32();
				connectionState = ConnectionState.Connected;

				while (true)
				{
					var len = reader.ReadInt32();
					var client = reader.ReadInt32();
					var buf = reader.ReadBytes(len);

					if (client == LocalClientId && len == 7 && buf[4] == (byte)OrderType.Ack)
					{
						Ack(buf);
					}
					else if (len == 0)
						throw new NotImplementedException();
					else
						AddPacket(new ReceivedPacket { FromClient = client, Data = buf });
				}
			}
			catch { }
			finally
			{
				connectionState = ConnectionState.NotConnected;
			}
		}

		void Ack(byte[] buf)
		{
			var reader = new BinaryReader(new MemoryStream(buf));
			var frameReceived = reader.ReadInt32();
			reader.ReadByte();
			var framesToAck = reader.ReadInt16();

			LastAckedFrame += framesToAck;

			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frameReceived));

			for (int i = 0; i < framesToAck; i++)
			{
				Pair<int, byte[]> queuedPacket;
				if (!awaitingAckPackets.TryDequeue(out queuedPacket))
				{
					throw new InvalidOperationException("Received acks for unknown frames");
				}

				var queuedFrame = queuedPacket.First;
				latencyTracker.TrackAck(queuedFrame);

				ms.WriteArray(queuedPacket.Second);
			}

			AddPacket(new ReceivedPacket { FromClient = LocalClientId, Data = ms.ToArray() });
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		public override void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream(4 + syncData.Length);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(syncData);

			queuedSyncPackets.Add(ms.ToArray()); // TODO: re-add sync packets
		}

		// Override send frame orders so we can hold them until ACK'ed
		public override void Send(int frame, IEnumerable<byte[]> orders)
		{
			var ackMs = new MemoryStream();
			var enumerable = orders as byte[][] ?? orders.ToArray();
			foreach (var o in enumerable)
				ackMs.WriteArray(o);

			awaitingAckPackets.Enqueue(new Pair<int, byte[]>(frame, ackMs.ToArray())); // TODO fix having to write byte buffer twice

			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frame));
			foreach (var o in enumerable)
				ms.WriteArray(o);
			byte[] packet = ms.ToArray();

			latencyTracker.TrackSend(frame);
			SendNetwork(packet);
		}

		protected override void Send(byte[] packet)
		{
			base.Send(packet);
			SendNetwork(packet);
		}

		void SendNetwork(byte[] packet)
		{
			try
			{
				var ms = new MemoryStream();
				ms.WriteArray(BitConverter.GetBytes(packet.Length));
				ms.WriteArray(packet);

				foreach (var q in queuedSyncPackets)
				{
					ms.WriteArray(BitConverter.GetBytes(q.Length));
					ms.WriteArray(q);
					base.Send(q);
				}

				queuedSyncPackets.Clear();
				ms.WriteTo(tcp.GetStream());
			}
			catch (SocketException) { /* drop this on the floor; we'll pick up the disconnect from the reader thread */ }
			catch (ObjectDisposedException) { /* ditto */ }
			catch (InvalidOperationException) { /* ditto */ }
			catch (IOException) { /* ditto */ }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			// Closing the stream will cause any reads on the receiving thread to throw.
			// This will mark the connection as no longer connected and the thread will terminate cleanly.
			if (tcp != null)
				tcp.Close();

			base.Dispose(disposing);
		}
	}
}
