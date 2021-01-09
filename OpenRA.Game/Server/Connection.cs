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
using OpenRA.Network;

namespace OpenRA.Server
{
	public class Connection
	{
		public const int MaxOrderFrameLength = 131072;
		public const int OrderHeaderLength = 8;
		public Socket Socket;
		byte[] data = new byte[MaxOrderFrameLength + OrderHeaderLength];
		int dataCount = 0;
		ReceiveState state = ReceiveState.Header;
		int expectLength = 8;
		int frameIndex = 0;
		public int MostRecentFrame = 0;
		public bool Validated;

		public long TimeSinceLastResponse { get { return Game.RunTime - lastReceivedTime; } }
		public bool TimeoutMessageShown = false;
		long lastReceivedTime = 0;

		/* client data */
		public int PlayerIndex;
		public string AuthToken;

		void RemoveProcessedFrames(int lastFrameLength)
		{
			if (dataCount > lastFrameLength)
			{
				var tailSize = dataCount - lastFrameLength;
				Array.Copy(data, lastFrameLength, data, 0, tailSize);
				dataCount = tailSize;
			}
			else
				dataCount = 0;
		}

		bool ReadDataInner(Server server)
		{
			while (true)
			{
				var size = data.Length - dataCount;
				if (size == 0)
					break;

				try
				{
					// Poll the socket first to see if there's anything there.
					// This avoids the exception with SocketErrorCode == `SocketError.WouldBlock` thrown
					// from `socket.Receive(rx)`.
					if (!Socket.Poll(0, SelectMode.SelectRead)) break;

					var len = Socket.Receive(data, dataCount, size, SocketFlags.None);
					if (len > 0)
						dataCount += len;
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

		int ParseNextFrames(Server server)
		{
			// PERF: process all frames received in full, i.e. cached Sync frames
			var count = 0;
			var frameStartOffset = expectLength;
			while (true)
			{
				var tailSize = dataCount - frameStartOffset;
				if (tailSize >= OrderHeaderLength)
				{
					var dataSize = BitConverter.ToInt32(data, frameStartOffset) - 4;
					var frameSize = OrderHeaderLength + dataSize;
					if (tailSize >= frameSize)
					{
						frameIndex = BitConverter.ToInt32(data, frameStartOffset + 4);

						// TODO: Replace data with Span in future
						var frame = new Frame(frameIndex,
							new ArraySegment<byte>(data,
								frameStartOffset + OrderHeaderLength,
								dataSize));
						server.DispatchFrame(this, frame);

						frameStartOffset += frameSize;
						expectLength += frameSize;
						count++;
					}
					else
						break;
				}
				else
					break;
			}

			return count;
		}

		public void ReadData(Server server)
		{
			if (ReadDataInner(server))
				while (dataCount >= expectLength)
				{
					switch (state)
					{
						case ReceiveState.Header:
							{
								expectLength = BitConverter.ToInt32(data, 0) - 4;
								frameIndex = BitConverter.ToInt32(data, 4);
								state = ReceiveState.Data;

								if (expectLength < 0 || expectLength > MaxOrderFrameLength)
								{
									server.DropClient(this);
									Log.Write("server", "Dropping client {0} for excessive order frame length = {1}", PlayerIndex, expectLength);
									return;
								}

								expectLength += OrderHeaderLength;

								break;
							}

						case ReceiveState.Data:
							{
								if (MostRecentFrame < frameIndex)
									MostRecentFrame = frameIndex;

								var dataSize = expectLength - OrderHeaderLength;

								// TODO: Replace data with Span in future
								var frame = new Frame(frameIndex,
									new ArraySegment<byte>(data,
										OrderHeaderLength,
										dataSize));
								server.DispatchFrame(this, frame);
								ParseNextFrames(server);

								RemoveProcessedFrames(expectLength);
								expectLength = OrderHeaderLength;
								state = ReceiveState.Header;

								break;
							}
					}
				}
		}
	}

	public enum ReceiveState { Header, Data }
}
