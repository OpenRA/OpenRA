#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
		int offset = 0;

		public FastByteReader(byte[] src)
		{
			this.src = src;
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

		public static int DecodeInto(byte[] src, byte[] dest)
		{
			var ctx = new FastByteReader(src);
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
						var srcIndex = ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException("srcIndex >= destIndex {0} {1}".F(srcIndex, destIndex));

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
					else
					{
						// case 3
						var count = count3 + 3;
						var srcIndex = ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException("srcIndex >= destIndex {0} {1}".F(srcIndex, destIndex));

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
				}
			}
		}

		public static byte[] Encode(byte[] src)
		{
			/* quick & dirty format80 encoder -- only uses raw copy operator, terminated with a zero-run. */
			/* this does not produce good compression, but it's valid format80 */

			var ctx = new FastByteReader(src);
			var ms = new MemoryStream();

			do
			{
				var len = Math.Min(ctx.Remaining(), 0x3F);
				ms.WriteByte((byte)(0x80 | len));
				while (len-- > 0)
					ms.WriteByte(ctx.ReadByte());
			}
			while (!ctx.Done());

			ms.WriteByte(0x80);	// terminator -- 0-length run.

			return ms.ToArray();
		}
	}
}
