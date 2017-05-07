#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileSystem
{
	public class ZipFileLoader : IPackageLoader
	{
		static readonly string[] Extensions = { ".zip", ".oramap" };

		sealed class ZipFile : IReadWritePackage
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

			public ZipFile(string filename, IReadWritePackage parent)
			{
				pkgStream = new MemoryStream();

				Name = filename;
				Parent = parent;
				pkg = SZipFile.Create(pkgStream);
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

			public IReadOnlyPackage OpenPackage(string filename, FileSystem context)
			{
				// Directories are stored with a trailing "/" in the index
				var entry = pkg.GetEntry(filename) ?? pkg.GetEntry(filename + "/");
				if (entry == null)
					return null;

				if (entry.IsDirectory)
					return new ZipFolder(this, filename);

				if (Extensions.Any(e => filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
					return new ZipFile(GetStream(filename), filename, this);

				// Other package types can be loaded normally
				IReadOnlyPackage package;
				var s = GetStream(filename);
				if (s == null)
					return null;

				if (context.TryParsePackage(s, filename, out package))
					return package;

				s.Dispose();
				return null;
			}
		}

		sealed class ZipFolder : IReadOnlyPackage
		{
			public string Name { get { return path; } }
			public ZipFile Parent { get; private set; }
			readonly string path;

			static ZipFolder()
			{
				ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
			}

			public ZipFolder(ZipFile parent, string path)
			{
				if (path.EndsWith("/", StringComparison.Ordinal))
					path = path.Substring(0, path.Length - 1);

				Parent = parent;
				this.path = path;
			}

			public Stream GetStream(string filename)
			{
				// Zip files use '/' as a path separator
				return Parent.GetStream(path + '/' + filename);
			}

			public IEnumerable<string> Contents
			{
				get
				{
					foreach (var entry in Parent.Contents)
					{
						if (entry.StartsWith(path, StringComparison.Ordinal) && entry != path)
						{
							var filename = entry.Substring(path.Length + 1);
							var dirLevels = filename.Split('/').Count(c => !string.IsNullOrEmpty(c));
							if (dirLevels == 1)
								yield return filename;
						}
					}
				}
			}

			public bool Contains(string filename)
			{
				return Parent.Contains(path + '/' + filename);
			}

			public IReadOnlyPackage OpenPackage(string filename, FileSystem context)
			{
				return Parent.OpenPackage(path + '/' + filename, context);
			}

			public void Dispose() { /* nothing to do */ }
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

		public bool TryParsePackage(Stream s, string filename, FileSystem context, out IReadOnlyPackage package)
		{
			if (!Extensions.Any(e => filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
			{
				package = null;
				return false;
			}

			string name;
			IReadOnlyPackage p;
			if (context.TryGetPackageContaining(filename, out p, out name))
				package = new ZipFile(p.GetStream(name), name, p);
			else
				package = new ZipFile(s, filename, null);

			return true;
		}

		public static IReadWritePackage Create(string filename, IReadWritePackage parent)
		{
			return new ZipFile(filename, parent);
		}
	}
}
