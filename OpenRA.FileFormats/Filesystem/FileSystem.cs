#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.FileFormats
{
	public static class FileSystem
	{
		static List<IFolder> MountedFolders = new List<IFolder>();

		static Cache<uint, List<IFolder>> allFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );

		public static List<string> FolderPaths = new List<string>();

		static void MountInner(IFolder folder)
		{
			MountedFolders.Add(folder);

			foreach (var hash in folder.AllFileHashes())
			{
				var l = allFiles[hash];
				if (!l.Contains(folder))
					l.Add(folder);
			}
		}

		static int order = 0;

		static IFolder OpenPackage(string filename)
		{
			return OpenPackage(filename, order++);
		}

		public static IFolder CreatePackage(string filename, int order, Dictionary<string, byte[]> content)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
				return new MixFile(filename, order, content);
			else if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order, content);
			else if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order, content);
			else if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				throw new NotImplementedException("Creating .Z archives is unsupported");
			else
				return new Folder(filename, order, content);
		}

		public static IFolder OpenPackage(string filename, int order)
		{
			if (filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
				return new MixFile(filename, order);
			else if (filename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order);
			else if (filename.EndsWith(".oramap", StringComparison.InvariantCultureIgnoreCase))
				return new ZipFile(filename, order);
			else if (filename.EndsWith(".Z", StringComparison.InvariantCultureIgnoreCase))
				return new InstallShieldPackage(filename, order);
			else
				return new Folder(filename, order);
		}

		public static void Mount(string name)
		{
			var optional = name.StartsWith("~");
			if (optional) name = name.Substring(1);

			// paths starting with ^ are relative to the support dir
			if (name.StartsWith("^"))
				name = Platform.SupportDir+name.Substring(1);

			if (Directory.Exists(name))
				FolderPaths.Add(name);

			var a = (Action)(() => FileSystem.MountInner(OpenPackage(name)));

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
			allFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );
		}

		public static bool Unmount(IFolder mount)
		{
			return (MountedFolders.RemoveAll(f => f == mount) > 0);
		}

		public static void Mount(IFolder mount)
		{
			if (!MountedFolders.Contains(mount)) MountedFolders.Add(mount);
		}

		public static void LoadFromManifest(Manifest manifest)
		{
			UnmountAll();
			foreach (var dir in manifest.Folders) Mount(dir);
			foreach (var pkg in manifest.Packages) Mount(pkg);
		}

		static Stream GetFromCache(Cache<uint, List<IFolder>> index, string filename)
		{
			var folder = index[PackageEntry.HashFilename(filename)]
				.Where(x => x.Exists(filename))
				.OrderBy(x => x.Priority)
				.FirstOrDefault();

			if (folder != null)
				return folder.GetContent(filename);

			return null;
		}

		public static Stream Open(string filename) { return OpenWithExts(filename, ""); }

		public static Stream OpenWithExts(string filename, params string[] exts)
		{
			if( filename.IndexOfAny( new char[] { '/', '\\' } ) == -1 )
			{
				foreach( var ext in exts )
				{
					var s = GetFromCache(allFiles, filename + ext);
					if (s != null)
						return s;
				}
			}

			foreach (var ext in exts)
			{
				var folder = MountedFolders
					.Where(x => x.Exists(filename + ext))
					.OrderByDescending(x => x.Priority)
					.FirstOrDefault();

				if (folder != null)
					return folder.GetContent(filename + ext);
			}

			throw new FileNotFoundException("File not found: {0}".F(filename), filename);
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

			if (FileSystem.Exists(filename))
				using (var s = FileSystem.Open(filename))
				{
					var buf = new byte[s.Length];
					s.Read(buf, 0, buf.Length);
					a = Assembly.Load(buf);
					assemblyCache.Add(filename, a);
					return a;
				}

			return null;
		}
	}
}
