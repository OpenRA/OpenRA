#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using LiteNetLib;

namespace OpenRA.Server
{
	public class Connection : IDisposable
	{
		public const int MaxOrderLength = 131072;

		// Cap ping history at 15 seconds as a balance between expiring stale state and having enough data for decent statistics
		const int MaxPingSamples = 15;

		public readonly int PlayerIndex;
		private readonly NetPeer peer;
		public readonly string AuthToken;
		public readonly EndPoint EndPoint;
		public readonly Stopwatch ConnectionTimer = Stopwatch.StartNew();

		readonly Stopwatch lastPingSent = Stopwatch.StartNew();

		public long TimeSinceLastResponse => Game.RunTime - lastReceivedTime;

		public bool TimeoutMessageShown;
		public bool Validated;
		public int LastOrdersFrame;

		long lastReceivedTime = 0;

		readonly Queue<int> pingHistory = new Queue<int>();

		public Connection(Server server, NetPeer peer, string authToken)
		{
			PlayerIndex = server.ChooseFreePlayerIndex();
			this.peer = peer;
			AuthToken = authToken;
			EndPoint = peer.EndPoint;
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

		public int[] UpdatePing(int time)
		{
			if (pingHistory.Count == MaxPingSamples)
				pingHistory.Dequeue();

			pingHistory.Enqueue(time);

			return pingHistory.ToArray();
		}

		public void UpdateLastReceivedTime()
		{
			lastReceivedTime = Game.RunTime;
			TimeoutMessageShown = false;
		}

		public void SendData(byte[] data)
		{
			peer.Send(data, DeliveryMethod.ReliableOrdered);
		}

		public void SendPing()
		{
			if (lastPingSent.ElapsedMilliseconds > 1000)
			{
				SendData(CreatePingFrame());
				lastPingSent.Restart();
			}
		}

		public void Dispose()
		{
			// Tell the sendReceiveThread that the socket should be closed
			peer.Disconnect();
		}
	}
}
