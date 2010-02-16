using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace OpenRa.FileFormats
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
	}
}
