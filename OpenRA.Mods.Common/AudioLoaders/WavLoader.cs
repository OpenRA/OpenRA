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

	public class WavFormat : ISoundFormat
	{
		public int Channels { get { return reader.Value.Channels; } }
		public int SampleBits { get { return reader.Value.BitsPerSample; } }
		public int SampleRate { get { return reader.Value.SampleRate; } }
		public float LengthInSeconds { get { return WavReader.WaveLength(stream); } }
		public Stream GetPCMInputStream() { return new MemoryStream(reader.Value.RawOutput); }

		Lazy<WavReader> reader;

		readonly Stream stream;

		public WavFormat(Stream stream)
		{
			this.stream = stream;

			var position = stream.Position;
			reader = Exts.Lazy(() =>
			{
				var wavReader = new WavReader();
				try
				{
					if (!wavReader.LoadSound(stream))
						throw new InvalidDataException();
				}
				finally
				{
					stream.Position = position;
				}

				return wavReader;
			});
		}
	}
}