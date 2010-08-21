#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OpenRA.FileFormats
{
	public static class FileSystem
	{
		static List<IFolder> mountedFolders = new List<IFolder>();

		static Cache<uint, List<IFolder>> allFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );

		static void MountInner(IFolder folder)
		{
			mountedFolders.Add(folder);

			foreach( var hash in folder.AllFileHashes() )
			{
				var l = allFiles[hash];
				if( !l.Contains( folder ) )
					l.Add( folder );
			}
		}

		static IFolder OpenPackage(string filename)
		{
			if (filename.EndsWith(".mix"))
				return new Package(filename);
			else if (filename.EndsWith(".zip"))
				return new CompressedPackage(filename);
			else
				return new Folder(filename);
		}

		public static void Mount(string name)
		{
			name = name.ToLowerInvariant();
			var optional = name.StartsWith("~");
			if (optional) name = name.Substring(1);

			var a = (Action)(() => FileSystem.MountInner(OpenPackage(name)));

			if (optional)
				try { a(); }
				catch { }
			else
				a();
		}

		public static void UnmountAll()
		{
			mountedFolders.Clear();
			allFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );
		}

		public static void LoadFromManifest( Manifest manifest )
		{
			UnmountAll();
			foreach (var dir in manifest.Folders) Mount(dir);
			foreach (var pkg in manifest.Packages) Mount(pkg);
		}

		static Stream GetFromCache( Cache<uint, List<IFolder>> index, string filename )
		{
			foreach( var folder in index[ PackageEntry.HashFilename( filename ) ] )
				if (folder.Exists(filename))
					return folder.GetContent(filename);
			return null;
		}

		public static Stream Open(string filename)
		{
			if( filename.IndexOfAny( new char[] { '/', '\\' } ) == -1 )
			{
				var ret = GetFromCache( allFiles, filename );
				if( ret != null )
					return ret;
			}

			foreach( IFolder folder in mountedFolders )
			{
				if (folder.Exists(filename))
					return folder.GetContent(filename);
			}

			throw new FileNotFoundException( string.Format( "File not found: {0}", filename ), filename );
		}

		public static Stream OpenWithExts( string filename, params string[] exts )
		{
			if( filename.IndexOfAny( new char[] { '/', '\\' } ) == -1 )
			{
				foreach( var ext in exts )
				{
					var s = GetFromCache( allFiles, filename + ext );
					if( s != null )
						return s;
				}
			}

			foreach( var ext in exts )
			{
				foreach( IFolder folder in mountedFolders )
					if (folder.Exists(filename + ext))
						return folder.GetContent( filename + ext );
			}

			throw new FileNotFoundException( string.Format( "File not found: {0}", filename ), filename );
		}

		public static bool Exists(string filename)
		{
			foreach (var folder in mountedFolders)
				if (folder.Exists(filename))
				    return true;
			return false;
		}

		static Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();

		public static Assembly ResolveAssembly(object sender, ResolveEventArgs e)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.FullName == e.Name)
					return assembly;
			}

			string[] frags = e.Name.Split(',');
			var filename = frags[0] + ".dll";
			Assembly a;
			if (assemblyCache.TryGetValue(filename, out a))
				return a;

			if (FileSystem.Exists(filename))
				using (Stream s = FileSystem.Open(filename))
				{
					byte[] buf = new byte[s.Length];
					s.Read(buf, 0, buf.Length);
					a = Assembly.Load(buf);
					assemblyCache.Add(filename, a);
					return a;
				}
			
			return null;
		}
	}
}
