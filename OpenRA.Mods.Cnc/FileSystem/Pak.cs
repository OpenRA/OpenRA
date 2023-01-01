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
	public class PakFileLoader : IPackageLoader
	{
		struct Entry
		{
			public uint Offset;
			public uint Length;
			public string Filename;
		}

		sealed class PakFile : IReadOnlyPackage
		{
			public string Name { get; }
			public IEnumerable<string> Contents => index.Keys;

			readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
			readonly Stream stream;

			public PakFile(Stream stream, string filename)
			{
				Name = filename;
				this.stream = stream;

				try
				{
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
				if (!index.TryGetValue(filename, out var entry))
					return null;

				return SegmentStream.CreateWithoutOwningStream(stream, entry.Offset, (int)entry.Length);
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
				stream.Dispose();
			}
		}

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith(".pak", StringComparison.InvariantCultureIgnoreCase))
			{
				package = null;
				return false;
			}

			package = new PakFile(s, filename);
			return true;
		}
	}
}
