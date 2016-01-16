#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public sealed class D2kSoundResources : IReadOnlyPackage
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

		readonly Stream s;

		readonly string filename;
		readonly int priority;
		readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();

		public D2kSoundResources(FileSystem context, string filename, int priority)
		{
			this.filename = filename;
			this.priority = priority;

			s = context.Open(filename);
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

		public Stream GetContent(string filename)
		{
			Entry e;
			if (!index.TryGetValue(filename, out e))
				return null;

			s.Seek(e.Offset, SeekOrigin.Begin);
			return new MemoryStream(s.ReadBytes((int)e.Length));
		}

		public bool Exists(string filename)
		{
			return index.ContainsKey(filename);
		}

		public IEnumerable<string> AllFileNames()
		{
			return index.Keys;
		}

		public string Name { get { return filename; } }

		public int Priority { get { return 1000 + priority; } }

		public void Dispose()
		{
			s.Dispose();
		}
	}
}
