using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace OpenRA.Server
{
	class Connection
	{
		public Socket socket;
		public List<byte> data = new List<byte>();
		public ReceiveState State = ReceiveState.Header;
		public int ExpectLength = 8;
		public int Frame = 0;

		/* client data */
		public int PlayerIndex;

		/* file server state */
		public int NextChunk = 0;
		public int NumChunks = 0;
		public int RemainingBytes = 0;
		public Stream Stream = null;

		public byte[] PopBytes(int n)
		{
			var result = data.GetRange(0, n);
			data.RemoveRange(0, n);
			return result.ToArray();
		}

		bool ReadDataInner()
		{
			var rx = new byte[1024];
			var len = 0;

			for (; ; )
			{
				try
				{
					if (0 < (len = socket.Receive(rx)))
						data.AddRange(rx.Take(len));
					else
						break;
				}
				catch (SocketException e)
				{
					if (e.SocketErrorCode == SocketError.WouldBlock) break;
					Server.DropClient(this, e); 
					return false; 
				}
			}

			return true;
		}

		public void ReadData()
		{
			if (ReadDataInner())
				while (data.Count >= ExpectLength)
				{
					var bytes = PopBytes(ExpectLength);
					switch (State)
					{
						case ReceiveState.Header:
							{
								ExpectLength = BitConverter.ToInt32(bytes, 0) - 4;
								Frame = BitConverter.ToInt32(bytes, 4);
								State = ReceiveState.Data;
							} break;

						case ReceiveState.Data:
							{
						//		if (bytes.Length > 0)
						//			Console.WriteLine("{0} bytes", bytes.Length);

								Server.DispatchOrders(this, Frame, bytes);
								ExpectLength = 8;
								State = ReceiveState.Header;

								Server.UpdateInFlightFrames(this);
							} break;
					}
				}
		}}

	enum ReceiveState { Header, Data };
}
