using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;

namespace OpenRA.Server
{
	static class Server
	{
		static void Main(string[] args)
		{
			var listener = new TcpListener(IPAddress.Any, 1234);
			var clients = new List<TcpClient>();

			listener.Start();

			for (; ; )
			{
				try
				{
					var conn = listener.AcceptTcpClient();
					conn.NoDelay = true;
					Console.WriteLine("Accepted connection from {0}", 
						conn.Client.RemoteEndPoint.ToString());

					new Thread(() =>
					{
						lock (clients)
							clients.Add(conn);

						var ns = conn.GetStream();

						try
						{
							for (; ; )
							{
								var frame = BitConverter.ToInt32(ns.Read(4), 0);
								var length = BitConverter.ToInt32(ns.Read(4), 0);
								var data = ns.Read(length);

								lock (clients)
									foreach (var c in clients)
										if (c != conn)
										{
											var otherStream = c.GetStream();
											otherStream.Write(BitConverter.GetBytes(frame));
											otherStream.Write(BitConverter.GetBytes(length));
											otherStream.Write(data);
										}
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("Client dropped: {0}", conn.Client.RemoteEndPoint.ToString());

							lock (clients)
								clients.Remove(conn);
						}
					}) { IsBackground = true }.Start();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}

		public static void Write(this Stream s, byte[] data) { s.Write(data, 0, data.Length); }
		public static byte[] Read(this Stream s, int len) { var data = new byte[len]; s.Read(data, 0, len); return data; }
	}
}
