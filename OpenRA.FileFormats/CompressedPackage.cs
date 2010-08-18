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
using System.IO.Packaging;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class CompressedPackage : IFolder
	{
		readonly uint[] hashes;
		readonly Stream s;
		readonly ZipPackage pkg;

		public CompressedPackage(string filename)
		{
			s = FileSystem.Open(filename);
			pkg = (ZipPackage)ZipPackage.Open(s, FileMode.Open);
			hashes = pkg.GetParts()
				.Select(p => PackageEntry.HashFilename(p.Uri.LocalPath)).ToArray();
		}

		public Stream GetContent(string filename)
		{
			return pkg.GetPart(new Uri(filename)).GetStream(FileMode.Open);
		}

		public IEnumerable<uint> AllFileHashes() { return hashes; }
		
		public bool Exists(string filename)
		{
			return hashes.Contains(PackageEntry.HashFilename(filename));
		}
	}
}
