#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.FileFormats
{
	public static class Format2
	{
		public static int DecodeInto(byte[] src, byte[] dest)
		{
			FastByteReader r = new FastByteReader(src);

			int i = 0;
			while (!r.Done())
			{
				byte cmd = r.ReadByte();
				if (cmd == 0)
				{
					byte count = r.ReadByte();
					while (count-- > 0)
						dest[i++] = 0;
				}
				else
					dest[i++] = cmd;
			}

			return i;
		}
	}
}
