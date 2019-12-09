#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.FileFormats
{
	[Flags]
	enum SoundFlags
	{
		Stereo = 0x1,
		_16Bit = 0x2,
	}

	enum SoundFormat
	{
		WestwoodCompressed = 1,
		ImaAdpcm = 99,
	}

	public static class AudReader
	{
		public static byte[] LoadSound(byte[] raw, ref int index)
		{
			return ImaAdpcmReader.LoadImaAdpcmSound(raw, ref index);
		}

		public static float SoundLength(Stream s)
		{
			var sampleRate = s.ReadUInt16();
			/*var dataSize = */ s.ReadInt32();
			var outputSize = s.ReadInt32();
			var flags = (SoundFlags)s.ReadByte();

			var samples = outputSize;
			if ((flags & SoundFlags.Stereo) != 0)
				samples /= 2;

			if ((flags & SoundFlags._16Bit) != 0)
				samples /= 2;

			return (float)samples / sampleRate;
		}

		public static bool LoadSound(Stream s, out Func<Stream> result, out int sampleRate)
		{
			result = null;
			var startPosition = s.Position;
			try
			{
				sampleRate = s.ReadUInt16();
				var dataSize = s.ReadInt32();
				var outputSize = s.ReadInt32();

				var readFlag = s.ReadByte();
				if (!Enum.IsDefined(typeof(SoundFlags), readFlag))
					return false;

				var readFormat = s.ReadByte();
				if (!Enum.IsDefined(typeof(SoundFormat), readFormat))
					return false;

				var offsetPosition = s.Position;

				result = () =>
				{
					var audioStream = SegmentStream.CreateWithoutOwningStream(s, offsetPosition, (int)(s.Length - offsetPosition));
					return new AudStream(audioStream, outputSize, dataSize);
				};
			}
			finally
			{
				s.Position = startPosition;
			}

			return true;
		}

		sealed class AudStream : ReadOnlyAdapterStream
		{
			readonly int outputSize;
			int dataSize;

			int currentSample;
			int baseOffset;
			int index;

			public AudStream(Stream stream, int outputSize, int dataSize)
				: base(stream)
			{
				this.outputSize = outputSize;
				this.dataSize = dataSize;
			}

			public override long Length
			{
				get { return outputSize; }
			}

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				if (dataSize <= 0)
					return true;

				var chunk = ImaAdpcmChunk.Read(baseStream);
				for (var n = 0; n < chunk.CompressedSize; n++)
				{
					var b = baseStream.ReadUInt8();

					var t = ImaAdpcmReader.DecodeImaAdpcmSample(b, ref index, ref currentSample);
					data.Enqueue((byte)t);
					data.Enqueue((byte)(t >> 8));
					baseOffset += 2;

					if (baseOffset < outputSize)
					{
						/* possible that only half of the final byte is used! */
						t = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b >> 4), ref index, ref currentSample);
						data.Enqueue((byte)t);
						data.Enqueue((byte)(t >> 8));
						baseOffset += 2;
					}
				}

				dataSize -= 8 + chunk.CompressedSize;

				return dataSize <= 0;
			}
		}
	}
}
