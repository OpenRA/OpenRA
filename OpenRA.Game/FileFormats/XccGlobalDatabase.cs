#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class XccGlobalDatabase : IDisposable
	{
		public readonly string[] Entries;
		readonly Stream s;

		public XccGlobalDatabase(Stream stream)
		{
			s = stream;

			var entries = new List<string>();
			while (s.Peek() > -1)
			{
				var count = s.ReadInt32();
				for (var i = 0; i < count; i++)
				{
					var chars = new List<char>();
					byte c;

					// Read filename
					while ((c = s.ReadUInt8()) != 0)
						chars.Add((char)c);
					entries.Add(new string(chars.ToArray()));

					// Skip comment
					while ((c = s.ReadUInt8()) != 0) { }
				}
			}

			Entries = entries.ToArray();
		}

		public void Dispose()
		{
			s.Dispose();
		}
	}
}