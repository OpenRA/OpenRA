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
#region Additional Copyright & License Information
/*
 * This file is based on the blast routines (version 1.1 by Mark Adler)
 * included in zlib/contrib
 */
#endregion

using System;
using System.IO;

namespace OpenRA.Mods.Common.FileFormats
{
	public static class Blast
	{
		public static readonly int MAXBITS = 13; // maximum code length
		public static readonly int MAXWIN = 4096; // maximum window size

		static readonly byte[] LitLen =
		{
			11, 124, 8, 7, 28, 7, 188, 13, 76, 4,
			10, 8, 12, 10, 12, 10, 8, 23, 8, 9,
			7, 6, 7, 8, 7, 6, 55, 8, 23, 24,
			12, 11, 7, 9, 11, 12, 6, 7, 22, 5,
			7, 24, 6, 11, 9, 6, 7, 22, 7, 11,
			38, 7, 9, 8, 25, 11, 8, 11, 9, 12,
			8, 12, 5, 38, 5, 38, 5, 11, 7, 5,
			6, 21, 6, 10, 53, 8, 7, 24, 10, 27,
			44, 253, 253, 253, 252, 252, 252, 13, 12, 45,
			12, 45, 12, 61, 12, 45, 44, 173
		};

		// bit lengths of length codes 0..15
		static readonly byte[] LenLen = { 2, 35, 36, 53, 38, 23 };

		// bit lengths of distance codes 0..63
		static readonly byte[] DistLen = { 2, 20, 53, 230, 247, 151, 248 };

		// base for length codes
		static readonly short[] LengthBase =
		{
			3, 2, 4, 5, 6, 7, 8, 9, 10, 12,
			16, 24, 40, 72, 136, 264
		};

		// extra bits for length codes
		static readonly byte[] Extra =
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 2,
			3, 4, 5, 6, 7, 8
		};

		static readonly Huffman LitCode = new Huffman(LitLen, 256);
		static readonly Huffman LenCode = new Huffman(LenLen, 16);
		static readonly Huffman DistCode = new Huffman(DistLen, 64);

		/// <summary>PKWare Compression Library stream.</summary>
		/// <param name="input">Compressed input stream.</param>
		/// <param name="output">Stream to write the decompressed output.</param>
		/// <param name="onProgress">Progress callback, invoked with (read bytes, written bytes).</param>
		public static void Decompress(Stream input, Stream output, Action<long, long> onProgress = null)
		{
			var br = new BitReader(input);

			// Are literals coded?
			var coded = br.ReadBits(8);

			if (coded < 0 || coded > 1)
				throw new NotImplementedException("Invalid data stream");
			var encodedLiterals = coded == 1;

			// log2(dictionary size) - 6
			var dict = br.ReadBits(8);
			if (dict < 4 || dict > 6)
				throw new InvalidDataException("Invalid dictionary size");

			// output state
			ushort next = 0; // index of next write location in out[]
			var first = true; // true to check distances (for first 4K)
			var outBuffer = new byte[MAXWIN]; // output buffer and sliding window

			var inputStart = input.Position;
			var outputStart = output.Position;

			// decode literals and length/distance pairs
			do
			{
				// length/distance pair
				if (br.ReadBits(1) == 1)
				{
					// Length
					var symbol = Decode(LenCode, br);
					var len = LengthBase[symbol] + br.ReadBits(Extra[symbol]);

					// Magic number for "done"
					if (len == 519)
					{
						for (var i = 0; i < next; i++)
							output.WriteByte(outBuffer[i]);

						onProgress?.Invoke(input.Position - inputStart, output.Position - outputStart);
						break;
					}

					// Distance
					symbol = len == 2 ? 2 : dict;
					var dist = Decode(DistCode, br) << symbol;
					dist += br.ReadBits(symbol);
					dist++;

					if (first && dist > next)
						throw new InvalidDataException("Attempt to jump before data");

					// copy length bytes from distance bytes back
					do
					{
						var dest = next;
						var source = dest - dist;

						var copy = MAXWIN;
						if (next < dist)
						{
							source += copy;
							copy = dist;
						}

						copy -= next;
						if (copy > len)
							copy = len;

						len -= copy;
						next += (ushort)copy;

						// copy with old-fashioned memcpy semantics
						// in case of overlapping ranges. this is NOT
						// the same as Array.Copy()
						while (copy-- > 0)
							outBuffer[dest++] = outBuffer[source++];

						// Flush window to outstream
						if (next == MAXWIN)
						{
							for (var i = 0; i < next; i++)
								output.WriteByte(outBuffer[i]);
							next = 0;
							first = false;

							onProgress?.Invoke(input.Position - inputStart, output.Position - outputStart);
						}
					}
					while (len != 0);
				}
				else
				{
					// literal value
					var symbol = encodedLiterals ? Decode(LitCode, br) : br.ReadBits(8);
					outBuffer[next++] = (byte)symbol;
					if (next == MAXWIN)
					{
						for (var i = 0; i < next; i++)
							output.WriteByte(outBuffer[i]);
						next = 0;
						first = false;

						onProgress?.Invoke(input.Position - inputStart, output.Position - outputStart);
					}
				}
			}
			while (true);
		}

