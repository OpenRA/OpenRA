#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.FileFormats
{
	class FastByteReader
	{
		readonly byte[] src;
		int offset;

		public FastByteReader(byte[] src, int offset = 0)
		{
			this.src = src;
			this.offset = offset;
		}

		public bool Done() { return offset >= src.Length; }
		public byte ReadByte() { return src[offset++]; }
		public int ReadWord()
		{
			var x = ReadByte();
			return x | (ReadByte() << 8);
		}

		public void CopyTo(byte[] dest, int offset, int count)
		{
			Array.Copy(src, this.offset, dest, offset, count);
			this.offset += count;
		}

		public int Remaining() { return src.Length - offset; }
	}

	public static class Format80
	{
		static void ReplicatePrevious(byte[] dest, int destIndex, int srcIndex, int count)
		{
			if (srcIndex > destIndex)
				throw new NotImplementedException("srcIndex > destIndex {0} {1}".F(srcIndex, destIndex));

			if (destIndex - srcIndex == 1)
			{
				for (var i = 0; i < count; i++)
					dest[destIndex + i] = dest[destIndex - 1];
			}
			else
			{
				for (var i = 0; i < count; i++)
					dest[destIndex + i] = dest[srcIndex + i];
			}
		}

		public static unsafe uint DecodeInto(byte* src, byte* dest) {
			byte* pdest = dest;
			byte* readp = src;
			byte* writep = dest;

			while (true) {
				byte code = *readp++;
				byte* copyp;
				int count;
				if ((~code & 0x80) != 0) {
					//bit 7 = 0
					//command 0 (0cccpppp p): copy
					count = (code >> 4) + 3;
					copyp = writep - (((code & 0xf) << 8) + *readp++);
					while (count-- != 0)
						*writep++ = *copyp++;
				}
				else {
					//bit 7 = 1
					count = code & 0x3f;
					if ((~code & 0x40) != 0) {
						//bit 6 = 0
						if (count == 0)
							//end of image
							break;
						//command 1 (10cccccc): copy
						while (count-- != 0)
							*writep++ = *readp++;
					}
					else {
						//bit 6 = 1
						if (count < 0x3e) {
							//command 2 (11cccccc p p): copy
							count += 3;
							copyp = &pdest[*(ushort*) readp];

							readp += 2;
							while (count-- != 0)
								*writep++ = *copyp++;
						}
						else if (count == 0x3e) {
							//command 3 (11111110 c c v): fill
							count = *(ushort*) readp;
							readp += 2;
							code = *readp++;
							while (count-- != 0)
								*writep++ = code;
						}
						else {
							//command 4 (copy 11111111 c c p p): copy
							count = *(ushort*) readp;
							readp += 2;
							copyp = &pdest[*(ushort*) readp];
							readp += 2;
							while (count-- != 0)
								*writep++ = *copyp++;
						}
					}
				}
			}

			return (uint) (dest - pdest);
		}

		public static int DecodeInto(byte[] src, byte[] dest, int srcOffset = 0, bool reverse = false)
		{
			var ctx = new FastByteReader(src, srcOffset);
			var destIndex = 0;
			while (true)
			{
				var i = ctx.ReadByte();
				if ((i & 0x80) == 0)
				{
					// case 2
					var secondByte = ctx.ReadByte();
					var count = ((i & 0x70) >> 4) + 3;
					var rpos = ((i & 0xf) << 8) + secondByte;

					if (destIndex + count > dest.Length)
						return destIndex;

					ReplicatePrevious(dest, destIndex, destIndex - rpos, count);
					destIndex += count;
				}
				else if ((i & 0x40) == 0)
				{
					// case 1
					var count = i & 0x3F;
					if (count == 0)
						return destIndex;

					ctx.CopyTo(dest, destIndex, count);
					destIndex += count;
				}
				else
				{
					var count3 = i & 0x3F;
					if (count3 == 0x3E)
					{
						// case 4
						var count = ctx.ReadWord();
						var color = ctx.ReadByte();

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = color;
					}
					else if (count3 == 0x3F)
					{
						// case 5
						var count = ctx.ReadWord();
						var srcIndex = reverse ? destIndex - ctx.ReadWord() : ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException("srcIndex >= destIndex {0} {1}".F(srcIndex, destIndex));

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
					else
					{
						// case 3
						var count = count3 + 3;
						var srcIndex = reverse ? destIndex - ctx.ReadWord() : ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException("srcIndex >= destIndex {0} {1}".F(srcIndex, destIndex));

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
				}
			}
		}

		static int CountSame(byte[] src, int offset, int maxCount)
		{
			maxCount = Math.Min(src.Length - offset, maxCount);
			if (maxCount <= 0)
				return 0;

			var first = src[offset++];
			var count = 1;

			while (count < maxCount && src[offset++] == first)
				count++;

			return count;
		}

		static void WriteCopyBlocks(byte[] src, int offset, int count, MemoryStream output)
		{
			while (count > 0)
			{
				var writeNow = Math.Min(count, 0x3F);
				output.WriteByte((byte)(0x80 | writeNow));
				output.Write(src, offset, writeNow);

				count -= writeNow;
				offset += writeNow;
			}
		}

		// Quick and dirty Format80 encoder version 2
		// Uses raw copy and RLE compression
		public static byte[] Encode(byte[] src)
		{
			using (var ms = new MemoryStream())
			{
				var offset = 0;
				var left = src.Length;
				var blockStart = 0;

				while (offset < left)
				{
					var repeatCount = CountSame(src, offset, 0xFFFF);
					if (repeatCount >= 4)
					{
						// Write what we haven't written up to now
						WriteCopyBlocks(src, blockStart, offset - blockStart, ms);

						// Command 4: Repeat byte n times
						ms.WriteByte(0xFE);

						// Low byte
						ms.WriteByte((byte)(repeatCount & 0xFF));

						// High byte
						ms.WriteByte((byte)(repeatCount >> 8));

						// Value to repeat
						ms.WriteByte(src[offset]);

						offset += repeatCount;
						blockStart = offset;
					}
					else
						offset++;
				}

				// Write what we haven't written up to now
				WriteCopyBlocks(src, blockStart, offset - blockStart, ms);

				// Write terminator
				ms.WriteByte(0x80);

				return ms.ToArray();
			}
		}
	}
}
