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
	public class XccLocalDatabase
	{
		public readonly string[] Entries;
		public XccLocalDatabase(Stream s)
		{
			// Skip unnecessary header data
			s.Seek(48, SeekOrigin.Begin);
			var reader = new BinaryReader(s);
			var count = reader.ReadInt32();
			Entries = new string[count];
			for (var i = 0; i < count; i++)
			{
				var chars = new List<char>();
				char c;
				while ((c = reader.ReadChar()) != 0)
					chars.Add(c);

				Entries[i] = new string(chars.ToArray());
			}
		}
	}
}