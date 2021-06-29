#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenRA.Server
{
	public class Connection : IDisposable
	{
		public const int MaxOrderLength = 131072;

		public readonly int PlayerIndex;
		public readonly string AuthToken;
		public readonly EndPoint EndPoint;

		public long TimeSinceLastResponse => Game.RunTime - lastReceivedTime;
		public int MostRecentFrame { get; private set; }

		public bool TimeoutMessageShown;
		public bool Validated;

		long lastReceivedTime = 0;

		readonly BlockingCollection<byte[]> sendQueue = new BlockingCollection<byte[]>();

		public Connection(Socket socket, int playerIndex, string authToken, Action<Connection, int, byte[]> onPacket, Action<Connection> onDisconnect)
		{
			PlayerIndex = playerIndex;
			AuthToken = authToken;
			EndPoint = socket.RemoteEndPoint;

			new Thread(SendReceiveLoop)
			{
				Name = $"Client communication ({EndPoint}",
				IsBackground = true
			}.Start((socket, onPacket, onDisconnect));
		}

		void SendReceiveLoop(object s)
		{
			var (socket, onPacket, onDisconnect) = (ValueTuple<Socket, Action<Connection, int, byte[]>, Action<Connection>>)s;
			socket.Blocking = false;
			socket.NoDelay = true;

			var receiveBuffer = new byte[1024];
			var readBuffer = new List<byte>();
			var state = ReceiveState.Header;
			var expectLength = 8;
			var frame = 0;

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

									if (expectLength < 0 || expectLength > MaxOrderLength)
									{
										Log.Write("server", $"Closing socket connection to {EndPoint} because of excessive order length: {expectLength}");
										return;
									}

									break;
								}

								case ReceiveState.Data:
								{
									if (MostRecentFrame < frame)
										MostRecentFrame = frame;

									onPacket(this, frame, bytes);
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
								Log.Write("server", "Non-blocking send of {0} bytes failed. Falling back to blocking send.", length - start);
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
				onDisconnect(this);
				socket.Dispose();
			}
		}

		public void SendData(byte[] data)
		{
			sendQueue.Add(data);
		}

		public void Dispose()
		{
			// Tell the sendReceiveThread that the socket should be closed
			sendQueue.CompleteAdding();
		}
	}

	public enum ReceiveState { Header, Data }
}
