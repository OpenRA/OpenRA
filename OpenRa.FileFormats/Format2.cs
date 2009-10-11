using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
