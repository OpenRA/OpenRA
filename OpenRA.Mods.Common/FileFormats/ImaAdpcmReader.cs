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
using System.Runtime.CompilerServices;

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
	}
}
