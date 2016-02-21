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

namespace OpenRA.FileSystem
{
	struct Entry
	{
		public uint Offset;
		public uint Length;
		public string Filename;
	}

	public sealed class PakFile : IReadOnlyPackage
	{
		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		readonly Dictionary<string, Entry> index;
		readonly Stream stream;

		public PakFile(FileSystem context, string filename)
		{
			Name = filename;
			index = new Dictionary<string, Entry>();

			stream = context.Open(filename);
			try
			{
				index = new Dictionary<string, Entry>();
				var offset = stream.ReadUInt32();
				while (offset != 0)
				{
					var file = stream.ReadASCIIZ();
					var next = stream.ReadUInt32();
					var length = (next == 0 ? (uint)stream.Length : next) - offset;

					// Ignore duplicate files
					if (index.ContainsKey(file))
						continue;

					index.Add(file, new Entry { Offset = offset, Length = length, Filename = file });
					offset = next;
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public Stream GetStream(string filename)
		{
			Entry entry;
			if (!index.TryGetValue(filename, out entry))
				return null;

			stream.Seek(entry.Offset, SeekOrigin.Begin);
			var data = stream.ReadBytes((int)entry.Length);
			return new MemoryStream(data);
		}

		public bool Contains(string filename)
		{
			return index.ContainsKey(filename);
		}

		public void Dispose()
		{
			stream.Dispose();
		}
	}
}
