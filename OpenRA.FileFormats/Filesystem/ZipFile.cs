#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		readonly SZipFile pkg;
		int priority;

		public ZipFile(string filename, int priority)
		{
			this.priority = priority;
			try
			{
				pkg = new SZipFile(File.OpenRead(filename));
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
			if (File.Exists(filename))
				File.Delete(filename);
			
			pkg = SZipFile.Create(filename);
			Write(contents);
		}

		public Stream GetContent(string filename)
		{
			var ms = new MemoryStream();
			var z = pkg.GetInputStream(pkg.GetEntry(filename));
			int bufSize = 2048;
			byte[] buf = new byte[bufSize];
			while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
				ms.Write(buf, 0, bufSize);
			
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach(ZipEntry entry in pkg)
				yield return PackageEntry.HashFilename(entry.Name);
		}
		
		public bool Exists(string filename)
		{
			return pkg.GetEntry(filename) != null;
		}

		public int Priority
		{
			get { return 500 + priority; }
		}
		
		public void Write(Dictionary<string, byte[]> contents)
		{
			pkg.BeginUpdate();
			// TODO: Clear existing content?
			
			foreach (var kvp in contents)
			{
				pkg.Add(new StaticMemoryDataSource(kvp.Value), kvp.Key);
			}
			pkg.CommitUpdate();
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
