#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenRA.Server
{
	public sealed class Connection : IDisposable
	{
		public const int MaxOrderLength = 131072;

		// Cap ping history at 15 seconds as a balance between expiring stale state and having enough data for decent statistics
		const int MaxPingSamples = 15;

		public readonly int PlayerIndex;
		public readonly string AuthToken;
		public readonly EndPoint EndPoint;
		public readonly Stopwatch ConnectionTimer = Stopwatch.StartNew();

		public long TimeSinceLastResponse => Game.RunTime - lastReceivedTime;

		public bool TimeoutMessageShown;
		public bool Validated;
		public int LastOrdersFrame;

		long lastReceivedTime = 0;

		readonly BlockingCollection<byte[]> sendQueue = new BlockingCollection<byte[]>();
		readonly Queue<int> pingHistory = new Queue<int>();

		public Connection(Server server, Socket socket, string authToken)
		{
			PlayerIndex = server.ChooseFreePlayerIndex();
			AuthToken = authToken;
			EndPoint = socket.RemoteEndPoint;

			new Thread(SendReceiveLoop)
			{
				Name = $"Client communication ({EndPoint}",
				IsBackground = true
			}.Start((server, socket));
		}

		static byte[] CreatePingFrame()
		{
			var ms = new MemoryStream(21);
			ms.WriteArray(BitConverter.GetBytes(13));
			ms.WriteArray(BitConverter.GetBytes(0));
			ms.WriteArray(BitConverter.GetBytes(0));
			ms.WriteByte((byte)OrderType.Ping);
			ms.WriteArray(BitConverter.GetBytes(Game.RunTime));
			return ms.GetBuffer();
		}

		void SendReceiveLoop(object s)
		{
			var (server, socket) = (ValueTuple<Server, Socket>)s;
			socket.Blocking = false;
			socket.NoDelay = true;

			var receiveBuffer = new byte[1024];
			var readBuffer = new List<byte>();
			var state = ReceiveState.Header;
			var expectLength = 8;
			var frame = 0;
			var lastPingSent = Stopwatch.StartNew();

			try
			{
				while (true)
				{
					// Wait up to 100ms for data to arrive before checking for data to send
					if (socket.Poll(100000, SelectMode.SelectRead))
					{
						var read = socket.Receive(receiveBuffer);
						if (read == 0)
						{
							// Empty packet signals that the client has been dropped
							return;
						}

						if (read > 0)
						{
							readBuffer.AddRange(receiveBuffer.Take(read));
							lastReceivedTime = Game.RunTime;
							TimeoutMessageShown = false;
						}

						while (readBuffer.Count >= expectLength)
						{
							var bytes = readBuffer.GetRange(0, expectLength).ToArray();
							readBuffer.RemoveRange(0, expectLength);

							switch (state)
							{
								case ReceiveState.Header:
								{
									expectLength = BitConverter.ToInt32(bytes, 0) - 4;
									frame = BitConverter.ToInt32(bytes, 4);
									state = ReceiveState.Data;

									if (expectLength < 0 || (server.Type != ServerType.Local && expectLength > MaxOrderLength))
									{
										Log.Write("server", $"Closing socket connection to {EndPoint} because of excessive order length: {expectLength}");
										return;
									}

									break;
								}

								case ReceiveState.Data:
								{
									// Ping packets are sent and processed internally within this thread to reduce
									// server-introduced latencies from polling loops
									if (expectLength == 10 && bytes[0] == (byte)OrderType.Ping)
									{
										if (pingHistory.Count == MaxPingSamples)
											pingHistory.Dequeue();

										pingHistory.Enqueue((int)(Game.RunTime - BitConverter.ToInt64(bytes, 1)));
										server.OnConnectionPing(this, pingHistory.ToArray(), bytes[9]);
									}
									else
										server.OnConnectionPacket(this, frame, bytes);

									expectLength = 8;
									state = ReceiveState.Header;

									break;
								}
							}
						}
					}

					// Client has been dropped by the server
					if (sendQueue.IsCompleted)
						return;

					// Regularly check player ping
					if (lastPingSent.ElapsedMilliseconds > 1000)
						if (TrySendData(CreatePingFrame()))
							lastPingSent.Restart();

					// Send all data immediately, we will block again on read
					while (sendQueue.TryTake(out var data, 0))
					{
						var start = 0;
						var length = data.Length;

						// Non-blocking sends are free to send only part of the data
						while (start < length)
						{
							var sent = socket.Send(data, start, length - start, SocketFlags.None, out var error);
							if (error == SocketError.WouldBlock)
							{
								Log.Write("server", $"Non-blocking send of {length - start} bytes failed. Falling back to blocking send.");
								socket.Blocking = true;
								sent = socket.Send(data, start, length - start, SocketFlags.None);
								socket.Blocking = false;
							}
							else if (error != SocketError.Success)
								throw new SocketException((int)error);

							start += sent;
						}
					}
				}
			}
			catch (SocketException e)
			{
				Log.Write("server", $"Closing socket connection to {EndPoint} because of socket error: {e}");
			}
			finally
			{
				server.OnConnectionDisconnect(this);
				socket.Dispose();
			}
		}

		public bool TrySendData(byte[] data)
		{
			if (sendQueue.IsAddingCompleted)
				return false;

			try
			{
				sendQueue.Add(data);
				return true;
			}
			catch (InvalidOperationException)
			{
				// Occurs if the collection is marked completed for adding by another thread.
				return false;
			}
		}

		public void Dispose()
		{
			// Tell the sendReceiveThread that the socket should be closed
			sendQueue.CompleteAdding();
		}
	}

	public enum ReceiveState { Header, Data }
}
