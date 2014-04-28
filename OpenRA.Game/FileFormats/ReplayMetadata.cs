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
using System.IO;
using System.Text;
using OpenRA.Network;

namespace OpenRA.FileFormats
{
	public class ReplayMetadata
	{
		// Must be an invalid replay 'client' value
		public const int MetaStartMarker = -1;
		public const int MetaEndMarker = -2;
		public const int MetaVersion = 0x00000001;

		public string FilePath { get; private set; }
		public DateTime EndTimestampUtc { get; private set; }
		public TimeSpan Duration { get { return EndTimestampUtc - StartTimestampUtc; } }
		public WinState Outcome { get; private set; }

		public readonly Lazy<Session> LobbyInfo;
		public readonly DateTime StartTimestampUtc;
		readonly string lobbyInfoData;

		ReplayMetadata()
		{
			Outcome = WinState.Undefined;
		}

		public ReplayMetadata(DateTime startGameTimestampUtc, Session lobbyInfo)
			: this()
		{
			if (startGameTimestampUtc.Kind == DateTimeKind.Unspecified)
				throw new ArgumentException("The 'Kind' property of the timestamp must be specified", "startGameTimestamp");

			StartTimestampUtc = startGameTimestampUtc.ToUniversalTime();

			lobbyInfoData = lobbyInfo.Serialize();
			LobbyInfo = Exts.Lazy(() => Session.Deserialize(this.lobbyInfoData));
		}

		public void FinalizeReplayMetadata(DateTime endGameTimestampUtc, WinState outcome)
		{
			if (endGameTimestampUtc.Kind == DateTimeKind.Unspecified)
				throw new ArgumentException("The 'Kind' property of the timestamp must be specified", "endGameTimestampUtc");
			EndTimestampUtc = endGameTimestampUtc.ToUniversalTime();

			Outcome = outcome;
		}

		ReplayMetadata(BinaryReader reader)
			: this()
		{
			// Read start marker
			if (reader.ReadInt32() != MetaStartMarker)
				throw new InvalidOperationException("Expected MetaStartMarker but found an invalid value.");

			// Read version
			var version = reader.ReadInt32();
			if (version > MetaVersion)
				throw new NotSupportedException("Metadata version {0} is not supported".F(version));

			// Read start game timestamp
			StartTimestampUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

			// Read end game timestamp
			EndTimestampUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

			// Read game outcome
			WinState outcome;
			if (Enum.TryParse(ReadUtf8String(reader), true, out outcome))
				Outcome = outcome;

			// Read lobby info
			lobbyInfoData = ReadUtf8String(reader);
			LobbyInfo = Exts.Lazy(() => Session.Deserialize(this.lobbyInfoData));
		}

		public void Write(BinaryWriter writer)
		{
			// Write start marker & version
			writer.Write(MetaStartMarker);
			writer.Write(MetaVersion);

			// Write data
			int dataLength = 0;
			{
				// Write start game timestamp
				writer.Write(StartTimestampUtc.Ticks);
				dataLength += sizeof(long);

				// Write end game timestamp
				writer.Write(EndTimestampUtc.Ticks);
				dataLength += sizeof(long);

				// Write game outcome
				dataLength += WriteUtf8String(writer, Outcome.ToString());

				// Write lobby info data
				dataLength += WriteUtf8String(writer, lobbyInfoData);
			}

			// Write total length & end marker
			writer.Write(dataLength);
			writer.Write(MetaEndMarker);
		}

		public static ReplayMetadata Read(string path, bool enableFallbackMethod = true)
		{
			Func<DateTime> timestampProvider = () =>
			{
				try
				{
					return File.GetCreationTimeUtc(path);
				}
				catch
				{
					return DateTime.MinValue;
				}
			};

			using (var fs = new FileStream(path, FileMode.Open))
			{
				var o = Read(fs, enableFallbackMethod, timestampProvider);
				if (o != null)
					o.FilePath = path;
				return o;
			}
		}

		static ReplayMetadata Read(FileStream fs, bool enableFallbackMethod, Func<DateTime> fallbackTimestampProvider)
		{
			using (var reader = new BinaryReader(fs))
			{
				// Disposing the BinaryReader will dispose the underlying stream
				// and we don't want that because ReplayConnection may use the
				// stream as well.
				//
				// Fixed in .NET 4.5.
				// See: http://msdn.microsoft.com/en-us/library/gg712804%28v=vs.110%29.aspx

				if (fs.CanSeek)
				{
					fs.Seek(-(4 + 4), SeekOrigin.End);
					var dataLength = reader.ReadInt32();
					if (reader.ReadInt32() == MetaEndMarker)
					{
						// go back end marker + length storage + data + version + start marker
						fs.Seek(-(4 + 4 + dataLength + 4 + 4), SeekOrigin.Current);
						try
						{
							return new ReplayMetadata(reader);
						}
						catch (InvalidOperationException ex)
						{
							Log.Write("debug", ex.ToString());
						}
						catch (NotSupportedException ex)
						{
							Log.Write("debug", ex.ToString());
						}
					}

					// Reset the stream position or the ReplayConnection will fail later
					fs.Seek(0, SeekOrigin.Begin);
				}

				if (enableFallbackMethod)
				{
					using (var conn = new ReplayConnection(fs))
					{
						var replay = new ReplayMetadata(fallbackTimestampProvider(), conn.LobbyInfo);
						if (conn.TickCount == 0)
							return null;
						var seconds = (int)Math.Ceiling((conn.TickCount * Game.NetTickScale) / 25f);
						replay.EndTimestampUtc = replay.StartTimestampUtc.AddSeconds(seconds);
						return replay;
					}
				}
			}

			return null;
		}

		static int WriteUtf8String(BinaryWriter writer, string text)
		{
			byte[] bytes;

			if (!string.IsNullOrEmpty(text))
				bytes = Encoding.UTF8.GetBytes(text);
			else
				bytes = new byte[0];

			writer.Write(bytes.Length);
			writer.Write(bytes);

			return 4 + bytes.Length;
		}

		static string ReadUtf8String(BinaryReader reader)
		{
			return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
		}

		public MapPreview MapPreview
		{
			get { return Game.modData.MapCache[LobbyInfo.Value.GlobalSettings.Map]; }
		}
	}
}
