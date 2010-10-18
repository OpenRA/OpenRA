#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Folder : IFolder
	{
		readonly string path;

		public Folder(string path) { this.path = path; }

		public Stream GetContent(string filename)
		{
			try { return File.OpenRead( Path.Combine( path, filename ) ); }
			catch { return null; }
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach( var filename in Directory.GetFiles( path, "*", SearchOption.TopDirectoryOnly ) )
				yield return PackageEntry.HashFilename( Path.GetFileName(filename) );
		}
		
		public bool Exists(string filename)
		{
			return File.Exists(Path.Combine(path,filename));
		}


		public int Priority
		{
			get { return 100; }
		}
	}
}
