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
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public class FileSystem
	{
		public IEnumerable<IReadOnlyPackage> MountedPackages { get { return mountedPackages.Keys; } }
		readonly Dictionary<IReadOnlyPackage, int> mountedPackages = new Dictionary<IReadOnlyPackage, int>();
		Cache<string, List<IReadOnlyPackage>> fileIndex = new Cache<string, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());

		public IReadWritePackage CreatePackage(string filename, Dictionary<string, byte[]> content)
		{
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, content);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, content);

			return new Folder(filename, content);
		}

		public IReadOnlyPackage OpenPackage(string filename)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
				return new MixFile(this, filename);
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);
			if (filename.EndsWith(".RS", StringComparison.InvariantCultureIgnoreCase))
				return new D2kSoundResources(this, filename);
			if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldPackage(this, filename);
			if (filename.EndsWith(".PAK", StringComparison.InvariantCultureIgnoreCase))
				return new PakFile(this, filename);
			if (filename.EndsWith(".big", StringComparison.InvariantCultureIgnoreCase))
				return new BigFile(this, filename);
			if (filename.EndsWith(".bag", StringComparison.InvariantCultureIgnoreCase))
				return new BagFile(this, filename);
			if (filename.EndsWith(".hdr", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldCABExtractor(this, filename);

			return new Folder(filename);
		}

		public IReadWritePackage OpenWritablePackage(string filename)
		{
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);

			return new Folder(filename);
		}

		public void Mount(string name)
		{
			var optional = name.StartsWith("~");
			if (optional)
				name = name.Substring(1);

			name = Platform.ResolvePath(name);

			Action a = () => Mount(OpenPackage(name));
			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		public void Mount(IReadOnlyPackage package)
		{
			var mountCount = 0;
			if (mountedPackages.TryGetValue(package, out mountCount))
			{
				// Package is already mounted
				// Increment the mount count and bump up the file loading priority
				mountedPackages[package] = mountCount + 1;
				foreach (var filename in package.Contents)
				{
					fileIndex[filename].Remove(package);
					fileIndex[filename].Add(package);
				}
			}
			else
			{
				// Mounting the package for the first time
				mountedPackages.Add(package, 1);
				foreach (var filename in package.Contents)
					fileIndex[filename].Add(package);
			}
		}

		public bool Unmount(IReadOnlyPackage package)
		{
			var mountCount = 0;
			if (!mountedPackages.TryGetValue(package, out mountCount))
				return false;

			if (--mountCount <= 0)
			{
				foreach (var packagesForFile in fileIndex.Values)
					packagesForFile.RemoveAll(p => p == package);

				mountedPackages.Remove(package);
				package.Dispose();
			}
			else
				mountedPackages[package] = mountCount;

			return true;
		}

		public void UnmountAll()
		{
			foreach (var package in mountedPackages.Keys)
				package.Dispose();

			mountedPackages.Clear();
			fileIndex = new Cache<string, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());
		}

		public void LoadFromManifest(Manifest manifest)
		{
			UnmountAll();
			foreach (var pkg in manifest.Packages)
				Mount(pkg);
		}

		Stream GetFromCache(string filename)
		{
			var package = fileIndex[filename]
				.LastOrDefault(x => x.Contains(filename));

			if (package != null)
				return package.GetStream(filename);

			return null;
		}

		public Stream Open(string filename)
		{
			Stream s;
			if (!TryOpen(filename, out s))
				throw new FileNotFoundException("File not found: {0}".F(filename), filename);

			return s;
		}

		public bool TryOpen(string name, out Stream s)
		{
			var filename = name;
			var packageName = string.Empty;

			// Used for faction specific packages; rule out false positive on Windows C:\ drive notation
			var explicitPackage = name.Contains(':') && !Directory.Exists(Path.GetDirectoryName(name));
			if (explicitPackage)
			{
				var divide = name.Split(':');
				packageName = divide.First();
				filename = divide.Last();
			}

			// Check the cache for a quick lookup if the package name is unknown
			// TODO: This disables caching for explicit package requests
			if (filename.IndexOfAny(new[] { '/', '\\' }) == -1 && !explicitPackage)
			{
				s = GetFromCache(filename);
				if (s != null)
					return true;
			}

			// Ask each package individually
			IReadOnlyPackage package;
			if (explicitPackage && !string.IsNullOrEmpty(packageName))
				package = mountedPackages.Keys.LastOrDefault(x => x.Name == packageName);
			else
				package = mountedPackages.Keys.LastOrDefault(x => x.Contains(filename));

			if (package != null)
			{
				s = package.GetStream(filename);
				return true;
			}

			s = null;
			return false;
		}

		public bool Exists(string name)
		{
			var explicitPackage = name.Contains(':') && !Directory.Exists(Path.GetDirectoryName(name));
			if (explicitPackage)
			{
				var divide = name.Split(':');
				var packageName = divide.First();
				var filename = divide.Last();
				return mountedPackages.Keys.Where(n => n.Name == packageName).Any(f => f.Contains(filename));
			}
			else
				return mountedPackages.Keys.Any(f => f.Contains(name));
		}
	}
}
