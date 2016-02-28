#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileSystem
{
	public sealed class ZipFile : IReadWritePackage
	{
		public string Name { get; private set; }
		SZipFile pkg;

		static ZipFile()
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
		}

		public ZipFile(FileSystem context, string filename, Stream stream, bool createOrClearContents = false)
		{
			Name = filename;

			if (createOrClearContents)
				pkg = SZipFile.Create(stream);
			else
				pkg = new SZipFile(stream);
		}

		public ZipFile(IReadOnlyFileSystem context, string filename, bool createOrClearContents = false)
		{
			Name = filename;

			if (createOrClearContents)
				pkg = SZipFile.Create(filename);
			else
				pkg = new SZipFile(filename);
		}

		public Stream GetStream(string filename)
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

		public IEnumerable<string> Contents
		{
			get
			{
				foreach (ZipEntry entry in pkg)
					yield return entry.Name;
			}
		}

		public bool Contains(string filename)
		{
			return pkg.GetEntry(filename) != null;
		}

		public void Update(string filename, byte[] contents)
		{
			pkg.BeginUpdate();
			pkg.Add(new StaticMemoryDataSource(contents), filename);
			pkg.CommitUpdate();
		}

		public void Delete(string filename)
		{
			pkg.BeginUpdate();
			pkg.Delete(filename);
			pkg.CommitUpdate();
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
