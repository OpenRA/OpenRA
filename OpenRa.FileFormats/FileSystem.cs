using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenRa.FileFormats;

namespace OpenRa.FileFormats
{
	public static class FileSystem
	{
		static List<IFolder> mountedFolders = new List<IFolder>();

		public static void Mount(IFolder folder)
		{
			mountedFolders.Add(folder);
		}

		public static Stream Open(string filename)
		{
			foreach( IFolder folder in mountedFolders )
			{
				Stream s = folder.GetContent(filename);
				if( s != null )
					return s;
			}

			throw new FileNotFoundException("File not found", filename);
		}
	}
}
