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
using System.Linq;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	static class Command
	{
		public static void ListMods(string _)
		{
			foreach (var m in Mod.AllMods)
				Console.WriteLine(m.Key);
		}

		public static void ListModInfo(string modList)
		{
			string[] mods = modList.Split(',');
			foreach (var m in mods)
			{
				var mod = Mod.AllMods
					.Where(x => x.Key.Equals(m))
					.Select(x => x.Value)
					.FirstOrDefault();
				if (mod == null)
				{
					Console.WriteLine("Error: Mod `{0}` is not installed or could not be found.", m);
					return;
				}

				Console.WriteLine("{0}:", m);
				Console.WriteLine("  Title: {0}", mod.Title);
				Console.WriteLine("  Version: {0}", mod.Version);
				Console.WriteLine("  Author: {0}", mod.Author);
				Console.WriteLine("  Description: {0}", mod.Description);
				Console.WriteLine("  Requires: {0}", mod.RequiresMods == null ? "" : string.Join(",", mod.RequiresMods));
				Console.WriteLine("  Standalone: {0}", mod.Standalone.ToString());
			}
		}

		public static void InstallRAMusic(string path)
		{
			Util.ExtractPackagesFromMix(path, string.Format("mods{0}ra{0}packages", Path.DirectorySeparatorChar),
								   "MAIN.MIX", "scores.mix");
			Console.WriteLine("Done");
		}

		public static void InstallCncMusic(string path)
		{
			if (!Directory.Exists(path)) { Console.WriteLine("Error: Path {0} does not exist", path); return; }
			string scoresMixPath = path + Path.DirectorySeparatorChar + "SCORES.MIX";
			if (!File.Exists(scoresMixPath)) { Console.WriteLine("Error: Could not find SCORES.MIX in path {0}", path); return; }

			File.Copy(scoresMixPath, string.Format("mods{0}cnc{0}packages{0}scores.mix", Path.DirectorySeparatorChar), true);

			Console.WriteLine("Done");
		}

		public static void DownloadPackages(string argValue)
		{
			string[] args = argValue.Split(',');
			string mod = "";
			string destPath = Path.GetTempPath();

			if (args.Length >= 1)
				mod = args[0];
			if (args.Length >= 2)
				destPath = args[1];

			string destFile = string.Format("{0}{1}{2}-packages.zip", destPath, Path.DirectorySeparatorChar, mod);

			if (File.Exists(destFile))
			{
				Console.WriteLine("Downloaded file already exists, using it instead.");
				Util.ExtractPackagesFromZip(mod, destPath);
				return;
			}

			WebClient wc = new WebClient();
			wc.DownloadProgressChanged += DownloadProgressChanged;
			wc.DownloadFileCompleted += DownloadFileCompleted;
			Console.WriteLine("Downloading {0}-packages.zip to {1}", mod, destPath);
			wc.DownloadFileAsync(
				new Uri(string.Format("http://open-ra.org/get-dependency.php?file={0}-packages", mod)),
				destFile,
				new string[] { mod, destPath });

			while (wc.IsBusy)
				Thread.Sleep(500);
		}

		static void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Console.WriteLine("Error: {0}", e.Error.Message);
				return;
			}

			Console.WriteLine("Download Completed");
			string[] modAndDest = (string[])e.UserState;
			Util.ExtractPackagesFromZip(modAndDest[0], modAndDest[1]);
		}

		static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Console.WriteLine("{0}% {1}/{2} bytes", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
		}

		public static void InstallRAPackages(string path)
		{
			Util.ExtractPackagesFromMix(path, "mods{0}ra{0}packages".F(Path.DirectorySeparatorChar), "MAIN.MIX",
								   "conquer.mix", "russian.mix", "allies.mix", "sounds.mix", "scores.mix",
								   "snow.mix", "interior.mix", "temperat.mix");
			var redalertMixPath = "{0}{1}INSTALL{1}REDALERT.MIX".F(path, Path.DirectorySeparatorChar);
			if (!File.Exists(redalertMixPath)) { Console.WriteLine("Error: REDALERT.MIX could not be found on the CD"); return; }
			Console.WriteLine("Copying REDALERT.MIX");
			File.Copy(redalertMixPath, "mods{0}ra{0}packages{0}redalert.mix".F(Path.DirectorySeparatorChar));
			Console.WriteLine("Done");
		}

		public static void InstallCncPackages(string path)
		{
			Console.WriteLine("Error: NotI");
		}

		public static void Settings(string argValue)
		{
			string[] args = argValue.Split(',');

			if (args.Length < 2) { return; }

			string settingsFile = args[0] + Path.DirectorySeparatorChar + "settings.yaml";
			List<MiniYamlNode> settingsYaml = MiniYaml.FromFile(settingsFile);
			Queue<String> settingKey = new Queue<string>(args[1].Split('.'));

			string s = settingKey.Dequeue();
			MiniYaml n = settingsYaml.Where(x => x.Key == s).Select(x => x.Value).FirstOrDefault();

			if (n == null)
			{
				Console.WriteLine("Error: Could not find {0} in {1}", args[1], settingsFile);
				return;
			}

			while (settingKey.Count > 0)
			{
				s = settingKey.Dequeue();
				if (!n.NodesDict.TryGetValue(s, out n))
				{
					Console.WriteLine("Error: Could not find {0} in {1}", args[1], settingsFile);
					return;
				}
			}

			Console.WriteLine(n.Value);
		}

		public static void InstallMod(string zipFile)
		{
			if (!File.Exists(zipFile)) { Console.WriteLine("Error: Could not find {0}", zipFile); return; }
			new ZipInputStream(File.OpenRead(zipFile)).ExtractZip("mods");
		}
	}
}
