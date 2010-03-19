#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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

		public static void Mount(string name)
		{
			name = name.ToLowerInvariant();
			var optional = name.StartsWith("~");
			if (optional) name = name.Substring(1);

			var a = name.EndsWith(".mix")
				? (Action)(() => FileSystem.MountInner(new Package(name)))
				: () => FileSystem.MountInner(new Folder(name));

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

		static Stream GetFromCache( Cache<uint, List<IFolder>> index, string filename )
		{
			foreach( var folder in index[ PackageEntry.HashFilename( filename ) ] )
			{
				Stream s = folder.GetContent(filename);
				if( s != null )
					return s;
			}
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
				Stream s = folder.GetContent(filename);
				if( s != null )
					return s;
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
				{
					Stream s = folder.GetContent( filename + ext );
					if( s != null )
						return s;
				}
			}

			throw new FileNotFoundException( string.Format( "File not found: {0}", filename ), filename );
		}

		public static bool Exists(string filename)
		{
			foreach (var folder in mountedFolders)
			{
				var s = folder.GetContent(filename);
				if (s != null)
				{
					s.Dispose();
					return true;
				}
			}

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
