using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace OpenRa.Game
{
	class Network
	{
		public const int Port = 6543;
		const int netSyncInterval = 40 * 5;

		UdpClient client = new UdpClient(Port);
		int nextSyncTime = 0;
		int currentFrame = 0;

		public int CurrentFrame { get { return currentFrame; } }
		public int RemainingNetSyncTime { get { return Math.Max(0, nextSyncTime - Environment.TickCount); } }

		Queue<Packet> incomingPackets = new Queue<Packet>();

		public Network()
		{
			client.EnableBroadcast = true;

			Thread receiveThread = new Thread(delegate()
			{
				for (; ; )
				{
					IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
					byte[] data = client.Receive(ref sender);

					Packet packet = Packet.FromReceivedData(sender, data);

					lock (this)
						if (currentFrame <= packet.Frame)
							incomingPackets.Enqueue(packet);
				}
			});

			receiveThread.IsBackground = true;
			receiveThread.Start();
		}

		public void Send(byte[] data)
		{
			IPEndPoint destination = new IPEndPoint(IPAddress.Broadcast, Port);
			using(MemoryStream ms = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(ms))
			{
				writer.Write(currentFrame);
				writer.Write(data);
				writer.Flush();

				byte[] toSend = ms.ToArray();

				client.Send(toSend, toSend.Length);
			}
		}

		public Queue<Packet> Tick()
		{
			Queue<Packet> toProcess = new Queue<Packet>();

			if (Environment.TickCount > nextSyncTime)
				lock (this)
				{
					while (incomingPackets.Count > 0 && incomingPackets.Peek().Frame <= currentFrame)
					{
						Packet p = incomingPackets.Dequeue();
						if (p.Frame == currentFrame)
							toProcess.Enqueue(p);
					}

					++currentFrame;
					nextSyncTime = Environment.TickCount + netSyncInterval;
				}

			return toProcess;
		}
	}
}
