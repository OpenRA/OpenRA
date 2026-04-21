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

using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Cnc.FileFormats
{
	// Data that is to be XORed against another set of data (aka Format40)
	public static class XORDeltaCompression
	{
		public static int DecodeInto(byte[] src, byte[] dest, int srcOffset)
		{
			var ctx = new FastByteReader(src, srcOffset);
			var destIndex = 0;

			while (true)
			{
				var i = ctx.ReadByte();
				if ((i & 0x80) == 0)
				{
					var count = i & 0x7F;
					if (count == 0)
					{
						// case 6
						count = ctx.ReadByte();
						var value = ctx.ReadByte();
						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] ^= value;
					}
					else
					{
						// case 5
						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] ^= ctx.ReadByte();
					}
				}
				else
				{
					var count = i & 0x7F;
					if (count == 0)
					{
						count = ctx.ReadWord();
						if (count == 0)
							return destIndex;

						if ((count & 0x8000) == 0)
						{
							// case 2
							destIndex += count & 0x7FFF;
						}
						else if ((count & 0x4000) == 0)
						{
							// case 3
							for (var end = destIndex + (count & 0x3FFF); destIndex < end; destIndex++)
								dest[destIndex] ^= ctx.ReadByte();
						}
						else
						{
							// case 4
							var value = ctx.ReadByte();
							for (var end = destIndex + (count & 0x3FFF); destIndex < end; destIndex++)
								dest[destIndex] ^= value;
						}
					}
					else
					{
						// case 1
						destIndex += count;
					}
				}
			}
		}
	}
}
