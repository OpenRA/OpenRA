#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileSystem;

namespace OpenRA
{
	public class InstallData
	{
		public readonly string InstallerMenuWidget = null;
		public readonly string InstallerBackgroundWidget = null;
		public readonly string[] TestFiles = {};
		public readonly string[] DiskTestFiles = {};
		public readonly string PackageToExtractFromCD = null;
		public readonly string[] ExtractFilesFromCD = {};
		public readonly string[] CopyFilesFromCD = {};

		public readonly string PackageMirrorList = null;

		public readonly string MusicPackageMirrorList = null;
		public readonly int ShippedSoundtracks = 0;
	}

	public static class InstallUtils
	{
		static IEnumerable<ZipEntry> GetEntries(this ZipInputStream z)
		{
			for (;;)
			{
				var e = z.GetNextEntry();
				if (e != null) yield return e; else break;
			}
		}

		public static string GetMountedDisk(Func<string, bool> isValidDisk)
		{
			var volumes = DriveInfo.GetDrives()
				.Where(v => v.DriveType == DriveType.CDRom && v.IsReady)
				.Select(v => v.RootDirectory.FullName);

			return volumes.FirstOrDefault(v => isValidDisk(v));
		}

		// TODO: The package should be mounted into its own context to avoid name collisions with installed files
		public static bool ExtractFromPackage(string srcPath, string package, string annotation, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{
			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);

			Log.Write("debug", "Mounting {0}".F(srcPath));
			GlobalFileSystem.Mount(srcPath);
			Log.Write("debug", "Mounting {0}".F(package));
			GlobalFileSystem.Mount(package, annotation);

			foreach (var file in files)
			{
				var dest = Path.Combine(destPath, file);
				if (File.Exists(dest))
					File.Delete(dest);
				using (var sourceStream = GlobalFileSystem.Open(file))
				using (var destStream = File.Create(dest))
				{
					Log.Write("debug", "Extracting {0} to {1}".F(file, dest));
					onProgress("Extracting " + file);
					destStream.Write(sourceStream.ReadAllBytes());
				}
			}

			return true;
		}

		public static bool CopyFiles(string srcPath, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{
			foreach (var file in files)
			{
				var fromPath = Path.Combine(srcPath, file);
				if (!File.Exists(fromPath))
				{
					onError("Cannot find " + file);
					return false;
				}

				var destFile = Path.GetFileName(file).ToLowerInvariant();
				var dest = Path.Combine(destPath, destFile);
				if (File.Exists(dest))
					File.Delete(dest);
				onProgress("Copying " + destFile);
				Log.Write("debug", "Copy {0} to {1}".F(fromPath, dest));
				File.Copy(fromPath, dest, true);
			}

			return true;
		}

		public static bool ExtractZip(string zipFile, string dest, Action<string> onProgress, Action<string> onError)
		{
			if (!File.Exists(zipFile))
			{
				onError("Invalid path: " + zipFile);
				return false;
			}

			var extracted = new List<string>();
			try
			{
				using (var stream = File.OpenRead(zipFile))
				using (var z = new ZipInputStream(stream))
					z.ExtractZip(dest, extracted, s => onProgress("Extracting " + s));
			}
			catch (SharpZipBaseException)
			{
				foreach (var f in extracted)
					File.Delete(f);

				onError("Invalid archive");
				return false;
			}

			return true;
		}

		// TODO: this belongs in FileSystem/ZipFile
		static void ExtractZip(this ZipInputStream z, string destPath, List<string> extracted, Action<string> onProgress)
		{
			foreach (var entry in z.GetEntries())
			{
				if (!entry.IsFile) continue;

				onProgress(entry.Name);

				Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				var path = Path.Combine(destPath, entry.Name);
				extracted.Add(path);

				using (var f = File.Create(path))
					z.CopyTo(f);
			}

			z.Close();
		}
	}
}