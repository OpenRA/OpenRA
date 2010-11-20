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
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class ZipFile : IFolder
	{
		readonly SZipFile pkg;
		int priority;

		public ZipFile(string filename, int priority)
		{
			this.priority = priority;
			pkg = new SZipFile(File.OpenRead(filename));
		}

		public Stream GetContent(string filename)
		{
			return pkg.GetInputStream(pkg.GetEntry(filename));
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach(ZipEntry entry in pkg)
				yield return PackageEntry.HashFilename(entry.Name);
		}
		
		public bool Exists(string filename)
		{
			return pkg.GetEntry(filename) != null;
		}

		public int Priority
		{
			get { return 500 + priority; }
		}
	}
}
