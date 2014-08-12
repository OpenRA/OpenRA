#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public sealed class ReplayConnection : IConnection
	{
		class Chunk
		{
			public int Frame;
			public List<Pair<int, byte[]>> Packets = new List<Pair<int, byte[]>>();
		}

		IConnection inner;
		Queue<Chunk> chunks = new Queue<Chunk>();
		List<byte[]> sync = new List<byte[]>();
		int ordersFrame;
		public bool ResumeAtEnd { get; private set; }
		bool useInner = false;
		OrderManager orderManager;

		public int LocalClientId { get { return inner == null ? 0 : inner.LocalClientId; } }
		public ConnectionState ConnectionState { get { return !useInner ? ConnectionState.Connected : inner.ConnectionState; } }
		public readonly int TickCount;
		public readonly bool IsValid;
		public readonly Session LobbyInfo;

		public ReplayConnection(string replayFilename)
			: this(replayFilename, null, null) { }

		public ReplayConnection(string replayFilename, OrderManager om, IConnection inner)
		{
			this.ResumeAtEnd = om != null;
			this.inner = inner;

			if (om != null)
			{
				orderManager = om;
				Game.IsSimulating = true;
			}

			// Parse replay data into a struct that can be fed to the game in chunks
			// to avoid issues with all immediate orders being resolved on the first tick.
			using (var rs = File.OpenRead(replayFilename))
			{
				var chunk = new Chunk();

				while (rs.Position < rs.Length)
				{
					var client = rs.ReadInt32();
					if (client == ReplayMetadata.MetaStartMarker)
						break;
					var packetLen = rs.ReadInt32();
					var packet = rs.ReadBytes(packetLen);
					var frame = BitConverter.ToInt32(packet, 0);
					chunk.Packets.Add(Pair.New(client, packet));

					if (packet.Length == 5 && packet[4] == 0xBF)
						continue; // disconnect
					else if (packet.Length >= 5 && packet[4] == 0x65)
						continue; // sync
					else if (frame == 0)
					{
						// Parse replay metadata from orders stream
						var orders = packet.ToOrderList(null);
						foreach (var o in orders)
						{
							if (o.OrderString == "StartGame")
								IsValid = true;
							else if (o.OrderString == "SyncInfo" && !IsValid)
								LobbyInfo = Session.Deserialize(o.TargetString);
						}
					}
					else
					{
						// Regular order - finalize the chunk
						chunk.Frame = frame;
						chunks.Enqueue(chunk);
						chunk = new Chunk();

						TickCount = Math.Max(TickCount, frame);
					}
				}
			}

			ordersFrame = LobbyInfo.GlobalSettings.OrderLatency;
		}

		// Do nothing: ignore locally generated orders
		public void Send(int frame, List<byte[]> orders)
		{
			if (useInner && inner != null)
				inner.Send(frame, orders);
		}

		public void SendImmediate(List<byte[]> orders)
		{
			if (useInner && inner != null)
				inner.SendImmediate(orders);
		}

		public void SendSync(int frame, byte[] syncData)
		{
			if (useInner && inner != null)
			{
				inner.SendSync(frame, syncData);
				return;
			}

			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frame));
			ms.Write(syncData);
			sync.Add(ms.ToArray());

			// Store the current frame so Receive() can return the next chunk of orders.
			ordersFrame = frame + LobbyInfo.GlobalSettings.OrderLatency;
		}

		public void Receive(Action<int, byte[]> packetFn)
		{
			while (sync.Count != 0)
			{
				packetFn(LocalClientId, sync[0]);
				sync.RemoveAt(0);
			}

			while (chunks.Count != 0 && chunks.Peek().Frame <= ordersFrame)
				foreach (var o in chunks.Dequeue().Packets)
					packetFn(o.First, o.Second);

			if (inner != null)
				inner.Receive(packetFn);

			if (chunks.Count == 0 && ResumeAtEnd && !useInner && inner != null)
			{
				useInner = true;
				orderManager.world.IssueOrder(new Order("PauseGame", null, false) { TargetString = "UnPause" });
				Game.IsSimulating = false;
			}
		}

		public void Dispose()
		{
			if (inner != null)
				inner.Dispose();
		}
	}
}
