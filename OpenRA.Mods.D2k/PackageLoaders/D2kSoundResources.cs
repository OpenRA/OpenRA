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

namespace OpenRA.Mods.D2k.PackageLoaders
{
	public class D2kSoundResourcesLoader : IPackageLoader
	{
		sealed class D2kSoundResources : IReadOnlyPackage
		{
			readonly struct Entry
			{
				public readonly uint Offset;
				public readonly uint Length;

				public Entry(uint offset, uint length)
				{
					Offset = offset;
					Length = length;
				}
			}

			public string Name { get; }
			public IEnumerable<string> Contents => index.Keys;

			readonly Stream s;
			readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();

			public D2kSoundResources(Stream s, string filename)
			{
				Name = filename;
				this.s = s;

				try
				{
					var headerLength = s.ReadUInt32();
					while (s.Position < headerLength + 4)
					{
						var name = s.ReadASCIIZ();
						var offset = s.ReadUInt32();
						var length = s.ReadUInt32();
						index.Add(name, new Entry(offset, length));
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
				if (!index.TryGetValue(filename, out var e))
					return null;

				return SegmentStream.CreateWithoutOwningStream(s, e.Offset, (int)e.Length);
			}

			public IReadOnlyPackage OpenPackage(string filename, FileSystem.FileSystem context)
			{
				// Not implemented
				return null;
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

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FileSystem.FileSystem context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith(".rs", StringComparison.InvariantCultureIgnoreCase))
			{
				package = null;
				return false;
			}

			package = new D2kSoundResources(s, filename);
			return true;
		}
	}
}
