using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRa.FileFormats
{
	public static class FileSystem
	{
		static List<IFolder> mountedFolders = new List<IFolder>();
		static List<IFolder> temporaryMounts = new List<IFolder>();

		public static void MountDefault( bool useAftermath )
		{
			FileSystem.Mount( new Folder( "./" ) );
			if( File.Exists( "main.mix" ) )
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
			if( useAftermath )
			{
				FileSystem.Mount( new Package( "expand2.mix" ) );
				FileSystem.Mount( new Package( "hires1.mix" ) );
			}
		}

		public static void Mount(IFolder folder)
		{
			mountedFolders.Add(folder);
		}

		public static void MountTemporary(IFolder folder)
		{
			mountedFolders.Add(folder);
			temporaryMounts.Add(folder);
		}

		public static void UnmountTemporaryPackages()
		{
			mountedFolders.RemoveAll(f => temporaryMounts.Contains(f));
			temporaryMounts.Clear();
		}

		public static Stream Open(string filename)
		{
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
