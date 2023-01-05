#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

	struct AudChunk
	{
		public int CompressedSize;
		public int OutputSize;

		public static AudChunk Read(Stream s)
		{
			AudChunk c;
			c.CompressedSize = s.ReadUInt16();
			c.OutputSize = s.ReadUInt16();

			if (s.ReadUInt32() != 0xdeaf)
				throw new InvalidDataException("Chunk header is bogus");

			return c;
		}
	}

	public static class AudReader
	{
		public static bool LoadSound(Stream s, out Func<Stream> result, out int sampleRate, out int sampleBits, out int channels, out float lengthInSeconds)
		{
			result = null;
			var startPosition = s.Position;
			try
			{
				sampleRate = s.ReadUInt16();
				var dataSize = s.ReadInt32();
				var outputSize = s.ReadInt32();
				var audioFlags = (SoundFlags)s.ReadByte();
				sampleBits = (audioFlags & SoundFlags._16Bit) == 0 ? 8 : 16;
				channels = (audioFlags & SoundFlags.Stereo) == 0 ? 1 : 2;
				lengthInSeconds = (float)(outputSize * 8) / (channels * sampleBits * sampleRate);

				var readFormat = s.ReadByte();
				if (!Enum.IsDefined(typeof(SoundFormat), readFormat))
					return false;

				var offsetPosition = s.Position;
				var streamLength = s.Length;
				var segmentLength = (int)(streamLength - offsetPosition);

				result = () =>
				{
					var audioStream = SegmentStream.CreateWithoutOwningStream(s, offsetPosition, segmentLength);

					switch (readFormat)
					{
						case (int)SoundFormat.ImaAdpcm:
							return new ImaAdpcmAudStream(audioStream, outputSize, dataSize);

						case (int)SoundFormat.WestwoodCompressed:
							return new WestwoodCompressedAudStream(audioStream, outputSize, dataSize);

						default:
							throw new NotImplementedException();
					}
				};
			}
			finally
			{
				s.Position = startPosition;
			}

			return true;
		}

		sealed class ImaAdpcmAudStream : ReadOnlyAdapterStream
		{
			readonly int outputSize;
			int dataSize;

			int currentSample;
			int baseOffset;
			int index;

			public ImaAdpcmAudStream(Stream stream, int outputSize, int dataSize)
				: base(stream)
			{
				this.outputSize = outputSize;
				this.dataSize = dataSize;
			}

			public override long Length => outputSize;

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				if (dataSize <= 0)
					return true;

				var chunk = AudChunk.Read(baseStream);
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

		sealed class WestwoodCompressedAudStream : ReadOnlyAdapterStream
		{
			readonly int outputSize;
			int dataSize;

			public WestwoodCompressedAudStream(Stream stream, int outputSize, int dataSize)
				: base(stream)
			{
				this.outputSize = outputSize;
				this.dataSize = dataSize;
			}

			public override long Length => outputSize;

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				if (dataSize <= 0)
					return true;

				var chunk = AudChunk.Read(baseStream);

				var input = baseStream.ReadBytes(chunk.CompressedSize);
				var output = new byte[chunk.OutputSize];
				WestwoodCompressedReader.DecodeWestwoodCompressedSample(input, output);

				foreach (var b in output)
					data.Enqueue(b);

				dataSize -= 8 + chunk.CompressedSize;

				return dataSize <= 0;
			}
		}
	}
}
