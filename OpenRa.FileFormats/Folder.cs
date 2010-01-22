using System.IO;
using System.Collections.Generic;

namespace OpenRa.FileFormats
{
	public class Folder : IFolder
	{
		readonly string path;

		public Folder(string path) { this.path = path; }

		public Stream GetContent(string filename)
		{
			Log.Write( "GetContent from folder: {0}", filename );
			try { return File.OpenRead( Path.Combine( path, filename ) ); }
			catch { return null; }
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach( var filename in Directory.GetFiles( path, "*", SearchOption.TopDirectoryOnly ) )
				yield return PackageEntry.HashFilename( filename );
		}
	}
}
