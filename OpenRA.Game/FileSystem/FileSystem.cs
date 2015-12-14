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
		public readonly List<string> FolderPaths = new List<string>();
		public readonly List<IFolder> MountedFolders = new List<IFolder>();

		static readonly Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

		int order;
		Cache<uint, List<IFolder>> crcHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
		Cache<uint, List<IFolder>> classicHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());

		public IFolder CreatePackage(string filename, int order, Dictionary<string, byte[]> content)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
				return new MixFile(this, filename, order, content);
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order, content);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(this, filename, order, content);
			if (filename.EndsWith(".RS", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .RS archives is unimplemented");
			if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .Z archives is unimplemented");
			if (filename.EndsWith(".PAK", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .PAK archives is unimplemented");
			if (filename.EndsWith(".big", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .big archives is unimplemented");
			if (filename.EndsWith(".cab", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .cab archives is unimplemented");

			return new Folder(filename, order, content);
		}

		public IFolder OpenPackage(string filename, string annotation, int order)
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

		public void Mount(IFolder mount)
		{
			if (!MountedFolders.Contains(mount))
				MountedFolders.Add(mount);
		}

		public void Mount(string name, string annotation = null)
		{
			var optional = name.StartsWith("~");
			if (optional)
				name = name.Substring(1);

			name = Platform.ResolvePath(name);

			FolderPaths.Add(name);
			Action a = () => MountInner(OpenPackage(name, annotation, order++));

			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		void MountInner(IFolder folder)
		{
			MountedFolders.Add(folder);

			foreach (var hash in folder.ClassicHashes())
			{
				var folderList = classicHashIndex[hash];
				if (!folderList.Contains(folder))
					folderList.Add(folder);
			}

			foreach (var hash in folder.CrcHashes())
			{
				var folderList = crcHashIndex[hash];
				if (!folderList.Contains(folder))
					folderList.Add(folder);
			}
		}

		public bool Unmount(IFolder mount)
		{
			if (MountedFolders.Contains(mount))
				mount.Dispose();

			return MountedFolders.RemoveAll(f => f == mount) > 0;
		}

		public void UnmountAll()
		{
			foreach (var folder in MountedFolders)
				folder.Dispose();

			MountedFolders.Clear();
			FolderPaths.Clear();
			classicHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
			crcHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
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
			var folder = index[PackageEntry.HashFilename(filename, type)]
				.Where(x => x.Exists(filename))
				.MinByOrDefault(x => x.Priority);

			if (folder != null)
				return folder.GetContent(filename);

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
			var foldername = string.Empty;

			// Used for faction specific packages; rule out false positive on Windows C:\ drive notation
			var explicitFolder = name.Contains(':') && !Directory.Exists(Path.GetDirectoryName(name));
			if (explicitFolder)
			{
				var divide = name.Split(':');
				foldername = divide.First();
				filename = divide.Last();
			}

			// Check the cache for a quick lookup if the folder name is unknown
			// TODO: This disables caching for explicit folder requests
			if (filename.IndexOfAny(new char[] { '/', '\\' }) == -1 && !explicitFolder)
			{
				s = GetFromCache(PackageHashType.Classic, filename);
				if (s != null)
					return true;

				s = GetFromCache(PackageHashType.CRC32, filename);
				if (s != null)
					return true;
			}

			// Ask each package individually
			IFolder folder;
			if (explicitFolder && !string.IsNullOrEmpty(foldername))
				folder = MountedFolders.Where(x => x.Name == foldername).MaxByOrDefault(x => x.Priority);
			else
				folder = MountedFolders.Where(x => x.Exists(filename)).MaxByOrDefault(x => x.Priority);

			if (folder != null)
			{
				s = folder.GetContent(filename);
				return true;
			}

			s = null;
			return false;
		}

		public bool Exists(string name)
		{
			var explicitFolder = name.Contains(':') && !Directory.Exists(Path.GetDirectoryName(name));
			if (explicitFolder)
			{
				var divide = name.Split(':');
				var foldername = divide.First();
				var filename = divide.Last();
				return MountedFolders.Where(n => n.Name == foldername).Any(f => f.Exists(filename));
			}
			else
				return MountedFolders.Any(f => f.Exists(name));
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
