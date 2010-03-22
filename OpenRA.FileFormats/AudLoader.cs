#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.FileFormats
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

	struct Chunk
	{
		public int CompressedSize;
		public int OutputSize;

		public static Chunk Read(BinaryReader r)
		{
			Chunk c;
			c.CompressedSize = r.ReadUInt16();
			c.OutputSize = r.ReadUInt16();
			if (0xdeaf != r.ReadUInt32())
				throw new InvalidDataException("Chunk header is bogus");
			return c;
		}
	}

	public static class AudLoader
	{
		static int[] IndexAdjust = { -1, -1, -1, -1, 2, 4, 6, 8 };
		static int[] StepTable = {
									7,     8,     9,     10,    11,    12,     13,    14,    16,
									17,    19,    21,    23,    25,    28,     31,    34,    37,
									41,    45,    50,    55,    60,    66,     73,    80,    88,
									97,    107,   118,   130,   143,   157,    173,   190,   209,
									230,   253,   279,   307,   337,   371,    408,   449,   494,
									544,   598,   658,   724,   796,   876,    963,   1060,  1166,
									1282,  1411,  1552,  1707,  1878,  2066,   2272,  2499,  2749,
									3024,  3327,  3660,  4026,  4428,  4871,   5358,  5894,  6484,
									7132,  7845,  8630,  9493,  10442, 11487,  12635, 13899, 15289,
									16818, 18500, 20350, 22385, 24623, 27086,  29794, 32767 };

		static short DecodeSample(byte b, ref int index, ref int current)
		{
			var sb = (b & 8) != 0;
			b &= 7;

			var delta = (StepTable[index] * b) / 4 + StepTable[index] / 8;
			if (sb) delta = -delta;

			current += delta;
			if (current > short.MaxValue) current = short.MaxValue;
			if (current < short.MinValue) current = short.MinValue;

			index += IndexAdjust[b];
			if (index < 0) index = 0;
			if (index > 88) index = 88;

			return (short)current;
		}

		public static byte[] LoadSound(Stream s)
		{
			var br = new BinaryReader(s);
			/*var sampleRate =*/ br.ReadUInt16();
			var dataSize = br.ReadInt32();
			var outputSize = br.ReadInt32();
			/*var flags = (SoundFlags)*/ br.ReadByte();
			/*var format = (SoundFormat)*/ br.ReadByte();

			var output = new byte[outputSize];
			var offset = 0;
			var index = 0;
			var currentSample = 0;

			while (dataSize > 0)
			{
				var chunk = Chunk.Read(br);
				for (int n = 0; n < chunk.CompressedSize; n++)
				{
					var b = br.ReadByte();

					var t = DecodeSample(b, ref index, ref currentSample);
					output[offset++] = (byte)t;
					output[offset++] = (byte)(t >> 8);
		
					t = DecodeSample((byte)(b >> 4), ref index, ref currentSample);
					output[offset++] = (byte)t;
					output[offset++] = (byte)(t >> 8);
				}

				dataSize -= 8 + chunk.CompressedSize;
			}

			return output;
		}
	}
}
