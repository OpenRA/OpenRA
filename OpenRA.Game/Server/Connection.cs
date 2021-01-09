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
using System.Net.Sockets;

namespace OpenRA.Server
{
	public class Connection
	{
		public const int MaxOrderLength = 131072;
		public Socket Socket;
		public List<byte> Data = new List<byte>();
		public List<byte> SendBuffer = new List<byte>();
		public ReceiveState State = ReceiveState.Header;
		public int ExpectLength = 8;
		public int Frame = 0;
		public int MostRecentFrame = 0;
		public bool Validated;

		public long TimeSinceLastResponse { get { return Game.RunTime - lastReceivedTime; } }
		public bool TimeoutMessageShown = false;
		long lastReceivedTime = 0;

		/* client data */
		public int PlayerIndex;
		public string AuthToken;

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
				while (Data.Count >= ExpectLength)
				{
					var bytes = PopBytes(ExpectLength);
					switch (State)
					{
						case ReceiveState.Header:
							{
								ExpectLength = BitConverter.ToInt32(bytes, 0) - 4;
								Frame = BitConverter.ToInt32(bytes, 4);
								State = ReceiveState.Data;

								if (ExpectLength < 0 || ExpectLength > MaxOrderLength)
								{
									server.DropClient(this);
									Log.Write("server", "Dropping client {0} for excessive order length = {1}", PlayerIndex, ExpectLength);
									return;
								}

								break;
							}

						case ReceiveState.Data:
							{
								if (MostRecentFrame < Frame)
									MostRecentFrame = Frame;

								server.ReceiveOrders(this, Frame, bytes);
								ExpectLength = 8;
								State = ReceiveState.Header;

								break;
							}
					}
				}
		}

		public void SendData(byte[] data, Server server)
		{
			BufferData(data);
			Flush(server);
		}

		public void BufferData(byte[] data)
		{
			SendBuffer.AddRange(data);
		}

		public void Flush(Server server)
		{
			if (SendBuffer.Count == 0)
				return;

			var data = SendBuffer.ToArray();
			SendBuffer.Clear();

			try
			{
				InnerSend(data);
			}
			catch (Exception e)
			{
				server.DropClient(this);
				Log.Write("server", "Dropping client {0} because dispatching orders failed: {1}", PlayerIndex, e);
			}
		}

		private void InnerSend(byte[] data)
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
	}

	public enum ReceiveState { Header, Data }
}
