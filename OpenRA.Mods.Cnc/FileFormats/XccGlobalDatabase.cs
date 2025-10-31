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
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public sealed class XccGlobalDatabase : IDisposable
	{
		public readonly string[] Entries;
		readonly Stream s;

		public XccGlobalDatabase(Stream stream)
		{
			s = stream;

			var entries = new List<string>();
			var chars = new char[32];
			while (s.Peek() > -1)
			{
				var count = s.ReadInt32();
				entries.Capacity += count;
				for (var i = 0; i < count; i++)
				{
					// Read filename
					byte c;
					var charsIndex = 0;
					while ((c = s.ReadUInt8()) != 0)
					{
						if (charsIndex >= chars.Length)
							Array.Resize(ref chars, chars.Length * 2);
						chars[charsIndex++] = (char)c;
					}

					entries.Add(new string(chars, 0, charsIndex));

					// Skip comment
					while (s.ReadUInt8() != 0) { }
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
