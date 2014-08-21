#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public interface IFolder
	{
		Stream GetContent(string filename);
		bool Exists(string filename);
		IEnumerable<uint> ClassicHashes();
		IEnumerable<uint> CrcHashes();
		IEnumerable<string> AllFileNames();
		void Write(Dictionary<string, byte[]> contents);
		int Priority { get; }
		string Name { get; }
	}

	public static class GlobalFileSystem
	{
		public static List<IFolder> MountedFolders = new List<IFolder>();
		static Cache<uint, List<IFolder>> classicHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
		static Cache<uint, List<IFolder>> crcHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());

		public static List<string> FolderPaths = new List<string>();

		static void MountInner(IFolder folder)
		{
			MountedFolders.Add(folder);

			foreach (var hash in folder.ClassicHashes())
			{
				var l = classicHashIndex[hash];
				if (!l.Contains(folder))
					l.Add(folder);
			}

			foreach (var hash in folder.CrcHashes())
			{
				var l = crcHashIndex[hash];
				if (!l.Contains(folder))
					l.Add(folder);
			}
		}

		static int order = 0;

		public static IFolder CreatePackage(string filename, int order, Dictionary<string, byte[]> content)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
				return new MixFile(filename, order, content);
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order, content);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order, content);
			if (filename.EndsWith(".RS", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .RS archives is unimplemented");
			if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .Z archives is unimplemented");
			if (filename.EndsWith(".PAK", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .PAK archives is unimplemented");
			if (filename.EndsWith(".big", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("The creation of .big archives is unimplemented");

			return new Folder(filename, order, content);
		}

		public static IFolder OpenPackage(string filename, string annotation, int order)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
			{
				var type = string.IsNullOrEmpty(annotation)
					? PackageHashType.Classic
					: FieldLoader.GetValue<PackageHashType>("(value)", annotation);

				return new MixFile(filename, type, order);
			}
			if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order);
			if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order);
			if (filename.EndsWith(".RS", StringComparison.InvariantCultureIgnoreCase))
				return new D2kSoundResources(filename, order);
			if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldPackage(filename, order);
			if (filename.EndsWith(".PAK", StringComparison.InvariantCultureIgnoreCase))
				return new PakFile(filename, order);
			if (filename.EndsWith(".big", StringComparison.InvariantCultureIgnoreCase))
				return new BigFile(filename, order);

			return new Folder(filename, order);
		}

		public static void Mount(string name)
		{
			Mount(name, null);
		}

		public static void Mount(string name, string annotation)
		{
			var optional = name.StartsWith("~");
			if (optional)
				name = name.Substring(1);

			// paths starting with ^ are relative to the support dir
			if (name.StartsWith("^"))
				name = Platform.SupportDir + name.Substring(1);

			FolderPaths.Add(name);
			Action a = () => MountInner(OpenPackage(name, annotation, order++));

			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		public static void UnmountAll()
		{
			MountedFolders.Clear();
			FolderPaths.Clear();
			classicHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
			crcHashIndex = new Cache<uint, List<IFolder>>(_ => new List<IFolder>());
		}

		public static bool Unmount(IFolder mount)
		{
			return MountedFolders.RemoveAll(f => f == mount) > 0;
		}

		public static void Mount(IFolder mount)
		{
			if (!MountedFolders.Contains(mount)) MountedFolders.Add(mount);
		}

		public static void LoadFromManifest(Manifest manifest)
		{
			UnmountAll();
			foreach (var dir in manifest.Folders)
				Mount(dir);

			foreach (var pkg in manifest.Packages)
				Mount(pkg.Key, pkg.Value);
		}

		static Stream GetFromCache(PackageHashType type, string filename)
		{
			var index = type == PackageHashType.CRC32 ? crcHashIndex : classicHashIndex;
			var folder = index[PackageEntry.HashFilename(filename, type)]
				.Where(x => x.Exists(filename))
				.MinByOrDefault(x => x.Priority);

			if (folder != null)
				return folder.GetContent(filename);

			return null;
		}

		public static Stream Open(string filename) { return OpenWithExts(filename, ""); }

		public static Stream OpenWithExts(string filename, params string[] exts)
		{
			Stream s;
			if (!TryOpenWithExts(filename, exts, out s))
				throw new FileNotFoundException("File not found: {0}".F(filename), filename);

			return s;
		}

		public static bool TryOpenWithExts(string filename, string[] exts, out Stream s)
		{
			if (filename.IndexOfAny(new char[] { '/', '\\' }) == -1)
			{
				foreach (var ext in exts)
				{
					s = GetFromCache(PackageHashType.Classic, filename + ext);
					if (s != null)
						return true;

					s = GetFromCache(PackageHashType.CRC32, filename + ext);
					if (s != null)
						return true;
				}
			}

			foreach (var ext in exts)
			{
				var folder = MountedFolders
					.Where(x => x.Exists(filename + ext))
					.MaxByOrDefault(x => x.Priority);

				if (folder != null)
				{
					s = folder.GetContent(filename + ext);
					return true;
				}
			}

			s = null;
			return false;
		}

		public static bool Exists(string filename) { return MountedFolders.Any(f => f.Exists(filename)); }

		static Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();

		public static Assembly ResolveAssembly(object sender, ResolveEventArgs e)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				if (assembly.FullName == e.Name)
					return assembly;

			var frags = e.Name.Split(',');
			var filename = frags[0] + ".dll";

			Assembly a;
			if (assemblyCache.TryGetValue(filename, out a))
				return a;

			if (Exists(filename))
				using (var s = Open(filename))
				{
					var buf = s.ReadBytes((int)s.Length);
					a = Assembly.Load(buf);
					assemblyCache.Add(filename, a);
					return a;
				}

			return null;
		}
	}
}
