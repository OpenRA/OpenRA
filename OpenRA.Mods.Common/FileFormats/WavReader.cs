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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.FileFormats
{
	public static class WavReader
	{
		enum WaveType { Pcm = 0x1, ImaAdpcm = 0x11 }

		public static bool LoadSound(Stream s, out Func<Stream> result, out short channels, out int sampleBits, out int sampleRate)
		{
			result = null;
			channels = -1;
			sampleBits = -1;
			sampleRate = -1;

			var type = s.ReadASCII(4);
			if (type != "RIFF")
				return false;

			s.ReadInt32(); // File-size
			var format = s.ReadASCII(4);
			if (format != "WAVE")
				return false;

			WaveType audioType = 0;
			var dataOffset = -1L;
			var dataSize = -1;
			short blockAlign = -1;
			int uncompressedSize = -1;
			while (s.Position < s.Length)
			{
				if ((s.Position & 1) == 1)
					s.ReadByte(); // Alignment

				if (s.Position == s.Length)
					break; // Break if we aligned with end of stream

				var blockType = s.ReadASCII(4);
				switch (blockType)
				{
					case "fmt ":
						var fmtChunkSize = s.ReadInt32();
						var audioFormat = s.ReadInt16();
						audioType = (WaveType)audioFormat;

						if (!Enum.IsDefined(typeof(WaveType), audioType))
							throw new NotSupportedException("Compression type {0} is not supported.".F(audioFormat));

						channels = s.ReadInt16();
						sampleRate = s.ReadInt32();
						s.ReadInt32(); // Byte Rate
						blockAlign = s.ReadInt16();
						sampleBits = s.ReadInt16();

						s.ReadBytes(fmtChunkSize - 16);
						break;
					case "fact":
						var chunkSize = s.ReadInt32();
						uncompressedSize = s.ReadInt32();
						s.ReadBytes(chunkSize - 4);
						break;
					case "data":
						dataSize = s.ReadInt32();
						dataOffset = s.Position;
						s.Position += dataSize;
						break;
					case "LIST":
					case "cue ":
						var listCueChunkSize = s.ReadInt32();
						s.ReadBytes(listCueChunkSize);
						break;
					default:
						s.Position = s.Length; // Skip to end of stream
						break;
				}
			}

			if (audioType == WaveType.ImaAdpcm)
				sampleBits = 16;

			var chan = channels;
			result = () =>
			{
				var audioStream = SegmentStream.CreateWithoutOwningStream(s, dataOffset, dataSize);
				if (audioType == WaveType.ImaAdpcm)
					return new WavStream(audioStream, dataSize, blockAlign, chan, uncompressedSize);

				return audioStream; // Data is already PCM format.
			};

			return true;
		}

		public static float WaveLength(Stream s)
		{
			s.Position = 12;
			var fmt = s.ReadASCII(4);

			if (fmt != "fmt ")
				return 0;

			s.Position = 22;
			var channels = s.ReadInt16();
			var sampleRate = s.ReadInt32();

			s.Position = 34;
			var bitsPerSample = s.ReadInt16();
			var length = s.Length * 8;

			return length / (channels * sampleRate * bitsPerSample);
		}

		sealed class WavStream : ReadOnlyAdapterStream
		{
			readonly short channels;
			readonly int numBlocks;
			readonly int blockDataSize;
			readonly int outputSize;
			readonly int[] predictor;
			readonly int[] index;

			readonly byte[] interleaveBuffer;
			int outOffset;
			int currentBlock;

			public WavStream(Stream stream, int dataSize, short blockAlign, short channels, int uncompressedSize)
				: base(stream)
			{
				this.channels = channels;
				numBlocks = dataSize / blockAlign;
				blockDataSize = blockAlign - (channels * 4);
				outputSize = uncompressedSize * channels * 2;
				predictor = new int[channels];
				index = new int[channels];

				interleaveBuffer = new byte[channels * 16];
			}

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				// Decode each block of IMA ADPCM data
				// Each block starts with a initial state per-channel
				for (var c = 0; c < channels; c++)
				{
					predictor[c] = baseStream.ReadInt16();
					index[c] = baseStream.ReadUInt8();
					baseStream.ReadUInt8(); // Unknown/Reserved

					// Output first sample from input
					data.Enqueue((byte)predictor[c]);
					data.Enqueue((byte)(predictor[c] >> 8));
					outOffset += 2;

					if (outOffset >= outputSize)
						return true;
				}

				// Decode and output remaining data in this block
				var blockOffset = 0;
				while (blockOffset < blockDataSize)
				{
					for (var c = 0; c < channels; c++)
					{
						// Decode 4 bytes (to 16 bytes of output) per channel
						var chunk = baseStream.ReadBytes(4);
						var decoded = ImaAdpcmReader.LoadImaAdpcmSound(chunk, ref index[c], ref predictor[c]);

						// Interleave output, one sample per channel
						var interleaveChannelOffset = 2 * c;
						for (var i = 0; i < decoded.Length; i += 2)
						{
							var interleaveSampleOffset = interleaveChannelOffset + i;
							interleaveBuffer[interleaveSampleOffset] = decoded[i];
							interleaveBuffer[interleaveSampleOffset + 1] = decoded[i + 1];
							interleaveChannelOffset += 2 * (channels - 1);
						}

						blockOffset += 4;
					}

					var outputRemaining = outputSize - outOffset;
					var toCopy = Math.Min(outputRemaining, interleaveBuffer.Length);
					for (var i = 0; i < toCopy; i++)
						data.Enqueue(interleaveBuffer[i]);

					outOffset += 16 * channels;

					if (outOffset >= outputSize)
						return true;
				}

				return ++currentBlock >= numBlocks;
			}
		}
	}
}
