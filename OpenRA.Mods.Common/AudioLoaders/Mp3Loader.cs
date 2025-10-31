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
using System.IO;
using MP3Sharp;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.AudioLoaders
{
	public class Mp3Loader : ISoundLoader
	{
		static bool IsMp3(Stream s)
		{
			var start = s.Position;

			// First try: MP3 may have ID3 meta data in front.
			var idTag = s.ReadASCII(3);
			s.Position = start;

			if (idTag == "ID3")
				return true;

			// Second try: MP3 without metadata, starts with MPEG chunk.
			var frameSync = s.ReadUInt16();
			s.Position = start;

			if (frameSync == 0xfbff)
				return true;

			// Neither found, not an MP3!
			return false;
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				if (IsMp3(stream))
				{
					sound = new Mp3Format(stream);
					return true;
				}
			}
			catch
			{
				// Not a (supported) MP3
			}

			sound = null;
			return false;
		}
	}

	public sealed class Mp3Format : ISoundFormat
	{
		public int Channels => mp3.ChannelCount;
		public int SampleBits => 16;
		public int SampleRate => mp3.Frequency;
		public float LengthInSeconds { get; }
		public Stream GetPCMInputStream() { return new MP3Stream(Clone(this)); }
		public void Dispose() { mp3.Dispose(); }

		readonly MP3Stream mp3;
		readonly Stream stream;

		public Mp3Format(Stream stream)
		{
			var startPosition = stream.Position;
			try
			{
				mp3 = new MP3Stream(stream);
				this.stream = stream;

				// Make a first guess based on the file size and bitrate
				// This should be fine for constant bitrate files
				LengthInSeconds = mp3.Length * 8f / (2f * Channels * SampleRate);

				try
				{
					// Attempt to parse a more accurate length from the file metadata;
					LengthInSeconds = (float)new TagLib.Mpeg.AudioFile(new StreamAbstraction(stream)).Properties.Duration.TotalSeconds;
				}
				catch { }
			}
			finally
			{
				stream.Position = startPosition;
			}
		}

		static Stream Clone(Mp3Format cloneFrom)
		{
			return SegmentStream.CreateWithoutOwningStream(cloneFrom.stream, 0, (int)cloneFrom.stream.Length);
		}

		public class StreamAbstraction : TagLib.File.IFileAbstraction
		{
			public StreamAbstraction(Stream s)
			{
				ReadStream = s;
			}

			public Stream ReadStream { get; }

			public Stream WriteStream => throw new NotImplementedException();

			public void CloseStream(Stream stream) { }
			public string Name => "";
		}
	}
}
