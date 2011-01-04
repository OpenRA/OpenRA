#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OpenRA.FileFormats
{
	public static class HttpUtil
	{
		public static byte[] DownloadData(string url, Action<int, int> f, int chunkSize)
		{
			var uri = new Uri(url);
			var ip = Dns.GetHostEntry(uri.DnsSafeHost).AddressList[0];

			using (var s = new TcpClient())
			{
				s.Connect(new IPEndPoint(ip, uri.Port));
				var ns = s.GetStream();
				var sw = new StreamWriter(ns);

				sw.Write("GET {0} HTTP/1.1\r\nHost:{1}\r\n\r\n", uri.PathAndQuery, uri.Host);
				sw.Flush();

				var br = new BinaryReader(ns);
				var contentLength = 0;
				var offset = 0;
				for (; ; )
				{
					var result = br.ReadLine();
					var kv = result.Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);

					if (result == "")
					{
						/* data follows the blank line */

						if (contentLength > 0)
						{
							if (f != null)
								f(offset, contentLength);

							var data = new byte[contentLength];
							while (offset < contentLength)
							{
								var thisChunk = Math.Min(contentLength - offset, chunkSize);
								br.Read(data, offset, thisChunk);
								offset += thisChunk;
								if (f != null)
									f(offset, contentLength);
							}
							s.Close();
							return data;
						}
						else
						{
							s.Close();
							return new byte[] { };
						}
					}
					else if (kv[0] == "Content-Length")
						contentLength = int.Parse(kv[1]);
				}
			}
		}

		public static byte[] DownloadData(string url, Action<int, int> f)
		{
			return DownloadData(url, f, 4096);
		}

		public static byte[] DownloadData(string url)
		{
			return DownloadData(url, null);
		}

		static string ReadLine(this BinaryReader br)
		{
			var sb = new StringBuilder();
			char c;
			while ((c = br.ReadChar()) != '\n')
				if (c != '\r' && c != '\n')
					sb.Append(c);

			return sb.ToString();
		}
	}
}
