#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.FileFormats
{
	// A reimplementation of the Blast routines included in zlib
	public static class Blast
	{
		public static readonly int MAXBITS = 13; // maximum code length
		public static readonly int MAXWIN = 4096; // maximum window size
		
		/*
		 * Decode a code from the stream s using huffman table h.  Return the symbol or
		 * a negative value if there is an error.  If all of the lengths are zero, i.e.
		 * an empty code, or if the code is incomplete and an invalid code is received,
		 * then -9 is returned after reading MAXBITS bits.
		 *
		 * Format notes:
		 *
		 * - The codes as stored in the compressed data are bit-reversed relative to
		 *   a simple integer ordering of codes of the same lengths.  Hence below the
		 *   bits are pulled from the compressed data one at a time and used to
		 *   build the code value reversed from what is in the stream in order to
		 *   permit simple integer comparisons for decoding.
		 *
		 * - The first code for the shortest length is all ones.  Subsequent codes of
		 *   the same length are simply integer decrements of the previous code.  When
		 *   moving up a length, a one bit is appended to the code.  For a complete
		 *   code, the last code of the longest length will be all zeros.  To support
		 *   this ordering, the bits pulled during decoding are inverted to apply the
		 *   more "natural" ordering starting with all zeros and incrementing.
		 */
		private static int Decode(Huffman h, BitReader br)
		{
			int code = 0; // len bits being decoded
			int first = 0; // first code of length len
			int index = 0; // index of first code of length len in symbol table
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

		/*
		 * Decode PKWare Compression Library stream.
		 *
		 * Format notes:
		 *
		 * - First byte is 0 if literals are uncoded or 1 if they are coded.  Second
		 *   byte is 4, 5, or 6 for the number of extra bits in the distance code.
		 *   This is the base-2 logarithm of the dictionary size minus six.
		 *
		 * - Compressed data is a combination of literals and length/distance pairs
		 *   terminated by an end code.  Literals are either Huffman coded or
		 *   uncoded bytes.  A length/distance pair is a coded length followed by a
		 *   coded distance to represent a string that occurs earlier in the
		 *   uncompressed data that occurs again at the current location.
		 *
		 * - A bit preceding a literal or length/distance pair indicates which comes
		 *   next, 0 for literals, 1 for length/distance.
		 *
		 * - If literals are uncoded, then the next eight bits are the literal, in the
		 *   normal bit order in th stream, i.e. no bit-reversal is needed. Similarly,
		 *   no bit reversal is needed for either the length extra bits or the distance
		 *   extra bits.
		 *
		 * - Literal bytes are simply written to the output.  A length/distance pair is
		 *   an instruction to copy previously uncompressed bytes to the output.  The
		 *   copy is from distance bytes back in the output stream, copying for length
		 *   bytes.
		 *
		 * - Distances pointing before the beginning of the output data are not
		 *   permitted.
		 *
		 * - Overlapped copies, where the length is greater than the distance, are
		 *   allowed and common.  For example, a distance of one and a length of 518
		 *   simply copies the last byte 518 times.  A distance of four and a length of
		 *   twelve copies the last four bytes three times.  A simple forward copy
		 *   ignoring whether the length is greater than the distance or not implements
		 *   this correctly.
		 */
		
		static byte[] litlen = new byte[] {
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
		static byte[] lenlen = new byte[] { 2, 35, 36, 53, 38, 23 };

		// bit lengths of distance codes 0..63
		static byte[] distlen = new byte[] { 2, 20, 53, 230, 247, 151, 248 };

		// base for length codes
		static short[] lengthbase = new short[] {
			3, 2, 4, 5, 6, 7, 8, 9, 10, 12,
			16, 24, 40, 72, 136, 264
		};

		// extra bits for length codes
		static byte[] extra = new byte[] {
			0, 0, 0, 0, 0, 0, 0, 0, 1, 2,
			3, 4, 5, 6, 7, 8
		};
		
		static Huffman litcode = new Huffman(litlen, litlen.Length, 256);
		static Huffman lencode = new Huffman(lenlen, lenlen.Length, 16);
		static Huffman distcode = new Huffman(distlen, distlen.Length, 64);
				
		public static byte[] Decompress(byte[] src)
		{
			BitReader br = new BitReader(src);
			
			// Are literals coded?
			int coded = br.ReadBits(8);
			
			if (coded < 0 || coded > 1)
				throw new NotImplementedException("Invalid datastream");
			bool EncodedLiterals = (coded == 1);
			
			// log2(dictionary size) - 6
			int dict = br.ReadBits(8);
			if (dict < 4 || dict > 6)
				throw new InvalidDataException("Invalid dictionary size");	
			
			// output state
			ushort next = 0; // index of next write location in out[]
			bool first = true; // true to check distances (for first 4K)
			byte[] outBuffer = new byte[MAXWIN]; // output buffer and sliding window
			var ms = new MemoryStream();

			// decode literals and length/distance pairs
			do
			{
				// length/distance pair
				if (br.ReadBits(1) == 1)
				{
					// Length
					int symbol = Decode(lencode, br);
					int len = lengthbase[symbol] + br.ReadBits(extra[symbol]);
					if (len == 519) // Magic number for "done"
					{
						for (int i = 0; i < next; i++)
							ms.WriteByte(outBuffer[i]);
						break;
					}
					
					// Distance
					symbol = len == 2 ? 2 : dict;
					int dist = Decode(distcode, br) << symbol;
					dist += br.ReadBits(symbol);
					dist++;
					
					if (first && dist > next)
						throw new InvalidDataException("Attempt to jump before data");
					
					// copy length bytes from distance bytes back
					do
					{
						int dest = next;
						int source = dest - dist;
						
						int copy = MAXWIN;
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
						Array.Copy(outBuffer, source, outBuffer, dest, copy);
						
						// Flush window to outstream
						if (next == MAXWIN)
						{
							for (int i = 0; i < next; i++)
								ms.WriteByte(outBuffer[i]);
							next = 0;
							first = false;
						}
					} while (len != 0);
				}
				else // literal value
				{
					int symbol = EncodedLiterals ? Decode(litcode, br) : br.ReadBits(8);
					outBuffer[next++] = (byte)symbol;
					if (next == MAXWIN)
					{
						for (int i = 0; i < next; i++)
							ms.WriteByte(outBuffer[i]);
						next = 0;
						first = false;
					}
				}
			} while (true);
			
			return ms.ToArray();
		}
	}
	
	class BitReader
	{
		readonly byte[] src;
        int offset = 0;
		int bitBuffer = 0;
		int bitCount = 0;

		public BitReader(byte[] src)
		{
            this.src = src;
		}
		
		public int ReadBits(int count)
		{
			int ret = 0;
			int filled = 0;
			while (filled < count)
			{
				if (bitCount == 0)
				{
					bitBuffer = src[offset++];
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

		public Huffman(byte[] rep, int n, short SymbolCount)
		{
			short[] length = new short[256]; // code lengths
			int s = 0; // current symbol
			
			// convert compact repeat counts into symbol bit length list
			foreach (byte code in rep)
			{
				int num = (code >> 4) + 1; // Number of codes (top four bits plus 1)
				byte len = (byte)(code & 15); // Code length (low four bits)
				do
				{
					length[s++] = len;
				} while (--num > 0);
			}
			n = s;
			
			// count number of codes of each length
			Count = new short[Blast.MAXBITS + 1];
			for (int i = 0; i < n; i++)
				Count[length[i]]++;
			
			// no codes!
			if (Count[0] == n)
				return;
			
			// check for an over-subscribed or incomplete set of lengths
			int left = 1; // one possible code of zero length
			for (int len = 1; len <= Blast.MAXBITS; len++)
			{
				left <<= 1;
				// one more bit, double codes left
				left -= Count[len];
				// deduct count from possible codes
				if (left < 0)
					throw new InvalidDataException ("over subscribed code set");
			}
			
			// generate offsets into symbol table for each length for sorting
			short[] offs = new short[Blast.MAXBITS + 1];
			for (int len = 1; len < Blast.MAXBITS; len++)
				offs[len + 1] = (short)(offs[len] + Count[len]);
			
			// put symbols in table sorted by length, by symbol order within each length
			Symbol = new short[SymbolCount];
			for (short i = 0; i < n; i++)
				if (length[i] != 0)
					Symbol[offs[length[i]]++] = i;
		}
	}
}
