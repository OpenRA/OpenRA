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
	public class D2kSoundResources : IFolder
	{
		readonly Stream s;

		readonly string filename;
		readonly List<string> filenames;
		readonly int priority;

		readonly Dictionary<uint, PackageEntry> index = new Dictionary<uint, PackageEntry>();

		public D2kSoundResources(string filename, int priority)
		{
			this.filename = filename;
			this.priority = priority;

			s = GlobalFileSystem.Open(filename);
			s.Seek(0, SeekOrigin.Begin);

			filenames = new List<string>();

			var headerLength = s.ReadUInt32();
			while (s.Position < headerLength + 4)
			{
				var name = s.ReadASCIIZ();
				var offset = s.ReadUInt32();
				var length = s.ReadUInt32();

				var hash = PackageEntry.HashFilename(name, PackageHashType.Classic);
				if (!index.ContainsKey(hash))
					index.Add(hash, new PackageEntry(hash, offset, length));

				filenames.Add(name);
			}
		}

		public Stream GetContent(uint hash)
		{
			PackageEntry e;
			if (!index.TryGetValue(hash, out e))
				return null;

			s.Seek(e.Offset, SeekOrigin.Begin);
			var data = new byte[e.Length];
			s.Read(data, 0, (int)e.Length);

			return new MemoryStream(data);
		}

		public Stream GetContent(string filename)
		{
			return GetContent(PackageEntry.HashFilename(filename, PackageHashType.Classic));
		}

		public bool Exists(string filename)
		{
			return index.ContainsKey(PackageEntry.HashFilename(filename, PackageHashType.Classic));
		}

		public IEnumerable<string> AllFileNames()
		{
			return filenames;
		}

		public string Name { get { return filename; } }

		public int Priority { get { return 1000 + priority; }}

		public IEnumerable<uint> ClassicHashes()
		{
			return index.Keys;
		}

		public IEnumerable<uint> CrcHashes()
		{
			yield break;
		}

		public void Write(Dictionary<string, byte[]> contents)
		{
			throw new NotImplementedException("Cannot save Dune 2000 Sound Resources.");
		}
	}
}
