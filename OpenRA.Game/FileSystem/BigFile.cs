#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileSystem
{
	public class BigFile : IFolder
	{
		public string Name { get; private set; }
		public int Priority { get; private set; }
		readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

		public BigFile(string filename, int priority)
		{
			Name = filename;
			Priority = priority;

			var s = GlobalFileSystem.Open(filename);

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
				entries.Add(entry.Path, entry);
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

		public Stream GetContent(string filename)
		{
			return entries[filename].GetData();
		}

		public bool Exists(string filename)
		{
			return entries.ContainsKey(filename);
		}

		public IEnumerable<uint> ClassicHashes()
		{
			return entries.Keys.Select(filename => PackageEntry.HashFilename(filename, PackageHashType.Classic));
		}

		public IEnumerable<uint> CrcHashes()
		{
			return Enumerable.Empty<uint>();
		}

		public IEnumerable<string> AllFileNames()
		{
			return entries.Keys;
		}

		public void Write(Dictionary<string, byte[]> contents)
		{
			throw new NotImplementedException();
		}
	}
}