#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.FileFormats
{
	public static class Format2
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
