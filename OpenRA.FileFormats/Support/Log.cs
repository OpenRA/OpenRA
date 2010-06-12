#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using System.Net;

namespace OpenRA
{
	public struct ChannelInfo
	{
		public bool Upload;
		public string Filename;
		public StreamWriter Writer;
		public bool Diff;
	}

	public static class Log
	{
		public static string LogPathPrefix = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
		static Dictionary<string, ChannelInfo> channels = new Dictionary<string,ChannelInfo>();

		static Log()
		{
			AddChannel("debug", "openra.log.txt", false, false);
		}

		public static void AddChannel(string channelName, string filename, bool upload, bool diff)
		{
			StreamWriter writer = File.CreateText(LogPathPrefix + filename);
			writer.AutoFlush = true;

			channels.Add(channelName, new ChannelInfo() { Upload = upload, Filename = filename, Writer = writer, Diff = diff });
		}

		public static void Write(string channel, string format, params object[] args)
		{
			ChannelInfo info;
			if (!channels.TryGetValue(channel, out info))
				throw new Exception("Tried logging to non-existant channel " + channel);

			info.Writer.WriteLine(format, args);
		}

		public static void Upload(int gameId)
		{
			foreach (var kvp in channels.Where(x => x.Value.Upload))
			{
				kvp.Value.Writer.Close();
				var logfile = File.OpenRead(Log.LogPathPrefix + kvp.Value.Filename);
				byte[] fileContents = logfile.ReadAllBytes();
				var ms = new MemoryStream();

				using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
					gzip.Write(fileContents, 0, fileContents.Length);

				ms.Position = 0;
				byte[] buffer = ms.ReadAllBytes();

				WebRequest request = WebRequest.Create("http://open-ra.org/logs/upload.php");
				request.ContentType = "application/x-gzip";
				request.ContentLength = buffer.Length;
				request.Method = "POST";
				request.Headers.Add("Game-ID", gameId.ToString());
				request.Headers.Add("Channel", kvp.Key);
			//	request.Headers.Add("Diff", kvp.Value.Diff ? "1" : "0");

				using (var requestStream = request.GetRequestStream())
					requestStream.Write(buffer, 0, buffer.Length);

				var response = (HttpWebResponse)request.GetResponse();
			}
		}
	}
}
