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
	public class DrsFile : IFolder
	{
		public string Name { get; private set; }
		public int Priority { get; private set; }

		readonly Dictionary<string, DrsEmbeddedFile> entries = new Dictionary<string, DrsEmbeddedFile>();

		public DrsFile(string filename, int priority)
		{
			Name = filename;
			Priority = priority;

			var s = GlobalFileSystem.Open(filename);

			var header = new DrsHeader(s, Name);
			if (header.TableCount < 1)
				throw new InvalidOperationException("Bogus header in {0}".F(filename));

			var tables = new DrsTable[header.TableCount];

			// Table headers are sequential.
			// They are not like so: [header][data], [header][data]
			// Instead they are: [header][header], [data][data]
			// So we read all of the header info before diving into data.
			for (var t = 0; t < tables.Length; t++)
				tables[t] = new DrsTable(s);

			foreach (var t in tables)
			{
				s.Position = t.Offset;
				for (var file = 0; file < t.FileCount; file++)
				{
					var f = new DrsEmbeddedFile(s, t);
					if (!entries.ContainsKey(f.GeneratedFilename))
						entries.Add(f.GeneratedFilename, f);
				}
			}
		}

		class DrsHeader
		{
			public readonly string CopyrightInfo;
			public readonly string Version;
			public readonly string ArchiveType;
			public readonly int TableCount;
			public readonly int FirstFileOffset;

			public DrsHeader(Stream s, string name)
			{
				if (s.Position != 0)
					throw new InvalidOperationException("Trying to read DRS header from somewhere other than the beginning of the archive.");

				CopyrightInfo = s.ReadASCII(40);
				Version = s.ReadASCII(4);
				ArchiveType = s.ReadASCII(12);
				if (ArchiveType != "tribe\0\0\0\0\0\0\0")
					throw new InvalidOperationException("Archive `{0}` is not a valid `tribe` archive.".F(name));

				TableCount = s.ReadInt32();
				FirstFileOffset = s.ReadInt32();
			}
		}

		class DrsTable
		{
			public readonly string FileType;
			public readonly string Extension;
			public readonly int Offset;
			public readonly int FileCount;

			public DrsTable(Stream s)
			{
				FileType = s.ReadASCII(1);
				Extension = s.ReadASCII(3).Reversed();
				Offset = s.ReadInt32();
				FileCount = s.ReadInt32();
			}
		}

		class DrsEmbeddedFile
		{
			public readonly int FileID;
			public readonly int Offset;
			public readonly int Length;
			public readonly string GeneratedFilename;

			readonly Stream s;

			public DrsEmbeddedFile(Stream s, DrsTable t)
			{
				this.s = s;

				if (s.Length - s.Position < 12)
					throw new InvalidOperationException("Going to read past end of stream. Something went wrong reading this DRS archive.");

				FileID = s.ReadInt32();
				Offset = s.ReadInt32();
				Length = s.ReadInt32();

				GeneratedFilename = "{0}.{1}".F(FileID.ToString(), t.Extension);
			}

			public Stream GetData()
			{
				s.Position = Offset;
				return new MemoryStream(s.ReadBytes(Length));
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
