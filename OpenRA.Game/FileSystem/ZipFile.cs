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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileSystem
{
	public sealed class ZipFile : IReadWritePackage
	{
		public IReadWritePackage Parent { get; private set; }
		public string Name { get; private set; }
		readonly Stream pkgStream;
		readonly SZipFile pkg;

		static ZipFile()
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
		}

		public ZipFile(Stream stream, string name, IReadOnlyPackage parent = null)
		{
			// SharpZipLib breaks when asked to update archives loaded from outside streams or files
			// We can work around this by creating a clean in-memory-only file, cutting all outside references
			pkgStream = new MemoryStream();
			stream.CopyTo(pkgStream);
			pkgStream.Position = 0;

			Name = name;
			Parent = parent as IReadWritePackage;
			pkg = new SZipFile(pkgStream);
		}

		public ZipFile(IReadOnlyFileSystem context, string filename)
		{
			string name;
			IReadOnlyPackage p;
			if (!context.TryGetPackageContaining(filename, out p, out name))
				throw new FileNotFoundException("Unable to find parent package for " + filename);

			Name = name;
			Parent = p as IReadWritePackage;

			// SharpZipLib breaks when asked to update archives loaded from outside streams or files
			// We can work around this by creating a clean in-memory-only file, cutting all outside references
			pkgStream = new MemoryStream();
			p.GetStream(name).CopyTo(pkgStream);
			pkgStream.Position = 0;

			pkg = new SZipFile(pkgStream);
		}

		ZipFile(string filename, IReadWritePackage parent)
		{
			pkgStream = new MemoryStream();

			Name = filename;
			Parent = parent;
			pkg = SZipFile.Create(pkgStream);
		}

		public static ZipFile Create(string filename, IReadWritePackage parent)
		{
			return new ZipFile(filename, parent);
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

		void Commit()
		{
			if (Parent == null)
				throw new InvalidDataException("Cannot update ZipFile without writable parent");

			var pos = pkgStream.Position;
			pkgStream.Position = 0;
			Parent.Update(Name, pkgStream.ReadBytes((int)pkgStream.Length));
			pkgStream.Position = pos;
		}

		public void Update(string filename, byte[] contents)
		{
			pkg.BeginUpdate();
			pkg.Add(new StaticStreamDataSource(new MemoryStream(contents)), filename);
			pkg.CommitUpdate();
			Commit();
		}

		public void Delete(string filename)
		{
			pkg.BeginUpdate();
			pkg.Delete(filename);
			pkg.CommitUpdate();
			Commit();
		}

		public void Dispose()
		{
			if (pkg != null)
				pkg.Close();

			if (pkgStream != null)
				pkgStream.Dispose();
		}
	}

	class StaticStreamDataSource : IStaticDataSource
	{
		readonly Stream s;
		public StaticStreamDataSource(Stream s)
		{
			this.s = s;
		}

		public Stream GetSource()
		{
			return s;
		}
	}
}
