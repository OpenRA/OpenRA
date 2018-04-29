#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public interface IReadOnlyFileSystem
	{
		Stream Open(string filename);
		bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename);
		bool TryOpen(string filename, out Stream s);
		bool Exists(string filename);
		bool IsExternalModFile(string filename);
	}

	public class FileSystem : IReadOnlyFileSystem
	{
		public IEnumerable<IReadOnlyPackage> MountedPackages { get { return mountedPackages.Keys; } }
		readonly Dictionary<IReadOnlyPackage, int> mountedPackages = new Dictionary<IReadOnlyPackage, int>();
		readonly Dictionary<string, IReadOnlyPackage> explicitMounts = new Dictionary<string, IReadOnlyPackage>();
		readonly string modID;

		// Mod packages that should not be disposed
		readonly List<IReadOnlyPackage> modPackages = new List<IReadOnlyPackage>();
		readonly IReadOnlyDictionary<string, Manifest> installedMods;
		readonly IPackageLoader[] packageLoaders;

		Cache<string, List<IReadOnlyPackage>> fileIndex = new Cache<string, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());

		public FileSystem(string modID, IReadOnlyDictionary<string, Manifest> installedMods, IPackageLoader[] packageLoaders)
		{
			this.modID = modID;
			this.installedMods = installedMods;
			this.packageLoaders = packageLoaders
				.Append(new ZipFileLoader())
				.ToArray();
		}

		public bool TryParsePackage(Stream stream, string filename, out IReadOnlyPackage package)
		{
			package = null;
			foreach (var packageLoader in packageLoaders)
				if (packageLoader.TryParsePackage(stream, filename, this, out package))
					return true;

			return false;
		}

		public IReadOnlyPackage OpenPackage(string filename)
		{
			// Raw directories are the easiest and one of the most common cases, so try these first
			var resolvedPath = Platform.ResolvePath(filename);
			if (!filename.Contains("|") && Directory.Exists(resolvedPath))
				return new Folder(resolvedPath);

			// Children of another package require special handling
			IReadOnlyPackage parent;
			string subPath = null;
			if (TryGetPackageContaining(filename, out parent, out subPath))
				return parent.OpenPackage(subPath, this);

			// Try and open it normally
			IReadOnlyPackage package;
			var stream = Open(filename);
			if (TryParsePackage(stream, filename, out package))
				return package;

			// No package loaders took ownership of the stream, so clean it up
			stream.Dispose();

			return null;
		}

		public void Mount(string name, string explicitName = null)
		{
			var optional = name.StartsWith("~", StringComparison.Ordinal);
			if (optional)
				name = name.Substring(1);

			try
			{
				IReadOnlyPackage package;
				if (name.StartsWith("$", StringComparison.Ordinal))
				{
					name = name.Substring(1);

					Manifest mod;
					if (!installedMods.TryGetValue(name, out mod))
						throw new InvalidOperationException("Could not load mod '{0}'. Available mods: {1}".F(name, installedMods.Keys.JoinWith(", ")));

					package = mod.Package;
					modPackages.Add(package);
				}
				else
				{
					package = OpenPackage(name);
					if (package == null)
						throw new InvalidOperationException("Could not open package '{0}', file not found or its format is not supported.".F(name));
				}

				Mount(package, explicitName);
			}
			catch
			{
				if (!optional)
					throw;
			}
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
				var explicitKeys = explicitMounts.Where(kv => kv.Value == package)
					.Select(kv => kv.Key)
					.ToList();

				foreach (var key in explicitKeys)
					explicitMounts.Remove(key);

				// Mod packages aren't owned by us, so we shouldn't dispose them
				if (modPackages.Contains(package))
					modPackages.Remove(package);
				else
					package.Dispose();
			}
			else
				mountedPackages[package] = mountCount;

			return true;
		}

		public void UnmountAll()
		{
			foreach (var package in mountedPackages.Keys)
				if (!modPackages.Contains(package))
					package.Dispose();

			mountedPackages.Clear();
			explicitMounts.Clear();
			modPackages.Clear();

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

			// The file should be in an explicit package (but we couldn't find it)
			// Thus don't try to find it using the filename (which contains the invalid '|' char)
			// This can be removed once the TODO below is resolved
			if (explicitSplit > 0)
				return false;

			// Ask each package individually
			// TODO: This fallback can be removed once the filesystem cleanups are complete
			var package = mountedPackages.Keys.LastOrDefault(x => x.Contains(filename));
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

		/// <summary>
		/// Returns true if the given filename references an external mod via an explicit mount
		/// </summary>
		public bool IsExternalModFile(string filename)
		{
			var explicitSplit = filename.IndexOf('|');
			if (explicitSplit < 0)
				return false;

			IReadOnlyPackage explicitPackage;
			if (!explicitMounts.TryGetValue(filename.Substring(0, explicitSplit), out explicitPackage))
				return false;

			if (installedMods[modID].Package == explicitPackage)
				return false;

			return modPackages.Contains(explicitPackage);
		}

		/// <summary>
		/// Resolves a filesystem for an assembly, accounting for explicit and mod mounts.
		/// Assemblies must exist in the native OS file system (not inside an OpenRA-defined package).
		/// </summary>
		public static string ResolveAssemblyPath(string path, Manifest manifest, InstalledMods installedMods)
		{
			var explicitSplit = path.IndexOf('|');
			if (explicitSplit > 0)
			{
				var parent = path.Substring(0, explicitSplit);
				var filename = path.Substring(explicitSplit + 1);

				var parentPath = manifest.Packages.FirstOrDefault(kv => kv.Value == parent).Key;
				if (parentPath == null)
					return null;

				if (parentPath.StartsWith("$", StringComparison.Ordinal))
				{
					Manifest mod;
					if (!installedMods.TryGetValue(parentPath.Substring(1), out mod))
						return null;

					if (!(mod.Package is Folder))
						return null;

					 path = Path.Combine(mod.Package.Name, filename);
				}
				else
					path = Path.Combine(parentPath, filename);
			}

			var resolvedPath = Platform.ResolvePath(path);
			return File.Exists(resolvedPath) ? resolvedPath : null;
		}

		public string GetPrefix(IReadOnlyPackage package)
		{
			return explicitMounts.ContainsValue(package) ? explicitMounts.First(f => f.Value == package).Key : null;
		}
	}
}
