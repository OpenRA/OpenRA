#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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

	public class PakFile : IFolder
	{
		string filename;
		int priority;
		Dictionary<string, Entry> index;
		Stream stream;

		public PakFile(string filename, int priority)
		{
			this.filename = filename;
			this.priority = priority;
			index = new Dictionary<string, Entry>();
			stream = GlobalFileSystem.Open(filename);

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

		public Stream GetContent(string filename)
		{
			Entry entry;
			if (!index.TryGetValue(filename, out entry))
				return null;

			stream.Seek(entry.Offset, SeekOrigin.Begin);
			var data = stream.ReadBytes((int)entry.Length);
			return new MemoryStream(data);
		}

		public IEnumerable<uint> ClassicHashes()
		{
			foreach (var filename in index.Keys)
				yield return PackageEntry.HashFilename(filename, PackageHashType.Classic);
		}

		public IEnumerable<uint> CrcHashes()
		{
			yield break;
		}

		public IEnumerable<string> AllFileNames()
		{
			foreach (var filename in index.Keys)
				yield return filename;
		}

		public bool Exists(string filename)
		{
			return index.ContainsKey(filename);
		}

		public void Write(Dictionary<string, byte[]> contents)
		{
			throw new NotImplementedException("Cannot save Pak archives.");
		}

		public int Priority { get { return 1000 + priority; } }
		public string Name { get { return filename; } }
	}
}
