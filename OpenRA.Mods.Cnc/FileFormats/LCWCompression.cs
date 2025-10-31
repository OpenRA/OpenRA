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
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Cnc.FileFormats
{
	// Lempel - Castle - Welch algorithm (aka Format80)
	public static class LCWCompression
	{
		static void ReplicatePrevious(byte[] dest, int destIndex, int srcIndex, int count)
		{
			if (srcIndex > destIndex)
				throw new NotImplementedException($"srcIndex > destIndex {srcIndex} {destIndex}");

			for (var i = 0; i < count; i++)
			{
				if (destIndex - srcIndex == 1)
					dest[destIndex + i] = dest[destIndex - 1];
				else
					dest[destIndex + i] = dest[srcIndex + i];
			}
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
					else
					{
						// If count3 == 0x3F it's case 5, else case 3
						var count = count3 == 0x3F ? ctx.ReadWord() : count3 + 3;
						var srcIndex = reverse ? destIndex - ctx.ReadWord() : ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException($"srcIndex >= destIndex {srcIndex} {destIndex}");

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

		// Quick and dirty LCW encoder version 2
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
