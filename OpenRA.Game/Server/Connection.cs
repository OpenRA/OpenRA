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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace OpenRA.Server
{
	public class Connection
	{
		public const int MaxOrderLength = 131072;

		public readonly Socket Socket;
		public readonly List<byte> Data = new List<byte>();
		public readonly int PlayerIndex;
		public readonly string AuthToken;

		public long TimeSinceLastResponse => Game.RunTime - lastReceivedTime;
		public int MostRecentFrame { get; private set; }

		public bool TimeoutMessageShown;
		public bool Validated;

		ReceiveState state = ReceiveState.Header;
		int expectLength = 8;
		int frame = 0;
		long lastReceivedTime = 0;

		public Connection(Socket socket, int playerIndex, string authToken)
		{
			Socket = socket;
			PlayerIndex = playerIndex;
			AuthToken = authToken;
		}

		public byte[] PopBytes(int n)
		{
			var result = Data.GetRange(0, n);
			Data.RemoveRange(0, n);
			return result.ToArray();
		}

		bool ReadDataInner(Server server)
		{
			var rx = new byte[1024];
			var len = 0;

			while (true)
			{
				try
				{
					// Poll the socket first to see if there's anything there.
					// This avoids the exception with SocketErrorCode == `SocketError.WouldBlock` thrown
					// from `socket.Receive(rx)`.
					if (!Socket.Poll(0, SelectMode.SelectRead)) break;

					if ((len = Socket.Receive(rx)) > 0)
						Data.AddRange(rx.Take(len));
					else
					{
						if (len == 0)
							server.DropClient(this);
						break;
					}
				}
				catch (SocketException e)
				{
					// This should no longer be needed with the socket.Poll call above.
					if (e.SocketErrorCode == SocketError.WouldBlock) break;

					server.DropClient(this);
					Log.Write("server", "Dropping client {0} because reading the data failed: {1}", PlayerIndex, e);
					return false;
				}
			}

			lastReceivedTime = Game.RunTime;
			TimeoutMessageShown = false;

			return true;
		}

		public void ReadData(Server server)
		{
			if (ReadDataInner(server))
			{
				while (Data.Count >= expectLength)
				{
					var bytes = PopBytes(expectLength);
					switch (state)
					{
						case ReceiveState.Header:
							{
								expectLength = BitConverter.ToInt32(bytes, 0) - 4;
								frame = BitConverter.ToInt32(bytes, 4);
								state = ReceiveState.Data;

								if (expectLength < 0 || expectLength > MaxOrderLength)
								{
									server.DropClient(this);
									Log.Write("server", "Dropping client {0} for excessive order length = {1}", PlayerIndex, expectLength);
									return;
								}

								break;
							}

						case ReceiveState.Data:
							{
								if (MostRecentFrame < frame)
									MostRecentFrame = frame;

								server.DispatchOrders(this, frame, bytes);
								expectLength = 8;
								state = ReceiveState.Header;

								break;
							}
					}
				}
			}
		}

		public void SendData(byte[] data)
		{
			var start = 0;
			var length = data.Length;

			// Non-blocking sends are free to send only part of the data
			while (start < length)
			{
				var sent = Socket.Send(data, start, length - start, SocketFlags.None, out var error);
				if (error == SocketError.WouldBlock)
				{
					Log.Write("server", "Non-blocking send of {0} bytes failed. Falling back to blocking send.", length - start);
					Socket.Blocking = true;
					sent = Socket.Send(data, start, length - start, SocketFlags.None);
					Socket.Blocking = false;
				}
				else if (error != SocketError.Success)
					throw new SocketException((int)error);

				start += sent;
			}
		}

		public EndPoint EndPoint => Socket.RemoteEndPoint;
	}

	public enum ReceiveState { Header, Data }
}
