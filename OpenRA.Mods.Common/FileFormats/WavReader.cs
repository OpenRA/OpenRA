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
using System.Collections.Generic;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.FileFormats
{
	public static class WavReader
	{
		enum WaveType { Pcm = 0x1, MsAdpcm = 0x2, ImaAdpcm = 0x11 }

		public static bool LoadSound(Stream s, out Func<Stream> result, out short channels, out int sampleBits, out int sampleRate, out float lengthInSeconds)
		{
			result = null;
			channels = -1;
			sampleBits = -1;
			sampleRate = -1;
			lengthInSeconds = -1;

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
			var uncompressedSize = -1;
			short blockAlign = -1;
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
							throw new NotSupportedException($"Compression type {audioFormat} is not supported.");

						channels = s.ReadInt16();
						sampleRate = s.ReadInt32();
						s.ReadInt32(); // Byte Rate
						blockAlign = s.ReadInt16();
						sampleBits = s.ReadInt16();
						lengthInSeconds = (float)(s.Length * 8) / (channels * sampleRate * sampleBits);
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

			// sampleBits refers to the output bitrate, which is always 16 for adpcm.
			if (audioType != WaveType.Pcm)
				sampleBits = 16;

			var chan = channels;
			result = () =>
			{
				var audioStream = SegmentStream.CreateWithoutOwningStream(s, dataOffset, dataSize);
				if (audioType == WaveType.ImaAdpcm)
					return new WavStreamImaAdpcm(audioStream, dataSize, blockAlign, chan, uncompressedSize);
				if (audioType == WaveType.MsAdpcm)
					return new WavStreamMsAdpcm(audioStream, dataSize, blockAlign, chan);

				return audioStream; // Data is already PCM format.
			};

			return true;
		}

		sealed class WavStreamImaAdpcm : ReadOnlyAdapterStream
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

			public WavStreamImaAdpcm(Stream stream, int dataSize, short blockAlign, short channels, int uncompressedSize)
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

		// Format docs https://wiki.multimedia.cx/index.php/Microsoft_ADPCM
		public sealed class WavStreamMsAdpcm : ReadOnlyAdapterStream
		{
			static readonly int[] AdaptationTable =
			{
				230, 230, 230, 230, 307, 409, 512, 614,
				768, 614, 512, 409, 307, 230, 230, 230
			};

			static readonly int[] AdaptCoeff1 = { 256, 512, 0, 192, 240, 460, 392 };

			static readonly int[] AdaptCoeff2 = { 0, -256, 0, 64, 0, -208, -232 };

			readonly short channels;
			readonly int blockDataSize;
			readonly int numBlocks;

			int currentBlock;

			public WavStreamMsAdpcm(Stream stream, int dataSize, short blockAlign, short channels)
				: base(stream)
			{
				this.channels = channels;
				blockDataSize = blockAlign - channels * 7;
				numBlocks = dataSize / blockAlign;
			}

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				var bpred = new byte[channels];
				var chanIdelta = new short[channels];

				var s1 = new short[channels];
				var s2 = new short[channels];

				for (var c = 0; c < channels; c++)
					bpred[c] = baseStream.ReadUInt8();

				for (var c = 0; c < channels; c++)
					chanIdelta[c] = baseStream.ReadInt16();

				for (var c = 0; c < channels; c++)
					s1[c] = baseStream.ReadInt16();

				for (var c = 0; c < channels; c++)
					s2[c] = WriteSample(baseStream.ReadInt16(), data);

				for (var c = 0; c < channels; c++)
					WriteSample(s1[c], data);

				var channelNumber = channels > 1 ? 1 : 0;

				for (var blockindx = 0; blockindx < blockDataSize; blockindx++)
				{
					var bytecode = baseStream.ReadUInt8();

					// Decode the first nibble, this is always left channel
					WriteSample(DecodeNibble((short)((bytecode >> 4) & 0x0F), bpred[0], ref chanIdelta[0], ref s1[0], ref s2[0]), data);

					// Decode the second nibble, for stereo this will be the right channel
					WriteSample(DecodeNibble((short)(bytecode & 0x0F), bpred[channelNumber], ref chanIdelta[channelNumber], ref s1[channelNumber], ref s2[channelNumber]), data);
				}

				return ++currentBlock >= numBlocks;
			}

			short WriteSample(short t, Queue<byte> data)
			{
				data.Enqueue((byte)t);
				data.Enqueue((byte)(t >> 8));
				return t;
			}

			// This code contains elements from libsndfile
			short DecodeNibble(short nibble, byte bpred, ref short idelta, ref short s1, ref short s2)
			{
				var predict = ((s1 * AdaptCoeff1[bpred]) + (s2 * AdaptCoeff2[bpred])) >> 8;

				var twosCompliment = (nibble & 0x8) > 0
					? nibble - 0x10
					: nibble;

				s2 = s1;
				s1 = (short)(twosCompliment * idelta + predict).Clamp(-32768, 32767);

				// Compute next Adaptive Scale Factor (ASF), saturating to lower bound of 16
				idelta = (short)((AdaptationTable[nibble] * idelta) >> 8);
				if (idelta < 16)
					idelta = 16;

				return s1;
			}
		}
	}
}
