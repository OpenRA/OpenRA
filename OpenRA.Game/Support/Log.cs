#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA
{
	public struct ChannelInfo
	{
		public string Filename;
		public bool IsTimestamped;
		public TextWriter Writer;
	}

	public static class Log
	{
		static readonly Dictionary<string, ChannelInfo> Channels = new Dictionary<string, ChannelInfo>();

		static IEnumerable<string> FilenamesForChannel(string channelName, string baseFilename)
		{
			var path = Platform.SupportDir + "Logs";
			Directory.CreateDirectory(path);

			for (var i = 0; ; i++)
				yield return Path.Combine(path, i > 0 ? "{0}.{1}".F(baseFilename, i) : baseFilename);
		}

		public static ChannelInfo Channel(string channelName)
		{
			ChannelInfo info;
			lock (Channels)
				if (!Channels.TryGetValue(channelName, out info))
					throw new ArgumentException("Tried logging to non-existent channel " + channelName, "channelName");

			return info;
		}

		public static void AddChannel(string channelName, string baseFilename, bool isTimestamped = false)
		{
			lock (Channels)
			{
				if (Channels.ContainsKey(channelName)) return;

				if (string.IsNullOrEmpty(baseFilename))
				{
					Channels.Add(channelName, default(ChannelInfo));
					return;
				}

				foreach (var filename in FilenamesForChannel(channelName, baseFilename))
					try
					{
						var writer = File.CreateText(filename);
						writer.AutoFlush = true;

						Channels.Add(channelName,
							new ChannelInfo
							{
								Filename = filename,
								IsTimestamped = isTimestamped,
								Writer = TextWriter.Synchronized(writer)
							});

						return;
					}
					catch (IOException) { }
			}
		}

		public static void Write(string channelName, string value)
		{
			var channel = Channel(channelName);
			var writer = channel.Writer;
			if (writer == null)
				return;

			if (!channel.IsTimestamped)
				writer.WriteLine(value);
			else
			{
				var timestamp = DateTime.Now.ToString(Game.Settings.Server.TimestampFormat);
				writer.WriteLine("[{0}] {1}", timestamp, value);
			}
		}

		public static void Write(string channelName, string format, params object[] args)
		{
			var channel = Channel(channelName);
			if (channel.Writer == null)
				return;

			if (!channel.IsTimestamped)
				channel.Writer.WriteLine(format, args);
			else
			{
				var timestamp = DateTime.Now.ToString(Game.Settings.Server.TimestampFormat);
				channel.Writer.WriteLine("[{0}] {1}", timestamp, format.F(args));
			}
		}
	}
}
