#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

namespace OpenRA.FileFormats
{
	public static class InstallUtils
	{
		static IEnumerable<ZipEntry> GetEntries(this ZipInputStream z)
		{
			for (; ; )
			{
				var e = z.GetNextEntry();
				if (e != null) yield return e; else break;
			}
		}
			
		public static void ExtractZip(this ZipInputStream z, string destPath, List<string> extracted, Action<string> Extracting)
		{
			foreach (var entry in z.GetEntries())
			{
				if (!entry.IsFile) continue;
				
				Extracting(entry.Name);
				Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				var path = Path.Combine(destPath, entry.Name);
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
		
		// TODO: The package should be mounted into its own context to avoid name collisions with installed files
		public static bool ExtractFromPackage(string srcPath, string package, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{
			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);
			
			if (!Directory.Exists(srcPath)) { onError("Cannot find "+package); return false; }
			FileSystem.Mount(srcPath);
			if (!FileSystem.Exists(package)) { onError("Cannot find "+package); return false; }
			FileSystem.Mount(package);

			foreach (string s in files)
			{
				var destFile = Path.Combine(destPath, s);
				using (var sourceStream = FileSystem.Open(s))
				using (var destStream = File.Create(destFile))
				{
					onProgress("Extracting "+s);
					destStream.Write(sourceStream.ReadAllBytes());
				}
			}
			
			onProgress("Extraction complete");
			return true;
		}
		
		public static bool CopyFiles(string srcPath, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{
			foreach (var file in files)
			{
				var fromPath = Path.Combine(srcPath, file);
				if (!File.Exists(fromPath))
				{
					onError("Cannot find "+file);
					return false;
				}
				var destFile = Path.GetFileName(file).ToLowerInvariant();
				onProgress("Extracting "+destFile);
				File.Copy(fromPath,	Path.Combine(destPath, destFile), true);
			}
			onProgress("Extraction complete");
			return true;
		}
	}
}