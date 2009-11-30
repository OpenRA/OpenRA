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

		public byte[] PopBytes(int n)
		{
			var result = data.GetRange(0, n);
			data.RemoveRange(0, n);
			return result.ToArray();
		}
	}

	enum ReceiveState { Header, Data };
}
