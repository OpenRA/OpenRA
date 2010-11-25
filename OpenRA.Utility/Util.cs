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
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;
using System.Collections.Generic;

namespace OpenRA.Utility
{
	static class Util
	{
		public static void ExtractFromPackage(string srcPath, string package, string[] files, string destPath)
		{
			if (!Directory.Exists(srcPath)) { Console.WriteLine("Error: Path {0} does not exist", srcPath); return; }
			if (!Directory.Exists(destPath)) { Console.WriteLine("Error: Path {0} does not exist", destPath); return; }

			FileSystem.Mount(srcPath);
			if (!FileSystem.Exists(package)) { Console.WriteLine("Error: Could not find {0}", package); return; }
			FileSystem.Mount(package);

			foreach (string s in files)
			{
				var destFile = "{0}{1}{2}".F(destPath, Path.DirectorySeparatorChar, s);
				using (var sourceStream = FileSystem.Open(s))
				using (var destStream = File.Create(destFile))
				{
					Console.WriteLine("Status: Extracting {0}", s);
					destStream.Write(sourceStream.ReadAllBytes());
				}
			}
		}

		public static void ExtractZip(this ZipInputStream z, string destPath, List<string> extracted)
		{
			ZipEntry entry;
			while ((entry = z.GetNextEntry()) != null)
			{
				if (!entry.IsFile) continue;

				Console.WriteLine("Status: Extracting {0}", entry.Name);
				if (!Directory.Exists(Path.Combine(destPath, Path.GetDirectoryName(entry.Name))))
					Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				var path = destPath + Path.DirectorySeparatorChar + entry.Name;
				extracted.Add(path);
				using (var f = File.Create(path))
				{
					int bufSize = 2048;
					byte[] buf = new byte[bufSize];
					while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
						f.Write(buf, 0, bufSize);
				}
			}
			z.Close();
		}
	}
}
