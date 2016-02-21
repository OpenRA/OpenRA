#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		public TextWriter Writer;
	}

	public static class Log
	{
		static readonly Dictionary<string, ChannelInfo> Channels = new Dictionary<string, ChannelInfo>();

		static IEnumerable<string> FilenamesForChannel(string channelName, string baseFilename)
		{
			var path = Platform.SupportDir + "Logs";
			Directory.CreateDirectory(path);

			for (var i = 0;; i++)
				yield return Path.Combine(path, i > 0 ? "{0}.{1}".F(baseFilename, i) : baseFilename);
		}

		public static ChannelInfo Channel(string channelName)
		{
			ChannelInfo info;
			lock (Channels)
				if (!Channels.TryGetValue(channelName, out info))
					throw new Exception("Tried logging to non-existent channel " + channelName);

			return info;
		}

		public static void AddChannel(string channelName, string baseFilename)
		{
			lock (Channels)
			{
				if (Channels.ContainsKey(channelName)) return;

				if (string.IsNullOrEmpty(baseFilename))
				{
					Channels.Add(channelName, new ChannelInfo());
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
								Writer = TextWriter.Synchronized(writer)
							});

						return;
					}
					catch (IOException) { }
			}
		}

		public static void Write(string channel, string value)
		{
			var writer = Channel(channel).Writer;
			if (writer == null)
				return;

			writer.WriteLine(value);
		}

		public static void Write(string channel, string format, params object[] args)
		{
			var writer = Channel(channel).Writer;
			if (writer == null)
				return;

			writer.WriteLine(format, args);
		}
	}
}
