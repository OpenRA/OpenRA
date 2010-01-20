using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System;

namespace OpenRa.Orders
{
	enum ConnectionState
	{
		NotConnected,
		Connecting,
		Connected,
	}

	class NetworkOrderSource : IOrderSource
	{
		TcpClient socket;

		Dictionary<int, List<byte[]>> orderBuffers = new Dictionary<int, List<byte[]>>();
		Dictionary<int, bool> gotEverything = new Dictionary<int, bool>();

		public ConnectionState State { get; private set; }

		public NetworkOrderSource(string host, int port)
		{
			State = ConnectionState.Connecting;

			socket = new TcpClient();
			socket.BeginConnect(host, port, OnConnected, null);
			socket.NoDelay = true;
		}

		void OnConnected(IAsyncResult r)
		{
			try
			{
				socket.EndConnect(r);
				State = ConnectionState.Connected;
				new Thread(() =>
				{
					var reader = new BinaryReader(socket.GetStream());

					try
					{
						for (; ; )
						{
							var len = reader.ReadInt32();
							var frame = reader.ReadInt32();
							var buf = reader.ReadBytes(len - 4);

							lock (orderBuffers)
							{
								if (len == 5 && buf[0] == 0xef)	/* got everything marker */
									gotEverything[frame] = true;
								else
								{
									/* accumulate this chunk */
									if (!orderBuffers.ContainsKey(frame))
										orderBuffers[frame] = new List<byte[]> { buf };
									else
										orderBuffers[frame].Add(buf);
								}
							}
						}
					}
					catch (IOException)
					{
						State = ConnectionState.NotConnected;
					}

				}) { IsBackground = true }.Start();
			}
			catch
			{
				State = ConnectionState.NotConnected;
			}
		}

		static List<byte[]> NoOrders = new List<byte[]>();
		List<byte[]> ExtractOrders(int frame)
		{
			lock (orderBuffers)
			{
				List<byte[]> result;
				if (!orderBuffers.TryGetValue(frame, out result))
					result = NoOrders;
				orderBuffers.Remove(frame);
				gotEverything.Remove(frame);
				return result;
			}
		}

		public List<byte[]> OrdersForFrame(int currentFrame)
		{
			return ExtractOrders(currentFrame).ToList();
		}

		public void SendLocalOrders(int localFrame, List<Order> localOrders)
		{
			if (socket.Connected)
				socket.GetStream().WriteFrameData(localOrders, localFrame);
		}

		public bool IsReadyForFrame(int frameNumber)
		{
			lock (orderBuffers)
				return gotEverything.ContainsKey(frameNumber);
		}
	}
}