		// Decode a code using Huffman table h.
		static int Decode(Huffman h, BitReader br)
		{
			var code = 0; // len bits being decoded
			var first = 0; // first code of length len
			var index = 0; // index of first code of length len in symbol table
			short next = 1;
			while (true)
			{
				code |= br.ReadBits(1) ^ 1; // invert code
				int count = h.Count[next++];
				if (code < first + count)
					return h.Symbol[index + (code - first)];

				index += count;
				first += count;
				first <<= 1;
				code <<= 1;
			}
		}
	}

	class BitReader
	{
		readonly Stream stream;
		byte bitBuffer = 0;
		int bitCount = 0;

		public BitReader(Stream stream)
		{
			this.stream = stream;
		}

		public int ReadBits(int count)
		{
			var ret = 0;
			var filled = 0;
			while (filled < count)
			{
				if (bitCount == 0)
				{
					bitBuffer = stream.ReadUInt8();
					bitCount = 8;
				}

				ret |= (bitBuffer & 1) << filled;
				bitBuffer >>= 1;
				bitCount--;
				filled++;
			}

			return ret;
		}
	}

	/*
	 * Given a list of repeated code lengths rep[0..n-1], where each byte is a
	 * count (high four bits + 1) and a code length (low four bits), generate the
	 * list of code lengths.  This compaction reduces the size of the object code.
	 * Then given the list of code lengths length[0..n-1] representing a canonical
	 * Huffman code for n symbols, construct the tables required to decode those
	 * codes.  Those tables are the number of codes of each length, and the symbols
	 * sorted by length, retaining their original order within each length.
	 */
	class Huffman
	{
		public short[] Count; // number of symbols of each length
		public short[] Symbol; // canonically ordered symbols

		public Huffman(byte[] rep, short symbolCount)
		{
			var length = new short[256]; // code lengths
			var s = 0; // current symbol

			// convert compact repeat counts into symbol bit length list
			foreach (var code in rep)
			{
				var num = (code >> 4) + 1; // Number of codes (top four bits plus 1)
				var len = (byte)(code & 15); // Code length (low four bits)
				do
					length[s++] = len;
				while (--num > 0);
			}

			var n = s;

			// count number of codes of each length
			Count = new short[Blast.MAXBITS + 1];
			for (var i = 0; i < n; i++)
				Count[length[i]]++;

			// no codes!
			if (Count[0] == n)
				return;

			// check for an over-subscribed or incomplete set of lengths
			var left = 1; // one possible code of zero length
			for (var len = 1; len <= Blast.MAXBITS; len++)
			{
				left <<= 1;	// one more bit, double codes left
				left -= Count[len];	// deduct count from possible codes
				if (left < 0)
					throw new InvalidDataException("over subscribed code set");
			}

			// generate offsets into symbol table for each length for sorting
			var offs = new short[Blast.MAXBITS + 1];
			for (var len = 1; len < Blast.MAXBITS; len++)
				offs[len + 1] = (short)(offs[len] + Count[len]);

			// put symbols in table sorted by length, by symbol order within each length
			Symbol = new short[symbolCount];
			for (short i = 0; i < n; i++)
				if (length[i] != 0)
					Symbol[offs[length[i]]++] = i;
		}
	}
}
