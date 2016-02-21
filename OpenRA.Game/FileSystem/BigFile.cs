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
using System.Linq;

namespace OpenRA.FileSystem
{
	public sealed class BigFile : IReadOnlyPackage
	{
		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
		readonly Stream s;

		public BigFile(FileSystem context, string filename)
		{
			Name = filename;

			s = context.Open(filename);
			try
			{
				if (s.ReadASCII(4) != "BIGF")
					throw new InvalidDataException("Header is not BIGF");

				// Total archive size.
				s.ReadUInt32();

				var entryCount = s.ReadUInt32();
				if (BitConverter.IsLittleEndian)
					entryCount = int2.Swap(entryCount);

				// First entry offset? This is apparently bogus for EA's .big files
				// and we don't have to try seeking there since the entries typically start next in EA's .big files.
				s.ReadUInt32();

				for (var i = 0; i < entryCount; i++)
				{
					var entry = new Entry(s);
					index.Add(entry.Path, entry);
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		class Entry
		{
			readonly Stream s;
			readonly uint offset;
			readonly uint size;
			public readonly string Path;

			public Entry(Stream s)
			{
				this.s = s;

				offset = s.ReadUInt32();
				size = s.ReadUInt32();
				if (BitConverter.IsLittleEndian)
				{
					offset = int2.Swap(offset);
					size = int2.Swap(size);
				}

				Path = s.ReadASCIIZ();
			}

			public Stream GetData()
			{
				s.Position = offset;
				return new MemoryStream(s.ReadBytes((int)size));
			}
		}

		public Stream GetStream(string filename)
		{
			return index[filename].GetData();
		}

		public bool Contains(string filename)
		{
			return index.ContainsKey(filename);
		}

		public void Dispose()
		{
			s.Dispose();
		}
	}
}