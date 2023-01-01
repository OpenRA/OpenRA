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
using OpenRA.FileSystem;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public class BigLoader : IPackageLoader
	{
		sealed class BigFile : IReadOnlyPackage
		{
			public string Name { get; }
			public IEnumerable<string> Contents => index.Keys;

			readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
			readonly Stream s;

			public BigFile(Stream s, string filename)
			{
				Name = filename;
				this.s = s;

				try
				{
					/* var signature = */ s.ReadASCII(4);

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
				public readonly uint Offset;
				public readonly uint Size;
				public readonly string Path;

				public Entry(Stream s)
				{
					Offset = s.ReadUInt32();
					Size = s.ReadUInt32();
					if (BitConverter.IsLittleEndian)
					{
						Offset = int2.Swap(Offset);
						Size = int2.Swap(Size);
					}

					Path = s.ReadASCIIZ();
				}
			}

			public Stream GetStream(string filename)
			{
				var entry = index[filename];
				return SegmentStream.CreateWithoutOwningStream(s, entry.Offset, (int)entry.Size);
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				// Not implemented
				return null;
			}

			public void Dispose()
			{
				s.Dispose();
			}
		}

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			// Take a peek at the file signature
			var signature = s.ReadASCII(4);
			s.Position -= 4;

			if (signature != "BIGF")
			{
				package = null;
				return false;
			}

			package = new BigFile(s, filename);
			return true;
		}
	}
}
