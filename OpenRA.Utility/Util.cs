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

namespace OpenRA.Utility
{
	static class Util
	{
		public static void ExtractPackagesFromMix(string srcPath, string destPath, string mix, params string[] packages)
		{
			if (!Directory.Exists(srcPath)) { Console.WriteLine("Error: Path {0} does not exist", srcPath); return; }
			FileSystem.Mount(srcPath);
			if (!FileSystem.Exists(mix)) { Console.WriteLine("Error: Could not find {1} in path {0}", srcPath, mix); return; }
			FileSystem.Mount(mix);

			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);

			foreach (string s in packages)
			{
				var destFile = "{0}{1}{2}".F(destPath, Path.DirectorySeparatorChar, s);
				using (var sourceStream = FileSystem.Open(s))
				using (var destStream = File.Create(destFile))
				{
					Console.WriteLine("Extracting {0}", s);
					destStream.Write(sourceStream.ReadAllBytes());
				}
			}
		}

		public static void ExtractPackagesFromZip(string mod, string dest)
		{
			string filepath = string.Format("{0}{1}{2}-packages.zip", dest, Path.DirectorySeparatorChar, mod);
			string modPackageDir = string.Format("mods{0}{1}{0}packages{0}", Path.DirectorySeparatorChar, mod);

			if (!Directory.Exists(modPackageDir))
				Directory.CreateDirectory(modPackageDir);

			new ZipInputStream(File.OpenRead(filepath)).ExtractZip(modPackageDir);
			

			Console.WriteLine("Done");
		}

		public static void ExtractZip(this ZipInputStream z, string destPath)
		{
			ZipEntry entry;
			while ((entry = z.GetNextEntry()) != null)
			{
				if (!entry.IsFile) continue;

				Console.WriteLine("Extracting {0}", entry.Name);
				if (!Directory.Exists(Path.Combine(destPath, Path.GetDirectoryName(entry.Name))))
					Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				using (var f = File.Create(destPath + Path.DirectorySeparatorChar + entry.Name))
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
