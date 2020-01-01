#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Common.AudioLoaders
{
	public class WavLoader : ISoundLoader
	{
		bool IsWave(Stream s)
		{
			var start = s.Position;
			var type = s.ReadASCII(4);
			s.Position += 4;
			var format = s.ReadASCII(4);
			s.Position = start;

			return type == "RIFF" && format == "WAVE";
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				if (IsWave(stream))
				{
					sound = new WavFormat(stream);
					return true;
				}
			}
			catch
			{
				// Not a (supported) WAV
			}

			sound = null;
			return false;
		}
	}

	public sealed class WavFormat : ISoundFormat
	{
		public int Channels { get { return channels; } }
		public int SampleBits { get { return sampleBits; } }
		public int SampleRate { get { return sampleRate; } }
		public float LengthInSeconds { get { return WavReader.WaveLength(sourceStream); } }
		public Stream GetPCMInputStream() { return wavStreamFactory(); }
		public void Dispose() { sourceStream.Dispose(); }

		readonly Stream sourceStream;
		readonly Func<Stream> wavStreamFactory;
		readonly short channels;
		readonly int sampleBits;
		readonly int sampleRate;

		public WavFormat(Stream stream)
		{
			sourceStream = stream;

			if (!WavReader.LoadSound(stream, out wavStreamFactory, out channels, out sampleBits, out sampleRate))
				throw new InvalidDataException();
		}
	}
}
