#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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
		public int Channels { get { return mp3.ChannelCount; } }
		public int SampleBits { get { return 16; } }
		public int SampleRate { get { return mp3.Frequency; } }
		public float LengthInSeconds { get { return mp3.Length * 8f / (2f * Channels * SampleRate); } }
		public Stream GetPCMInputStream() { return new MP3Stream(Clone(this)); }
		public void Dispose() { mp3.Dispose(); }

		readonly MP3Stream mp3;
		readonly Stream stream;

		public Mp3Format(Stream stream)
		{
			mp3 = new MP3Stream(stream);
			this.stream = stream;
		}

		Stream Clone(Mp3Format cloneFrom)
		{
			return SegmentStream.CreateWithoutOwningStream(cloneFrom.stream, 0, (int)cloneFrom.stream.Length);
		}
	}
}
