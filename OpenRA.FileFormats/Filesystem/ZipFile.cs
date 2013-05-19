﻿#region Copyright & License Information
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
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileFormats
{
	public class ZipFile : IFolder
	{
		string filename;
		SZipFile pkg;
		int priority;

		public ZipFile(string filename, int priority)
		{
			this.filename = filename;
			this.priority = priority;
			try
			{
				// pull the file into memory, dont keep it open.
				pkg = new SZipFile(new MemoryStream(File.ReadAllBytes(filename)));
			}
			catch (ZipException e)
			{
				Log.Write("debug", "Couldn't load zip file: {0}", e.Message);
			}
		}

		// Create a new zip with the specified contents
		public ZipFile(string filename, int priority, Dictionary<string, byte[]> contents)
		{
			this.priority = priority;
			this.filename = filename;

			if (File.Exists(filename))
				File.Delete(filename);

			pkg = SZipFile.Create(filename);
			Write(contents);
		}

		public Stream GetContent(string filename)
		{
			using (var z = pkg.GetInputStream(pkg.GetEntry(filename)))
			{
				var ms = new MemoryStream();
				int bufSize = 2048;
				byte[] buf = new byte[bufSize];
				while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
					ms.Write(buf, 0, bufSize);

				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			}
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach(ZipEntry entry in pkg)
				yield return PackageEntry.HashFilename(entry.Name);
		}

		public IEnumerable<string> AllFileNames()
		{
			foreach(ZipEntry entry in pkg)
				yield return entry.Name;
		}

		public bool Exists(string filename)
		{
			return pkg.GetEntry(filename) != null;
		}

		public int Priority { get { return 500 + priority; } }
		public string Name { get { return filename; } }

		public void Write(Dictionary<string, byte[]> contents)
		{
			pkg.Close();

			pkg = SZipFile.Create(filename);

			pkg.BeginUpdate();
			// TODO: Clear existing content?

			foreach (var kvp in contents)
				pkg.Add(new StaticMemoryDataSource(kvp.Value), kvp.Key);

			pkg.CommitUpdate();

			pkg.Close();

			pkg = new SZipFile(new MemoryStream(File.ReadAllBytes(filename)));
		}
	}

	class StaticMemoryDataSource : IStaticDataSource
	{
		byte[] data;
		public StaticMemoryDataSource (byte[] data)
		{
			this.data = data;
		}

		public Stream GetSource()
		{
			return new MemoryStream(data);
		}
	}
}
