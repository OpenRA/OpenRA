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
		readonly Dictionary<string, IReadOnlyPackage> explicitMounts = new Dictionary<string, IReadOnlyPackage>();

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

			IReadOnlyPackage parent;
			string subPath = null;
			if (TryGetPackageContaining(filename, out parent, out subPath))
				if (parent is Folder)
					return new Folder(Path.Combine(((Folder)parent).Name, subPath));

			return new Folder(Platform.ResolvePath(filename));
		}

		public IReadWritePackage OpenWritablePackage(string filename)
		{
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename);

			return new Folder(filename);
		}

		public void Mount(string name, string explicitName = null)
		{
			var optional = name.StartsWith("~");
			if (optional)
				name = name.Substring(1);

			Action a = () => Mount(OpenPackage(name), explicitName);
			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		public void Mount(IReadOnlyPackage package, string explicitName = null)
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

				if (explicitName != null)
					explicitMounts.Add(explicitName, package);

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
				explicitMounts.Remove(package.Name);
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
			explicitMounts.Clear();
			fileIndex = new Cache<string, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());
		}

		public void LoadFromManifest(Manifest manifest)
		{
			UnmountAll();
			foreach (var kv in manifest.Packages)
				Mount(kv.Key, kv.Value);
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

		public bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename)
		{
			var explicitSplit = path.IndexOf('|');
			if (explicitSplit > 0 && explicitMounts.TryGetValue(path.Substring(0, explicitSplit), out package))
			{
				filename = path.Substring(explicitSplit + 1);
				return true;
			}

			package = fileIndex[path].LastOrDefault(x => x.Contains(path));
			filename = path;

			return package != null;
		}

		public bool TryOpen(string filename, out Stream s)
		{
			var explicitSplit = filename.IndexOf('|');
			if (explicitSplit > 0)
			{
				IReadOnlyPackage explicitPackage;
				if (explicitMounts.TryGetValue(filename.Substring(0, explicitSplit), out explicitPackage))
				{
					s = explicitPackage.GetStream(filename.Substring(explicitSplit + 1));
					if (s != null)
						return true;
				}
			}

			s = GetFromCache(filename);
			if (s != null)
				return true;

			// Ask each package individually
			// TODO: This fallback can be removed once the filesystem cleanups are complete
			var	package = mountedPackages.Keys.LastOrDefault(x => x.Contains(filename));
			if (package != null)
			{
				s = package.GetStream(filename);
				return s != null;
			}

			s = null;
			return false;
		}

		public bool Exists(string filename)
		{
			var explicitSplit = filename.IndexOf('|');
			if (explicitSplit > 0)
			{
				IReadOnlyPackage explicitPackage;
				if (explicitMounts.TryGetValue(filename.Substring(0, explicitSplit), out explicitPackage))
					if (explicitPackage.Contains(filename.Substring(explicitSplit + 1)))
						return true;
			}

			return fileIndex.ContainsKey(filename);
		}
	}
}
