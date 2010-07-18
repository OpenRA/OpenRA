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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
		static string LogPathPrefix = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
		static Dictionary<string, ChannelInfo> channels = new Dictionary<string,ChannelInfo>();

		public static string LogPath
		{
			get { return LogPathPrefix; }
			set
			{
				LogPathPrefix = value;				
				if (!Directory.Exists(LogPathPrefix))
					Directory.CreateDirectory(LogPathPrefix);
			}
		}
		
		public static void AddChannel(string channelName, string filename, bool upload, bool diff)
		{
			if (channels.ContainsKey(channelName)) return;
			
			var i = 0;
			var f = filename;
			while (File.Exists(LogPathPrefix + filename))
				try 
				{
					StreamWriter writer = File.CreateText(LogPathPrefix + filename);
					writer.AutoFlush = true;
					channels.Add(channelName, new ChannelInfo() { Upload = upload, Filename = filename, Writer = writer, Diff = diff });
					return;
				}
				catch (IOException) { filename = f + ".{0}".F(++i); }
			
			//if no logs exist, just make it
			StreamWriter w = File.CreateText(LogPathPrefix + filename);
			w.AutoFlush = true;
			channels.Add(channelName, new ChannelInfo() { Upload = upload, Filename = filename, Writer = w, Diff = diff });
			
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

				//var response = (HttpWebResponse)request.GetResponse();
			}
		}
	}
}
