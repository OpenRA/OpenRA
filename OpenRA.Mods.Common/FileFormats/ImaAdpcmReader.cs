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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.FileFormats
{
	public static class ImaAdpcmReader
	{
		static readonly int[] IndexAdjust = [-1, -1, -1, -1, 2, 4, 6, 8];
		static readonly int[] StepTable =
		[
			7, 8, 9, 10, 11, 12, 13, 14, 16,
			17, 19, 21, 23, 25, 28, 31, 34, 37,
			41, 45, 50, 55, 60, 66, 73, 80, 88,
			97, 107, 118, 130, 143, 157, 173, 190, 209,
			230, 253, 279, 307, 337, 371, 408, 449, 494,
			544, 598, 658, 724, 796, 876, 963, 1060, 1166,
			1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749,
			3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
			7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289,
			16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
		];

		/// <summary>
		/// Decodes a single IMA ADPCM nibble to a PCM sample.
		/// </summary>
		/// <remarks>
		/// Branchless and only the output variables leave registers.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short DecodeImaAdpcmSample(byte nibble, ref byte idx, ref short pred)
		{
			var step = StepTable[idx];
			var diff = step >> 3;

			var mask = nibble & 7;
			diff += ((mask >> 2) & 1) * step;
			diff += ((mask >> 1) & 1) * (step >> 1);
			diff += (mask & 1) * (step >> 2);

			// branchless negation via bitmask
			var sign = (nibble & 8) != 0 ? -1 : 1;
			diff *= sign;

			var sample = pred + diff;

			// branchless clamping (fast saturating logic)
			if ((uint)(sample - short.MinValue) > ushort.MaxValue)
				sample = sample > 0 ? short.MaxValue : short.MinValue;

			pred = (short)sample;

			var newIdx = idx + IndexAdjust[mask];
			newIdx = newIdx < 0 ? 0 : newIdx > 88 ? 88 : newIdx;
			idx = (byte)newIdx;

			return pred;
		}

		public static void LoadImaAdpcmSound(ReadOnlySpan<byte> raw, ref byte index, Span<byte> output)
		{
			short currentSample = 0;
			LoadImaAdpcmSound(raw, ref index, ref currentSample, output);
		}

		public static void LoadImaAdpcmSound(ReadOnlySpan<byte> raw, ref byte index, ref short currentSample, Span<byte> output)
		{
			var dataSize = raw.Length;
			if (output.Length != raw.Length * 4)
				throw new ArgumentException($"{nameof(output)} must be 4 times the length of {nameof(raw)}.", nameof(output));

			var offset = 0;

			while (dataSize-- > 0)
			{
				var b = raw[offset / 4];

				var t = DecodeImaAdpcmSample(b, ref index, ref currentSample);
				output[offset++] = (byte)t;
				output[offset++] = (byte)(t >> 8);

				t = DecodeImaAdpcmSample((byte)(b >> 4), ref index, ref currentSample);
				output[offset++] = (byte)t;
				output[offset++] = (byte)(t >> 8);
			}
		}

		public static byte[] ReadData(Stream stream, long dataOffset, int dataSize, short blockAlign, short channels)
		{
			const int SamplesPerGroup = 8;

			ArgumentNullException.ThrowIfNull(stream);

			var sourceData = SegmentStream.GetReadableData(stream, dataOffset, dataSize);

			var numBlocks = dataSize / blockAlign;

			var predictorSize = 4 * channels;
			var blockDataSize = blockAlign - predictorSize;

			// We get two samples per nibble
			var samplesPerChannel = blockDataSize * 2 / channels;

			// 8 samples from a 4-byte group
			var groupCount = samplesPerChannel / SamplesPerGroup;

			var predOut = numBlocks * channels * 2;
			var groupOut = numBlocks * groupCount * channels * SamplesPerGroup * 2;
			var estimatedOutDataSize = predOut + groupOut;

			var outData = new byte[estimatedOutDataSize];

			// PERF: The output is 16-bit PCM, so we can write bytes as if they were shorts for less CPU churn.
			var outShorts = MemoryMarshal.Cast<byte, short>(outData.AsSpan());

			// NOTE: decoding a block is sequentually dependent on predictor/index.
			Span<short> predictor = stackalloc short[channels];
			Span<byte> index = stackalloc byte[channels];

			// PERF: Avoid bounds checks by using refs.
			ref var srcRef = ref MemoryMarshal.GetReference(sourceData);
			ref var outRef = ref MemoryMarshal.GetReference(outShorts);
			ref var predRef = ref MemoryMarshal.GetReference(predictor);
			ref var idxRef = ref MemoryMarshal.GetReference(index);

			// Global decoded sample counter
			var src = 0;
			var outSample = 0;

			for (var block = 0; block < numBlocks; block++)
			{
				// Initial states
				for (var c = 0; c < channels; c++)
				{
					var offset = src + c * 4;

					// Load initial values.
					var pred = (short)(Unsafe.Add(ref srcRef, offset) | (Unsafe.Add(ref srcRef, offset + 1) << 8));
					var idx = Unsafe.Add(ref srcRef, offset + 2);

					Unsafe.Add(ref predRef, c) = pred;
					Unsafe.Add(ref idxRef, c) = idx;
				}

				src += predictorSize;

				// Write initial predictor samples interleaved
				for (var c = 0; c < channels; c++)
					Unsafe.Add(ref outRef, outSample + c) = Unsafe.Add(ref predRef, c);

				outSample += channels;

				for (var iter = 0; iter < groupCount; iter++)
				{
					// Decode 8 samples sequentially per channel
					for (var c = 0; c < channels; c++)
					{
						ref var pred = ref Unsafe.Add(ref predRef, c);
						ref var idx = ref Unsafe.Add(ref idxRef, c);

						var b0 = Unsafe.Add(ref srcRef, src + 0);
						var b1 = Unsafe.Add(ref srcRef, src + 1);
						var b2 = Unsafe.Add(ref srcRef, src + 2);
						var b3 = Unsafe.Add(ref srcRef, src + 3);

						src += 4;

						// PERF: Decode into temporary variables so they could be easily inlined directly to output.
						var s0 = DecodeImaAdpcmSample((byte)(b0 & 0x0F), ref idx, ref pred);
						var s1 = DecodeImaAdpcmSample((byte)(b0 >> 4), ref idx, ref pred);
						var s2 = DecodeImaAdpcmSample((byte)(b1 & 0x0F), ref idx, ref pred);
						var s3 = DecodeImaAdpcmSample((byte)(b1 >> 4), ref idx, ref pred);
						var s4 = DecodeImaAdpcmSample((byte)(b2 & 0x0F), ref idx, ref pred);
						var s5 = DecodeImaAdpcmSample((byte)(b2 >> 4), ref idx, ref pred);
						var s6 = DecodeImaAdpcmSample((byte)(b3 & 0x0F), ref idx, ref pred);
						var s7 = DecodeImaAdpcmSample((byte)(b3 >> 4), ref idx, ref pred);

						// Write interleaved samples (one sample per channel)
						var basePos = outSample + c;
						Unsafe.Add(ref outRef, basePos + channels * 0) = s0;
						Unsafe.Add(ref outRef, basePos + channels * 1) = s1;
						Unsafe.Add(ref outRef, basePos + channels * 2) = s2;
						Unsafe.Add(ref outRef, basePos + channels * 3) = s3;
						Unsafe.Add(ref outRef, basePos + channels * 4) = s4;
						Unsafe.Add(ref outRef, basePos + channels * 5) = s5;
						Unsafe.Add(ref outRef, basePos + channels * 6) = s6;
						Unsafe.Add(ref outRef, basePos + channels * 7) = s7;
					}

					outSample += channels * 8;
				}
			}

			return outData;
		}
	}
}
