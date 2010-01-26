using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.FileFormats
{
	public static class ProtocolVersion
	{
		// you *must* increment this whenever you make an incompatible protocol change
		public static readonly int Version = 2;
	}
}
