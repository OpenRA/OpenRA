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
using OpenRA.Mods.Cnc.FileFormats;

namespace OpenRA.Mods.Cnc.AudioLoaders
{
	public class AudLoader : ISoundLoader
	{
		bool IsAud(Stream s)
		{
			var start = s.Position;
			s.Position += 11;
			var readFormat = s.ReadByte();
			s.Position = start;

			return readFormat == (int)SoundFormat.ImaAdpcm || readFormat == (int)SoundFormat.WestwoodCompressed;
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				if (IsAud(stream))
				{
					sound = new AudFormat(stream);
					return true;
				}
			}
			catch
			{
				// Not a supported AUD
			}

			sound = null;
			return false;
		}
	}

	public sealed class AudFormat : ISoundFormat
	{
		public int Channels => channels;
		public int SampleBits => sampleBits;
		public int SampleRate => sampleRate;
		public float LengthInSeconds => lengthInSeconds;
		public Stream GetPCMInputStream() { return audStreamFactory(); }
		public void Dispose() { sourceStream.Dispose(); }

		readonly Stream sourceStream;
		readonly Func<Stream> audStreamFactory;
		readonly int channels;
		readonly int sampleBits;
		readonly int sampleRate;
		readonly float lengthInSeconds;

		public AudFormat(Stream stream)
		{
			sourceStream = stream;

			if (!AudReader.LoadSound(stream, out audStreamFactory, out sampleRate, out sampleBits, out channels, out lengthInSeconds))
				throw new InvalidDataException();
		}
	}
}
