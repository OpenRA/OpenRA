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

using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;

namespace OpenRA.FileFormats
{
	public class IdxReader
	{
		public readonly int SoundCount;
		public List<IdxEntry> Entries;

		public IdxReader(Stream s)
		{
			s.Seek(0, SeekOrigin.Begin);

			var id = s.ReadASCII(4);

			if (id != "GABA")
				throw new InvalidDataException("Unable to load Idx file, did not find magic id, found {0} instead".F(id));

			var two = s.ReadInt32();

			if (two != 2)
				throw new InvalidDataException("Unable to load Idx file, did not find magic number 2, found {0} instead".F(two));

			SoundCount = s.ReadInt32();

			Entries = new List<IdxEntry>();

			for (var i = 0; i < SoundCount; i++)
				Entries.Add(new IdxEntry(s));
		}
	}
}