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
	public class AudLoader : ISoundLoader
	{
		bool IsAud(Stream s)
		{
			var start = s.Position;
			s.Position += 10;
			var readFlag = s.ReadByte();
			var readFormat = s.ReadByte();
			s.Position = start;

			if (!Enum.IsDefined(typeof(SoundFlags), readFlag))
				return false;

			return Enum.IsDefined(typeof(SoundFormat), readFormat);
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

	public class AudFormat : ISoundFormat
	{
		public int Channels { get { return 1; } }
		public int SampleBits { get { return 16; } }
		public int SampleRate { get { return sampleRate; } }
		public float LengthInSeconds { get { return AudReader.SoundLength(stream); } }
		public Stream GetPCMInputStream() { return new MemoryStream(rawData.Value); }

		int sampleRate;
		Lazy<byte[]> rawData;

		Stream stream;

		public AudFormat(Stream stream)
		{
			this.stream = stream;

			var position = stream.Position;
			rawData = Exts.Lazy(() =>
			{
				try
				{
					byte[] data;
					if (!AudReader.LoadSound(stream, out data, out sampleRate))
						throw new InvalidDataException();
					return data;
				}
				finally
				{
					stream.Position = position;
				}
			});
		}
	}
}
