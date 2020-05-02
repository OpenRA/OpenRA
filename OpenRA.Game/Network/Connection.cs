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
using System.Net;
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
		IPEndPoint EndPoint { get; }
		string ErrorMessage { get; }
		void Send(int frame, List<byte[]> orders);
		void SendImmediate(IEnumerable<byte[]> orders);
		void SendSync(int frame, byte[] syncData);
		void Receive(Action<int, byte[]> packetFn);
	}

	public class ConnectionTarget
	{
		readonly DnsEndPoint[] endpoints;

		public ConnectionTarget()
		{
			endpoints = new[] { new DnsEndPoint("invalid", 0) };
		}

		public ConnectionTarget(string host, int port)
		{
			endpoints = new[] { new DnsEndPoint(host, port) };
		}

		public ConnectionTarget(IEnumerable<DnsEndPoint> endpoints)
		{
			this.endpoints = endpoints.ToArray();
			if (this.endpoints.Length == 0)
			{
				throw new ArgumentException("ConnectionTarget must have at least one address.");
			}
		}

		public IEnumerable<IPEndPoint> GetConnectEndPoints()
		{
			return endpoints
				.SelectMany(e =>
				{
					try
					{
						return Dns.GetHostAddresses(e.Host)
							.Select(a => new IPEndPoint(a, e.Port));
					}
					catch (Exception)
					{
						return Enumerable.Empty<IPEndPoint>();
					}
				})
				.ToList();
		}

		public override string ToString()
		{
			return endpoints
				.Select(e => "{0}:{1}".F(e.Host, e.Port))
				.JoinWith("/");
		}
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

		public virtual int LocalClientId
		{
			get { return 1; }
		}

		public virtual ConnectionState ConnectionState
		{
			get { return ConnectionState.PreConnecting; }
		}

		public virtual IPEndPoint EndPoint
		{
			get { throw new NotSupportedException("An echo connection doesn't have an endpoint"); }
		}

		public virtual string ErrorMessage
		{
			get { return null; }
		}

		public virtual void Send(int frame, List<byte[]> orders)
		{
			var ms = new MemoryStream();
			ms.WriteArray(BitConverter.GetBytes(frame));
			foreach (var o in orders)
				ms.WriteArray(o);
			Send(ms.ToArray());
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
			Send(ms.GetBuffer());
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
		readonly ConnectionTarget target;
		TcpClient tcp;
		IPEndPoint endpoint;
		readonly List<byte[]> queuedSyncPackets = new List<byte[]>();
		volatile ConnectionState connectionState = ConnectionState.Connecting;
		volatile int clientId;
		bool disposed;
		string errorMessage;

		public override IPEndPoint EndPoint { get { return endpoint; } }

		public override string ErrorMessage { get { return errorMessage; } }

		public NetworkConnection(ConnectionTarget target)
		{
			this.target = target;
			new Thread(NetworkConnectionConnect)
			{
				Name = "{0} (connect to {1})".F(GetType().Name, target),
				IsBackground = true
			}.Start();
		}

		void NetworkConnectionConnect()
		{
			var queue = new BlockingCollection<TcpClient>();

			var atLeastOneEndpoint = false;
			foreach (var endpoint in target.GetConnectEndPoints())
			{
				atLeastOneEndpoint = true;
				new Thread(() =>
				{
					try
					{
						var client = new TcpClient(endpoint.AddressFamily) { NoDelay = true };
						client.Connect(endpoint.Address, endpoint.Port);

						try
						{
							queue.Add(client);
						}
						catch (InvalidOperationException)
						{
							// Another connection was faster, close this one.
							client.Close();
						}
					}
					catch (Exception ex)
					{
						errorMessage = "Failed to connect";
						Log.Write("client", "Failed to connect to {0}: {1}".F(endpoint, ex.Message));
					}
				})
				{
					Name = "{0} (connect to {1})".F(GetType().Name, endpoint),
					IsBackground = true
				}.Start();
			}

			if (!atLeastOneEndpoint)
			{
				errorMessage = "Failed to resolve address";
				connectionState = ConnectionState.NotConnected;
			}

			// Wait up to 5s for a successful connection. This should hopefully be enough because such high latency makes the game unplayable anyway.
			else if (queue.TryTake(out tcp, 5000))
			{
				// Copy endpoint here to have it even after getting disconnected.
				endpoint = (IPEndPoint)tcp.Client.RemoteEndPoint;

				new Thread(NetworkConnectionReceive)
				{
					Name = "{0} (receive from {1})".F(GetType().Name, tcp.Client.RemoteEndPoint),
					IsBackground = true
				}.Start();
			}
			else
			{
				connectionState = ConnectionState.NotConnected;
			}

			// Close all unneeded connections in the queue and make sure new ones are closed on the connect thread.
			queue.CompleteAdding();
			foreach (var client in queue)
				client.Close();
		}

		void NetworkConnectionReceive()
		{
			try
			{
				var reader = new BinaryReader(tcp.GetStream());
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
					if (len == 0)
						throw new NotImplementedException();
					AddPacket(new ReceivedPacket { FromClient = client, Data = buf });
				}
			}
			catch (Exception ex)
			{
				errorMessage = "Connection failed";
				Log.Write("client", "Connection to {0} failed: {1}".F(endpoint, ex.Message));
			}
			finally
			{
				connectionState = ConnectionState.NotConnected;
			}
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		public override void SendSync(int frame, byte[] syncData)
		{
			var ms = new MemoryStream(4 + syncData.Length);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(syncData);
			queuedSyncPackets.Add(ms.GetBuffer());
		}

		protected override void Send(byte[] packet)
		{
			base.Send(packet);

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
