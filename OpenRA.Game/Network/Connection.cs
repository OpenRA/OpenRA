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
		void Send(int frame, IEnumerable<byte[]> orders);
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

		readonly ConcurrentBag<ReceivedPacket> receivedPackets = new ConcurrentBag<ReceivedPacket>();
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

		public virtual void Send(int frame, IEnumerable<byte[]> orders)
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
			receivedPackets.Add(packet);
		}

		public virtual void Receive(Action<int, byte[]> packetFn)
		{
			var packets = new List<ReceivedPacket>(receivedPackets.Count);

			while (receivedPackets.TryTake(out var received))
			{
				packets.Add(received);
			}

			foreach (var p in packets)
			{
				packetFn(p.FromClient, p.Data);
				Recorder?.Receive(p.FromClient, p.Data);
			}
		}

		public void StartRecording(Func<string> chooseFilename)
		{
			// If we have a previous recording then save/dispose it and start a new one.
			Recorder?.Dispose();
			Recorder = new ReplayRecorder(chooseFilename);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				Recorder?.Dispose();
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
		readonly ConcurrentQueue<byte[]> awaitingAckPackets = new ConcurrentQueue<byte[]>();
		volatile ConnectionState connectionState = ConnectionState.Connecting;
		volatile int clientId;
		bool disposed;
		string errorMessage;

		public override IPEndPoint EndPoint { get { return endpoint; } }

		public override string ErrorMessage { get { return errorMessage; } }

		public bool UseNewNetcode;

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

					if (UseNewNetcode && client == LocalClientId && len == 7 && buf[4] == (byte)OrderType.Ack)
					{
						Ack(buf);
					}
					else if (len == 0)
						throw new NotImplementedException();
					else
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

		void Ack(byte[] buf)
		{
			int frameReceived;
			short framesToAck;
			using (var reader = new BinaryReader(new MemoryStream(buf)))
			{
				frameReceived = reader.ReadInt32();
				reader.ReadByte();
				framesToAck = reader.ReadInt16();
			}

			var ms = new MemoryStream(4 + awaitingAckPackets.Take(framesToAck).Sum(i => i.Length));
			ms.WriteArray(BitConverter.GetBytes(frameReceived));

			for (var i = 0; i < framesToAck; i++)
			{
				byte[] queuedPacket = default;
				if (awaitingAckPackets.Count > 0 && !awaitingAckPackets.TryDequeue(out queuedPacket))
				{
					// The dequeuing failed because of concurrency, so we retry
					for (var c = 0; c < 5; c++)
					{
						if (awaitingAckPackets.TryDequeue(out queuedPacket))
						{
							break;
						}
					}
				}

				if (queuedPacket == default)
				{
					throw new InvalidOperationException("Received acks for unknown frames");
				}

				ms.WriteArray(queuedPacket);
			}

			AddPacket(new ReceivedPacket { FromClient = LocalClientId, Data = ms.GetBuffer() });
		}

		public override int LocalClientId { get { return clientId; } }
		public override ConnectionState ConnectionState { get { return connectionState; } }

		public override void SendSync(int frame, byte[] syncData)
		{
			using (var ms = new MemoryStream(4 + syncData.Length))
			{
				ms.WriteArray(BitConverter.GetBytes(frame));
				ms.WriteArray(syncData);

				queuedSyncPackets.Add(ms.GetBuffer());
			}
		}

		// Override send frame orders so we can hold them until ACK'ed
		public override void Send(int frame, IEnumerable<byte[]> orders)
		{
			var ordersLength = orders.Sum(i => i.Length);
			var ms = new MemoryStream(8 + ordersLength);

			// Always write data for old netcode
			if (orders.Count() > 0 || !UseNewNetcode)
			{
				// Write our packet to be acked
				byte[] ackArray;
				using (var ackMs = new MemoryStream(ordersLength))
				{
					foreach (var o in orders)
						ackMs.WriteArray(o);

					ackArray = ackMs.GetBuffer();
				}

				if (UseNewNetcode)
				{
					awaitingAckPackets.Enqueue(ackArray);
				}
				else
				{
					// Have to send data to self with frame information
					using (var dataMs = new MemoryStream(ackArray.Length + 4))
					{
						dataMs.WriteArray(BitConverter.GetBytes(frame));
						dataMs.WriteArray(ackArray);
						AddPacket(new ReceivedPacket { FromClient = LocalClientId, Data = dataMs.GetBuffer() });
					}
				}

				// Write our packet to send to the main memory stream
				ms.WriteArray(BitConverter.GetBytes(ackArray.Length + 4));
				ms.WriteArray(BitConverter.GetBytes(frame)); // TODO: Remove frames from send protocol
				ms.WriteArray(ackArray);
			}

			WriteQueuedSyncPackets(ms);
			SendNetwork(ms);
		}

		protected override void Send(byte[] packet)
		{
			base.Send(packet);

			var ms = new MemoryStream();
			WriteOrderPacket(ms, packet);
			WriteQueuedSyncPackets(ms);
			SendNetwork(ms);
		}

		void SendNetwork(MemoryStream ms)
		{
			try
			{
				ms.WriteTo(tcp.GetStream());
			}
			catch (SocketException) { /* drop this on the floor; we'll pick up the disconnect from the reader thread */ }
			catch (ObjectDisposedException) { /* ditto */ }
			catch (InvalidOperationException) { /* ditto */ }
			catch (IOException) { /* ditto */ }
		}

		void WriteOrderPacket(MemoryStream ms, byte[] packet)
		{
			ms.WriteArray(BitConverter.GetBytes(packet.Length));
			ms.WriteArray(packet);
		}

		void WriteQueuedSyncPackets(MemoryStream ms)
		{
			if (queuedSyncPackets.Any())
			{
				int listLengthNeeded = queuedSyncPackets.Sum(i => 4 + i.Length);
				if (ms.Capacity - ms.Length < listLengthNeeded)
				{
					ms.Capacity += listLengthNeeded - (ms.Capacity - (int)ms.Length);
				}
			}
			else
			{
				return;
			}

			foreach (var q in queuedSyncPackets)
			{
				ms.WriteArray(BitConverter.GetBytes(q.Length));
				ms.WriteArray(q);
				base.Send(q);
			}

			queuedSyncPackets.Clear();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			// Closing the stream will cause any reads on the receiving thread to throw.
			// This will mark the connection as no longer connected and the thread will terminate cleanly.
			tcp?.Close();

			base.Dispose(disposing);
		}
	}
}
