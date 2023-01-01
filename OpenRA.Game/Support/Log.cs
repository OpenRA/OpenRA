#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;

namespace OpenRA
{
	public struct ChannelInfo
	{
		public string Filename;
		public bool IsTimestamped;
		public TextWriter Writer;
	}

	readonly struct ChannelData
	{
		public readonly string Channel;
		public readonly string Text;

		public ChannelData(string channel, string text)
		{
			Text = text;
			Channel = channel;
		}
	}

	public static class Log
	{
		const int CreateLogFileMaxRetryCount = 128;

		static readonly ConcurrentDictionary<string, ChannelInfo> Channels = new ConcurrentDictionary<string, ChannelInfo>();
		static readonly Channel<ChannelData> Channel;
		static readonly ChannelWriter<ChannelData> ChannelWriter;
		static readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

		static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
		static readonly Timer Timer;
		static readonly Thread Thread;

		static Log()
		{
			Channel = System.Threading.Channels.Channel.CreateUnbounded<ChannelData>();
			ChannelWriter = Channel.Writer;

			Thread = new Thread(DoWork)
			{
				Name = "OpenRA Logging Thread"
			};

			Thread.Start(CancellationToken.Token);

			Timer = new Timer(FlushToDisk, CancellationToken.Token, FlushInterval, Timeout.InfiniteTimeSpan);
		}

		static void FlushToDisk(object state)
		{
			FlushToDisk();

			var token = (CancellationToken)state;
			if (!token.IsCancellationRequested)
				Timer.Change(FlushInterval, Timeout.InfiniteTimeSpan);
		}

		static void FlushToDisk()
		{
			foreach (var (_, channel) in Channels)
				channel.Writer?.Flush();
		}

		static void DoWork(object obj)
		{
			var token = (CancellationToken)obj;
			var reader = Channel.Reader;

			while (!token.IsCancellationRequested)
			{
				while (reader.TryRead(out var item))
					WriteValue(item);

				Thread.Sleep(1);
			}

			while (reader.TryRead(out var item))
				WriteValue(item);

			FlushToDisk();
		}

		static void WriteValue(ChannelData item)
		{
			var channel = GetChannel(item.Channel);
			var writer = channel.Writer;
			if (writer == null)
				return;

			if (!channel.IsTimestamped)
				writer.WriteLine(item.Text);
			else
			{
				var timestamp = DateTime.Now.ToString(Game.Settings.Server.TimestampFormat);
				writer.WriteLine("[{0}] {1}", timestamp, item.Text);
			}
		}

		static IEnumerable<string> FilenamesForChannel(string baseFilename)
		{
			var path = Platform.SupportDir + "Logs";
			Directory.CreateDirectory(path);

			for (var i = 0; i < CreateLogFileMaxRetryCount; i++)
				yield return Path.Combine(path, i > 0 ? $"{baseFilename}.{i}" : baseFilename);

			throw new ApplicationException($"Error creating log file \"{baseFilename}\"");
		}

		static ChannelInfo GetChannel(string channelName)
		{
			if (!Channels.TryGetValue(channelName, out var info))
				throw new ArgumentException("Tried logging to non-existent channel " + channelName, nameof(channelName));

			return info;
		}

		public static void AddChannel(string channelName, string baseFilename, bool isTimestamped = false)
		{
			if (Channels.ContainsKey(channelName))
				return;

			if (string.IsNullOrEmpty(baseFilename))
			{
				Channels.TryAdd(channelName, default);
				return;
			}

			foreach (var filename in FilenamesForChannel(baseFilename))
			{
				try
				{
					var writer = File.CreateText(filename);
					writer.AutoFlush = false;

					Channels.TryAdd(channelName,
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
			ChannelWriter.TryWrite(new ChannelData(channelName, value));
		}

		public static void Write(string channelName, Exception e)
		{
			ChannelWriter.TryWrite(new ChannelData(channelName, $"{e.Message}{Environment.NewLine}{e.StackTrace}"));
		}

		public static void Write(string channelName, string format, params object[] args)
		{
			ChannelWriter.TryWrite(new ChannelData(channelName, format.F(args)));
		}

		public static void Dispose()
		{
			CancellationToken.Cancel();
			Timer.Dispose();
			Thread.Join();
		}
	}
}
