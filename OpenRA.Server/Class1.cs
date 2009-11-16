using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace OpenRA.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			var left = new TcpListener(1234);
			left.Start();
			var right = new TcpListener(1235);
			right.Start();

			var lc = left.AcceptTcpClient();
			lc.NoDelay = true; 
			var l = lc.GetStream();

			var rc = right.AcceptTcpClient();
			rc.NoDelay = true;
			var r = rc.GetStream();

			var ll = new Thread(RW(l, r));
			var rr = new Thread(RW(r, l));
			ll.Start();
			rr.Start();

			ll.Join();
		}

		static ThreadStart RW(NetworkStream a, NetworkStream b)
		{
			return () =>
			{
				var buf = new byte[4096];
				while (true)
				{
					var len = a.Read(buf, 0, 4096);
					b.Write(buf, 0, len);
				}
			};
		}
	}
}
