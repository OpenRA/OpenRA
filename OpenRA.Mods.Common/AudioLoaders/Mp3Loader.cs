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
		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				sound = new Mp3Format(stream);
				return true;
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

		Stream Clone(Mp3Format cloneFrom)
		{
			return SegmentStream.CreateWithoutOwningStream(cloneFrom.stream, 0, (int)cloneFrom.stream.Length);
		}

		public class StreamAbstraction : TagLib.File.IFileAbstraction
		{
			readonly Stream s;
			public StreamAbstraction(Stream s)
			{
				this.s = s;
			}

			public Stream ReadStream => s;

			public Stream WriteStream => throw new NotImplementedException();

			public void CloseStream(Stream stream) { }
			public string Name => "";
		}
	}
}
