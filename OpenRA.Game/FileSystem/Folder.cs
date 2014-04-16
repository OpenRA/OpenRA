#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileSystem
{
	public class Folder : IFolder
	{
		readonly string path;
		readonly int priority;

		// Create a new folder package
		public Folder(string path, int priority, Dictionary<string, byte[]> contents)
		{
			this.path = path;
			this.priority = priority;
			if (Directory.Exists(path))
				Directory.Delete(path, true);

			Write(contents);
		}

		public Folder(string path, int priority)
		{
			this.path = path;
			this.priority = priority;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public Stream GetContent(string filename)
		{
			try { return File.OpenRead( Path.Combine( path, filename ) ); }
			catch { return null; }
		}

		public IEnumerable<uint> ClassicHashes()
		{
			foreach (var filename in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
				yield return PackageEntry.HashFilename(Path.GetFileName(filename), PackageHashType.Classic);
		}

		public IEnumerable<uint> CrcHashes()
		{
			yield break;
		}

		public IEnumerable<string> AllFileNames()
		{
			foreach (var filename in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
				yield return Path.GetFileName(filename);
		}

		public bool Exists(string filename)
		{
			return File.Exists(Path.Combine(path, filename));
		}

		public int Priority { get { return priority; } }
		public string Name { get { return path; } }

		public void Write(Dictionary<string, byte[]> contents)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			foreach (var file in contents)
				using (var dataStream = File.Create(Path.Combine(path, file.Key)))
					using (var writer = new BinaryWriter(dataStream))
					   writer.Write(file.Value);
		}
	}
}
