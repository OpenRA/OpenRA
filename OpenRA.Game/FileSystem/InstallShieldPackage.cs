#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.FileSystem
{
	public sealed class InstallShieldPackage : IReadOnlyPackage
	{
		struct Entry
		{
			public readonly uint Offset;
			public readonly uint Length;

			public Entry(uint offset, uint length)
			{
				Offset = offset;
				Length = length;
			}
		}

		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
		readonly Stream s;
		readonly long dataStart = 255;

		public InstallShieldPackage(FileSystem context, string filename)
		{
			Name = filename;

			s = context.Open(filename);
			try
			{
				// Parse package header
				var signature = s.ReadUInt32();
				if (signature != 0x8C655D13)
					throw new InvalidDataException("Not an Installshield package");

				s.Position += 8;
				/*var FileCount = */s.ReadUInt16();
				s.Position += 4;
				/*var ArchiveSize = */s.ReadUInt32();
				s.Position += 19;
				var tocAddress = s.ReadInt32();
				s.Position += 4;
				var dirCount = s.ReadUInt16();

				// Parse the directory list
				s.Position = tocAddress;

				// Parse directories
				var directories = new Dictionary<string, uint>();
				for (var i = 0; i < dirCount; i++)
				{
					// Parse directory header
					var fileCount = s.ReadUInt16();
					var chunkSize = s.ReadUInt16();
					var nameLength = s.ReadUInt16();
					var dirName = s.ReadASCII(nameLength);

					// Skip to the end of the chunk
					s.ReadBytes(chunkSize - nameLength - 6);
					directories.Add(dirName, fileCount);
				}

				// Parse files
				foreach (var dir in directories)
					for (var i = 0; i < dir.Value; i++)
						ParseFile(s, dir.Key);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		uint accumulatedData = 0;
		void ParseFile(Stream s, string dirName)
		{
			s.Position += 7;
			var compressedSize = s.ReadUInt32();
			s.Position += 12;
			var chunkSize = s.ReadUInt16();
			s.Position += 4;
			var nameLength = s.ReadByte();
			var fileName = dirName + "\\" + s.ReadASCII(nameLength);

			// Use index syntax to overwrite any duplicate entries with the last value
			index[fileName] = new Entry(accumulatedData, compressedSize);
			accumulatedData += compressedSize;

			// Skip to the end of the chunk
			s.Position += chunkSize - nameLength - 30;
		}

		public Stream GetStream(string filename)
		{
			Entry e;
			if (!index.TryGetValue(filename, out e))
				return null;

			s.Seek(dataStart + e.Offset, SeekOrigin.Begin);
			var data = s.ReadBytes((int)e.Length);

			return new MemoryStream(Blast.Decompress(data));
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
