using System.Collections.Generic;
using System.IO;
using System.Linq;
using IjwFramework.Collections;

namespace OpenRa.FileFormats
{
	public static class FileSystem
	{
		static List<IFolder> mountedFolders = new List<IFolder>();
		static List<IFolder> temporaryMounts = new List<IFolder>();

		static Cache<uint, List<IFolder>> allFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );
		static Cache<uint, List<IFolder>> allTemporaryFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );

		public static void MountDefaultPackages()
		{
			FileSystem.Mount(new Folder("./"));
			if( FileSystem.Exists( "main.mix" ) )
				FileSystem.Mount( new Package( "main.mix" ) );
			FileSystem.Mount( new Package( "redalert.mix" ) );
			FileSystem.Mount( new Package( "conquer.mix" ) );
			FileSystem.Mount( new Package( "hires.mix" ) );
			FileSystem.Mount( new Package( "general.mix" ) );
			FileSystem.Mount( new Package( "local.mix" ) );
			FileSystem.Mount( new Package( "sounds.mix" ) );
			FileSystem.Mount( new Package( "speech.mix" ) );
			FileSystem.Mount( new Package( "allies.mix" ) );
			FileSystem.Mount( new Package( "russian.mix" ) );
		}
		
		public static void MountAftermathPackages()
		{
			FileSystem.Mount( new Package( "expand2.mix" ) );
			FileSystem.Mount( new Package( "hires1.mix" ) );
		}

		public static void Mount(IFolder folder)
		{
			mountedFolders.Add(folder);

			foreach( var hash in folder.AllFileHashes() )
			{
				var l = allFiles[hash];
				if( !l.Contains( folder ) )
					l.Add( folder );
			}
		}

		public static void MountTemporary(IFolder folder)
		{
			mountedFolders.Add(folder);
			temporaryMounts.Add(folder);

			foreach( var hash in folder.AllFileHashes() )
			{
				var l = allTemporaryFiles[hash];
				if( !l.Contains( folder ) )
					l.Add( folder );
			}
		}

		public static void UnmountTemporaryPackages()
		{
			mountedFolders.RemoveAll(f => temporaryMounts.Contains(f));
			temporaryMounts.Clear();

			allTemporaryFiles = new Cache<uint, List<IFolder>>( _ => new List<IFolder>() );
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
				var ret = GetFromCache( allFiles, filename )
					?? GetFromCache( allTemporaryFiles, filename );
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
					var s = GetFromCache( allFiles, filename + ext )
						?? GetFromCache( allTemporaryFiles, filename + ext );
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
