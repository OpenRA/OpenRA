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
		public readonly List<string> PackagePaths = new List<string>();
		public readonly List<IReadOnlyPackage> MountedPackages = new List<IReadOnlyPackage>();

		static readonly Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

		int order;
		Cache<uint, List<IReadOnlyPackage>> crcHashIndex = new Cache<uint, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());
		Cache<uint, List<IReadOnlyPackage>> classicHashIndex = new Cache<uint, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());

		public IReadWritePackage CreatePackage(string filename, int order, Dictionary<string, byte[]> content)
		{
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order, content);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order, content);

			return new Folder(filename, order, content);
		}

		public IReadOnlyPackage OpenPackage(string filename, string annotation, int order)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
			{
				var type = string.IsNullOrEmpty(annotation)
					? PackageHashType.Classic
					: FieldLoader.GetValue<PackageHashType>("(value)", annotation);

				return new MixFile(this, filename, type, order);
			}

			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order);
			if (filename.EndsWith(".RS", StringComparison.InvariantCultureIgnoreCase))
				return new D2kSoundResources(this, filename, order);
			if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldPackage(this, filename, order);
			if (filename.EndsWith(".PAK", StringComparison.InvariantCultureIgnoreCase))
				return new PakFile(this, filename, order);
			if (filename.EndsWith(".big", StringComparison.InvariantCultureIgnoreCase))
				return new BigFile(this, filename, order);
			if (filename.EndsWith(".bag", StringComparison.InvariantCultureIgnoreCase))
				return new BagFile(this, filename, order);
			if (filename.EndsWith(".hdr", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldCABExtractor(this, filename, order);

			return new Folder(filename, order);
		}

		public IReadWritePackage OpenWritablePackage(string filename, int order)
		{
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order);

			return new Folder(filename, order);
		}

		public void Mount(IReadOnlyPackage mount)
		{
			if (!MountedPackages.Contains(mount))
				MountedPackages.Add(mount);
		}

		public void Mount(string name, string annotation = null)
		{
			var optional = name.StartsWith("~");
			if (optional)
				name = name.Substring(1);

			name = Platform.ResolvePath(name);

			PackagePaths.Add(name);
			Action a = () => MountInner(OpenPackage(name, annotation, order++));

			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		void MountInner(IReadOnlyPackage package)
		{
			MountedPackages.Add(package);

			foreach (var hash in package.ClassicHashes())
			{
				var packageList = classicHashIndex[hash];
				if (!packageList.Contains(package))
					packageList.Add(package);
			}

			foreach (var hash in package.CrcHashes())
			{
				var packageList = crcHashIndex[hash];
				if (!packageList.Contains(package))
					packageList.Add(package);
			}
		}

		public bool Unmount(IReadOnlyPackage mount)
		{
			if (MountedPackages.Contains(mount))
				mount.Dispose();

			return MountedPackages.RemoveAll(f => f == mount) > 0;
		}

		public void UnmountAll()
		{
			foreach (var package in MountedPackages)
				package.Dispose();

			MountedPackages.Clear();
			PackagePaths.Clear();
			classicHashIndex = new Cache<uint, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());
			crcHashIndex = new Cache<uint, List<IReadOnlyPackage>>(_ => new List<IReadOnlyPackage>());
		}

		public void LoadFromManifest(Manifest manifest)
		{
			UnmountAll();
			foreach (var dir in manifest.Folders)
				Mount(dir);

			foreach (var pkg in manifest.Packages)
				Mount(pkg.Key, pkg.Value);
		}

		Stream GetFromCache(PackageHashType type, string filename)
		{
			var index = type == PackageHashType.CRC32 ? crcHashIndex : classicHashIndex;
			var package = index[PackageEntry.HashFilename(filename, type)]
				.Where(x => x.Exists(filename))
				.MinByOrDefault(x => x.Priority);

			if (package != null)
				return package.GetContent(filename);

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
				s = GetFromCache(PackageHashType.Classic, filename);
				if (s != null)
					return true;

				s = GetFromCache(PackageHashType.CRC32, filename);
				if (s != null)
					return true;
			}

			// Ask each package individually
			IReadOnlyPackage package;
			if (explicitPackage && !string.IsNullOrEmpty(packageName))
				package = MountedPackages.Where(x => x.Name == packageName).MaxByOrDefault(x => x.Priority);
			else
				package = MountedPackages.Where(x => x.Exists(filename)).MaxByOrDefault(x => x.Priority);

			if (package != null)
			{
				s = package.GetContent(filename);
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
				return MountedPackages.Where(n => n.Name == packageName).Any(f => f.Exists(filename));
			}
			else
				return MountedPackages.Any(f => f.Exists(name));
		}

		public static Assembly ResolveAssembly(object sender, ResolveEventArgs e)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				if (assembly.FullName == e.Name)
					return assembly;

			var frags = e.Name.Split(',');
			var filename = frags[0] + ".dll";

			Assembly a;
			if (AssemblyCache.TryGetValue(filename, out a))
				return a;

			if (Game.ModData.ModFiles.Exists(filename))
				using (var s = Game.ModData.ModFiles.Open(filename))
				{
					var buf = s.ReadBytes((int)s.Length);
					a = Assembly.Load(buf);
					AssemblyCache.Add(filename, a);
					return a;
				}

			return null;
		}
	}
}
