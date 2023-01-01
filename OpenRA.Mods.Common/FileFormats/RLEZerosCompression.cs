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

namespace OpenRA.Mods.Common.FileFormats
{
	// Run length encoded sequences of zeros (aka Format2)
	public static class RLEZerosCompression
	{
		public static void DecodeInto(byte[] src, byte[] dest, int destIndex)
		{
			var r = new FastByteReader(src);

			while (!r.Done())
			{
				var cmd = r.ReadByte();
				if (cmd == 0)
				{
					var count = r.ReadByte();
					while (count-- > 0)
						dest[destIndex++] = 0;
				}
				else
					dest[destIndex++] = cmd;
			}
		}
	}
}
