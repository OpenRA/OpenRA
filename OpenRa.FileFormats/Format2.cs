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

namespace OpenRa.FileFormats
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
