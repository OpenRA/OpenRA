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
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileSystem
{
	public sealed class ZipFile : IFolder
	{
		readonly string filename;
		readonly int priority;
		SZipFile pkg;

		static ZipFile()
		{
			ZipConstants.DefaultCodePage = Encoding.Default.CodePage;
		}

		public ZipFile(FileSystem context, string filename, int priority)
		{
			this.filename = filename;
			this.priority = priority;

			try
			{
				// Pull the file into memory, don't keep it open.
				pkg = new SZipFile(new MemoryStream(File.ReadAllBytes(filename)));
			}
			catch (ZipException e)
			{
				Log.Write("debug", "Couldn't load zip file: {0}", e.Message);
			}
		}

		// Create a new zip with the specified contents.
		public ZipFile(FileSystem context, string filename, int priority, Dictionary<string, byte[]> contents)
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
			var entry = pkg.GetEntry(filename);
			if (entry == null)
				return null;

			using (var z = pkg.GetInputStream(entry))
			{
				var ms = new MemoryStream();
				z.CopyTo(ms);
				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			}
		}

		public IEnumerable<uint> ClassicHashes()
		{
			foreach (ZipEntry entry in pkg)
				yield return PackageEntry.HashFilename(entry.Name, PackageHashType.Classic);
		}

		public IEnumerable<uint> CrcHashes()
		{
			yield break;
		}

		public IEnumerable<string> AllFileNames()
		{
			foreach (ZipEntry entry in pkg)
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
			// TODO: Clear existing content?
			pkg.Close();
			pkg = SZipFile.Create(filename);
			pkg.BeginUpdate();

			foreach (var kvp in contents)
				pkg.Add(new StaticMemoryDataSource(kvp.Value), kvp.Key);

			pkg.CommitUpdate();
			pkg.Close();
			pkg = new SZipFile(new MemoryStream(File.ReadAllBytes(filename)));
		}

		public void Dispose()
		{
			if (pkg != null)
				pkg.Close();
		}
	}

	class StaticMemoryDataSource : IStaticDataSource
	{
		byte[] data;
		public StaticMemoryDataSource(byte[] data)
		{
			this.data = data;
		}

		public Stream GetSource()
		{
			return new MemoryStream(data);
		}
	}
}
