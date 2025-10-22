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
using System.Runtime.InteropServices;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.FileFormats
{
	public static class WavReader
	{
		enum WaveType : short { Pcm = 0x1, MsAdpcm = 0x2, ImaAdpcm = 0x11 }

		public static bool LoadSound(Stream s, out Func<Stream> result, out Func<byte[]> data,
			out short channels, out int sampleBits, out int sampleRate, out float lengthInSeconds)
		{
			result = null;
			data = null;
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
			short blockAlign = -1;
			while (s.Position < s.Length)
			{
				if ((s.Position & 1) == 1)
					s.ReadUInt8(); // Alignment

				if (s.Position == s.Length)
					break; // Break if we aligned with end of stream

				var blockType = s.ReadASCII(4);
				var chunkSize = s.ReadUInt32();
				switch (blockType)
				{
					case "fmt ":
						var audioFormat = s.ReadInt16();
						audioType = (WaveType)audioFormat;

						if (!Enum.IsDefined(audioType))
							throw new NotSupportedException($"Compression type {audioFormat} is not supported.");

						channels = s.ReadInt16();
						sampleRate = s.ReadInt32();
						s.ReadInt32(); // Byte Rate
						blockAlign = s.ReadInt16();
						sampleBits = s.ReadInt16();
						lengthInSeconds = (float)(s.Length * 8) / (channels * sampleRate * sampleBits);
						s.Position += chunkSize - 16; // Ignoring any optional extra params
						break;
					case "data":
						if (s.Position + chunkSize > s.Length)
							chunkSize = (uint)(s.Length - s.Position); // Handle defective data chunk size by assuming it's the remainder of the file

						dataSize = (int)chunkSize;
						dataOffset = s.Position;
						s.Position += dataSize;
						break;
					case "fact": // This chunk is often wrong, we will recalculate sample count ourselves
					case "LIST":
					case "cue ":
					default:
						s.Position += chunkSize; // Ignoring chunks we don't want to/know how to handle
						break;
				}
			}

			// sampleBits refers to the output bitrate, which is always 16 for adpcm.
			if (audioType != WaveType.Pcm)
				sampleBits = 16;

			if (channels != 1 && channels != 2)
				throw new NotSupportedException($"Expected 1 or 2 channels only for WAV file, received: {channels}");

			var chan = channels;
			result = () =>
			{
				var audioStream = SegmentStream.CreateWithoutOwningStream(s, dataOffset, dataSize);
				if (audioType == WaveType.ImaAdpcm)
					return new WavStreamImaAdpcm(audioStream, dataSize, blockAlign, chan);
				if (audioType == WaveType.MsAdpcm)
					return new WavStreamMsAdpcm(audioStream, dataSize, blockAlign, chan);

				return audioStream; // Data is already PCM format.
			};

			data = () =>
			{
				var currentPosition = s.Position;
				byte[] data;
				switch (audioType)
				{
					case WaveType.ImaAdpcm:
						data = ImaAdpcmReader.ReadData(s, dataOffset, dataSize, blockAlign, chan);
						break;
					case WaveType.MsAdpcm:
						var msStream = new WavStreamMsAdpcm(s, dataSize, blockAlign, chan);
						data = msStream.ReadAllBytes();

						break;
					default:
						s.Position = dataOffset;

						// Data is already PCM format.
						data = s.ReadBytes(dataSize);
						break;
				}

				s.Position = currentPosition;

				return data;
			};

			return true;
		}

		sealed class WavStreamImaAdpcm : Stream
		{
			const int SamplesPerGroup = 8;
			const int BytesPerSample = 2;

			readonly short channels;
			readonly int numBlocks;
			readonly int inBlockDataSize;
			readonly int outGroupSize;

			// NOTE: decoding a block is sequentually dependent on predictor/index.
			readonly short[] predictor;
			readonly byte[] index;

			int currentBlock;
			int inBlockOffset;

			readonly byte[] ringBuf = new byte[8192];
			int head, tail, count;

			bool baseStreamEmpty;
			readonly Stream baseStream;

			public WavStreamImaAdpcm(Stream stream, int dataSize, short blockAlign, short channels)
			{
				ArgumentNullException.ThrowIfNull(stream);
				if (!stream.CanRead)
					throw new ArgumentException("stream must be readable.", nameof(stream));

				baseStream = stream;

				this.channels = channels;

				var inHeaderSize = 4 * channels;
				inBlockDataSize = blockAlign - inHeaderSize;

				numBlocks = dataSize / blockAlign;
				var numGroupsPerBlock = inBlockDataSize / channels / SamplesPerGroup * BytesPerSample;

				outGroupSize = SamplesPerGroup * BytesPerSample * channels;
				var outHead = channels * BytesPerSample;
				var outGroups = numGroupsPerBlock * outGroupSize;
				Length = numBlocks * (outHead + outGroups);

				predictor = new short[channels];
				index = new byte[channels];
			}

			/// <summary>
			/// Reads and decodes blocks from the base stream into the ring buffer.
			/// </summary>
			bool ReadBlocks()
			{
				// Check if we have enough space for a group. Ask for a flush if not.
				// If ring buffer size were too small this could loop forever, however
				// we know our buffer will always be large enough.
				var availableBytes = ringBuf.Length - count;
				if (availableBytes < outGroupSize + 2)
					return false;

				// PERF: The output is 16-bit PCM, so we can write bytes as if they were shorts for less CPU churn.
				var outShorts = MemoryMarshal.Cast<byte, short>(ringBuf.AsSpan());

				// PERF: Used for batch reading data from stream.
				Span<byte> header = stackalloc byte[4];
				Span<byte> group = stackalloc byte[4 * channels];

				// Fill the ring buffer instead decoding just one group.
				while (ringBuf.Length - count >= outGroupSize + 2 && currentBlock < numBlocks)
				{
					// If we are at the start of a block, read the header
					if (inBlockOffset == 0)
					{
						for (var c = 0; c < channels; c++)
						{
							baseStream.ReadExactly(header);

							// Load initial values.
							predictor[c] = (short)(header[0] | (header[1] << 8));
							index[c] = header[2];

							// header[3] is unknown/reserved

							// Write initial predictor samples interleaved
							var pos = tail / 2;
							outShorts[pos] = predictor[c];
							tail = (tail + 2) % ringBuf.Length;
							count += 2;
						}
					}

					// Writing into a single buffer to improve cache locality, making sure data is serial.
					for (var c = 0; c < channels; c++)
						baseStream.ReadExactly(group.Slice(c * 4, 4));

					// Decode the group sequentially.
					for (var c = 0; c < channels; c++)
					{
						ref var pred = ref predictor[c];
						ref var idx = ref index[c];

						var offset = c * 4;
						var b0 = group[offset + 0];
						var b1 = group[offset + 1];
						var b2 = group[offset + 2];
						var b3 = group[offset + 3];

						// Decode into temporary variables so they could be easily turned into registers.
						var s0 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b0 & 0x0F), ref idx, ref pred);
						var s1 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b0 >> 4), ref idx, ref pred);
						var s2 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b1 & 0x0F), ref idx, ref pred);
						var s3 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b1 >> 4), ref idx, ref pred);
						var s4 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b2 & 0x0F), ref idx, ref pred);
						var s5 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b2 >> 4), ref idx, ref pred);
						var s6 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b3 & 0x0F), ref idx, ref pred);
						var s7 = ImaAdpcmReader.DecodeImaAdpcmSample((byte)(b3 >> 4), ref idx, ref pred);

						// Write interleaved samples (one sample per channel).
						// Handle ring buffer wrap-around.
						var basePos = tail / 2 + c;
						outShorts[(basePos + 0 * channels) % outShorts.Length] = s0;
						outShorts[(basePos + 1 * channels) % outShorts.Length] = s1;
						outShorts[(basePos + 2 * channels) % outShorts.Length] = s2;
						outShorts[(basePos + 3 * channels) % outShorts.Length] = s3;
						outShorts[(basePos + 4 * channels) % outShorts.Length] = s4;
						outShorts[(basePos + 5 * channels) % outShorts.Length] = s5;
						outShorts[(basePos + 6 * channels) % outShorts.Length] = s6;
						outShorts[(basePos + 7 * channels) % outShorts.Length] = s7;
					}

					tail = (tail + outGroupSize) % ringBuf.Length;
					count += outGroupSize;

					// We don't decode the entire block at once.
					inBlockOffset += 4 * channels;
					if (inBlockOffset >= inBlockDataSize)
					{
						inBlockOffset = 0;
						currentBlock++;

						// The file is done.
						if (currentBlock >= numBlocks)
							return true;
					}
				}

				return false;
			}

			public override bool CanSeek => false;
			public override bool CanRead => true;
			public override bool CanWrite => false;

			public override long Length { get; }

			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
			public override void SetLength(long value) { throw new NotSupportedException(); }
			public override void WriteByte(byte value) { throw new NotSupportedException(); }
			public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
			public override void Write(ReadOnlySpan<byte> buffer) { throw new NotSupportedException(); }
			public override void Flush() { throw new NotSupportedException(); }

			public override int ReadByte()
			{
				while (true)
				{
					if (count > 0)
					{
						var b = ringBuf[head++];
						if (head >= ringBuf.Length)
							head = 0;

						count--;
						return b;
					}

					if (baseStreamEmpty)
						return -1;

					// Try to fill buffer
					baseStreamEmpty = ReadBlocks();
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return Read(buffer.AsSpan(offset, count));
			}

			public override int Read(Span<byte> buffer)
			{
				var copied = 0;

				while (copied < buffer.Length)
				{
					if (count == 0)
					{
						if (baseStreamEmpty)
							break;

						baseStreamEmpty = ReadBlocks();
						if (count == 0 && baseStreamEmpty)
							break;

						if (count == 0)
							continue;
					}

					// Copy contiguous chunk to avoid per-byte copies
					var contiguous = Math.Min(count, ringBuf.Length - head);
					var toCopy = Math.Min(contiguous, buffer.Length - copied);

					// memcpy-like copy
					new Span<byte>(ringBuf, head, toCopy).CopyTo(buffer[copied..]);

					head += toCopy;
					if (head >= ringBuf.Length)
						head -= ringBuf.Length;
					count -= toCopy;
					copied += toCopy;
				}

				return copied;
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					baseStream.Dispose();
				base.Dispose(disposing);
			}
		}

		// Format docs https://wiki.multimedia.cx/index.php/Microsoft_ADPCM
		public sealed class WavStreamMsAdpcm : ReadOnlyAdapterStream
		{
			static readonly int[] AdaptationTable =
			[
				230, 230, 230, 230, 307, 409, 512, 614,
				768, 614, 512, 409, 307, 230, 230, 230
			];

			static readonly int[] AdaptCoeff1 = [256, 512, 0, 192, 240, 460, 392];

			static readonly int[] AdaptCoeff2 = [0, -256, 0, 64, 0, -208, -232];

			readonly short channels;
			readonly int blockDataSize;
			readonly int numBlocks;
			readonly byte[] blockData;

			int currentBlock;

			public WavStreamMsAdpcm(Stream stream, int dataSize, short blockAlign, short channels)
				: base(stream)
			{
				this.channels = channels;
				blockDataSize = blockAlign - channels * 7;
				numBlocks = dataSize / blockAlign;

				blockData = new byte[blockDataSize];
			}

			protected override bool BufferData(Stream baseStream, Queue<byte> data)
			{
				Span<byte> bpred = stackalloc byte[channels];
				Span<short> chanIdelta = stackalloc short[channels];

				Span<short> s1 = stackalloc short[channels];
				Span<short> s2 = stackalloc short[channels];

				baseStream.ReadBytes(bpred);
				baseStream.ReadBytes(MemoryMarshal.Cast<short, byte>(chanIdelta));
				baseStream.ReadBytes(MemoryMarshal.Cast<short, byte>(s1));
				baseStream.ReadBytes(MemoryMarshal.Cast<short, byte>(s2));

				for (var c = 0; c < channels; c++)
					s2[c] = WriteSample(s2[c], data);

				for (var c = 0; c < channels; c++)
					WriteSample(s1[c], data);

				var channelNumber = channels > 1 ? 1 : 0;

				baseStream.ReadBytes(blockData);
				for (var blockindx = 0; blockindx < blockDataSize; blockindx++)
				{
					var bytecode = blockData[blockindx];

					// Decode the first nibble, this is always left channel
					WriteSample(DecodeNibble((short)((bytecode >> 4) & 0x0F), bpred[0], ref chanIdelta[0], ref s1[0], ref s2[0]), data);

					// Decode the second nibble, for stereo this will be the right channel
					WriteSample(
						DecodeNibble(
							(short)(bytecode & 0x0F),
							bpred[channelNumber],
							ref chanIdelta[channelNumber],
							ref s1[channelNumber],
							ref s2[channelNumber]),
						data);
				}

				return ++currentBlock >= numBlocks;
			}

			static short WriteSample(short t, Queue<byte> data)
			{
				data.Enqueue((byte)t);
				data.Enqueue((byte)(t >> 8));
				return t;
			}

			// This code contains elements from libsndfile
			static short DecodeNibble(short nibble, byte bpred, ref short idelta, ref short s1, ref short s2)
			{
				var predict = (s1 * AdaptCoeff1[bpred] + s2 * AdaptCoeff2[bpred]) >> 8;

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
