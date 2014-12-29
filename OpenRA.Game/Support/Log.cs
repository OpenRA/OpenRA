#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public StreamWriter Writer;
	}

	public static class Log
	{
		public enum LoggingChannel
		{
			Debug,
			Perf,
			Utility,
			Exception,
			Server,
			Sound,
			Traitreport,
			Sync,
			Lua,
			Graphics,
			GeoIP
		}

		public static readonly Dictionary<LoggingChannel, ChannelInfo> Channels = new Dictionary<LoggingChannel, ChannelInfo>();

		static IEnumerable<string> FilenamesForChannel(LoggingChannel channel)
		{
			var path = Platform.SupportDir + "Logs";
			var Logfile = channel + ".log";

			Directory.CreateDirectory(path);

			for(var i = 0; i <= 32; i++)
				yield return Path.Combine(path,	i > 0 ? "{0}-{1}".F(Logfile, i) : Logfile);
		}

		public static void AddChannel(LoggingChannel channel)
		{
			if (Channels.ContainsKey(channel))
				return;

			if (string.IsNullOrEmpty(channel + ".log"))
			{
				Channels.Add(channel, new ChannelInfo());
				return;
			}

			foreach (var filename in FilenamesForChannel(channel))
			{
				if (Channels.ContainsKey(channel))
					continue;

				if (filename == null)
					continue;

				/* This is needed when we have to run more than one client on the same Mashine! */
				try
				{
					var writer = File.CreateText(filename);
					if (writer == null)
						continue;

					writer.AutoFlush = true;
					Channels.Add(channel, new ChannelInfo() { Filename = filename, Writer = writer });

				}
				catch (IOException ex) { Console.WriteLine(ex.Message);	}
			}
		}

		public static void Write(LoggingChannel channel, string format, params object[] args)
		{
			ChannelInfo info;
			if (!Channels.TryGetValue(channel, out info))
				throw new Exception("Tried logging to non-existant channel " + channel);

			if (info.Writer == null)
				return;

			info.Writer.WriteLine(format, args);
		}
	}
}
