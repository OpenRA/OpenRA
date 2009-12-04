using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace OpenRa.Game
{
	class NetworkOrderSource : IOrderSource
	{
		TcpClient socket;

		Dictionary<int, List<byte[]>> orderBuffers = new Dictionary<int, List<byte[]>>();
		Dictionary<int, bool> gotEverything = new Dictionary<int, bool>();

		public NetworkOrderSource(TcpClient socket)
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			var reader = new BinaryReader(socket.GetStream());

			new Thread(() =>
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
			}) { IsBackground = true }.Start();
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

		public List<Order> OrdersForFrame(int currentFrame)
		{
			var orderData = ExtractOrders(currentFrame);
			return orderData.SelectMany(a => a.ToOrderList()).ToList();
		}

		public void SendLocalOrders(int localFrame, List<Order> localOrders)
		{
			socket.GetStream().WriteFrameData(localOrders, localFrame);
		}

		public bool IsReadyForFrame(int frameNumber)
		{
			lock (orderBuffers)
				return gotEverything.ContainsKey(frameNumber);
		}
	}
}
