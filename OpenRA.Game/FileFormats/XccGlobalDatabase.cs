#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class XccGlobalDatabase
	{
		public readonly string[] Entries;
		public XccGlobalDatabase(Stream s)
		{
			var entries = new List<string>();
			var reader = new BinaryReader(s);
			while (reader.PeekChar() > -1)
			{
				var count = reader.ReadInt32();
				for (var i = 0; i < count; i++)
				{
					var chars = new List<char>();
					char c;

					// Read filename
					while ((c = reader.ReadChar()) != 0)
						chars.Add(c);
					entries.Add(new string(chars.ToArray()));

					// Skip comment
					while ((c = reader.ReadChar()) != 0);
				}
			}

			Entries = entries.ToArray();
		}
	}
}