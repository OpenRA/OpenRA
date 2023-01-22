#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using ICSharpCode.SharpZipLib.Zip;

namespace OpenRA.FileSystem
{
	public class ZipFileLoader : IPackageLoader
	{
		static readonly uint ZipSignature = 0x04034b50;

		class ReadOnlyZipFile : IReadOnlyPackage
		{
			public string Name { get; protected set; }
			protected ZipFile pkg;

			// Dummy constructor for use with ReadWriteZipFile
			protected ReadOnlyZipFile() { }

			public ReadOnlyZipFile(Stream s, string filename)
			{
				Name = filename;
				pkg = new ZipFile(s);
			}

			public Stream GetStream(string filename)
			{
				var entry = pkg.GetEntry(filename);
				if (entry == null)
					return null;

				using (var z = pkg.GetInputStream(entry))
				{
					var ms = new MemoryStream((int)entry.Size);
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
						if (entry.IsFile)
							yield return entry.Name;
				}
			}

			public bool Contains(string filename)
			{
				return pkg.GetEntry(filename) != null;
			}

			public void Dispose()
			{
				pkg?.Close();
			}

			public IReadOnlyPackage OpenPackage(string filename, FileSystem context)
			{
				// Directories are stored with a trailing "/" in the index
				var entry = pkg.GetEntry(filename) ?? pkg.GetEntry(filename + "/");
				if (entry == null)
					return null;

				if (entry.IsDirectory)
					return new ZipFolder(this, filename);

				// Other package types can be loaded normally
				var s = GetStream(filename);
				if (s == null)
					return null;

				if (context.TryParsePackage(s, filename, out var package))
					return package;

				s.Dispose();
				return null;
			}
		}

		sealed class ReadWriteZipFile : ReadOnlyZipFile, IReadWritePackage
		{
			readonly MemoryStream pkgStream = new MemoryStream();

			public ReadWriteZipFile(string filename, bool create = false)
			{
				// SharpZipLib breaks when asked to update archives loaded from outside streams or files
				// We can work around this by creating a clean in-memory-only file, cutting all outside references
				if (!create)
				{
					using (var copy = new MemoryStream(File.ReadAllBytes(filename)))
					{
						pkgStream.Capacity = (int)copy.Length;
						copy.CopyTo(pkgStream);
					}
				}

				pkgStream.Position = 0;
				pkg = new ZipFile(pkgStream);
				Name = filename;
			}

			void Commit()
			{
				var pos = pkgStream.Position;
				pkgStream.Position = 0;
				File.WriteAllBytes(Name, pkgStream.ReadBytes((int)pkgStream.Length));
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
		}

		sealed class ZipFolder : IReadOnlyPackage
		{
			public string Name => path;
			public ReadOnlyZipFile Parent { get; }
			readonly string path;

			public ZipFolder(ReadOnlyZipFile parent, string path)
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
			var readSignature = s.ReadUInt32();
			s.Position -= 4;

			if (readSignature != ZipSignature)
			{
				package = null;
				return false;
			}

			package = new ReadOnlyZipFile(s, filename);
			return true;
		}

		public static bool TryParseReadWritePackage(string filename, out IReadWritePackage package)
		{
			using (var s = File.OpenRead(filename))
			{
				if (s.ReadUInt32() != ZipSignature)
				{
					package = null;
					return false;
				}
			}

			package = new ReadWriteZipFile(filename);
			return true;
		}

		public static IReadWritePackage Create(string filename)
		{
			return new ReadWriteZipFile(filename, true);
		}
	}
}
