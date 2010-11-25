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
using ICSharpCode.SharpZipLib;
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

				Console.WriteLine("Mod:{0}", m);
				Console.WriteLine("  Title: {0}", mod.Title);
				Console.WriteLine("  Version: {0}", mod.Version);
				Console.WriteLine("  Author: {0}", mod.Author);
				Console.WriteLine("  Description: {0}", mod.Description);
				Console.WriteLine("  Requires: {0}", mod.RequiresMods == null ? "" : string.Join(",", mod.RequiresMods));
				Console.WriteLine("  Standalone: {0}", mod.Standalone.ToString());
			}
		}

		public static void ExtractZip(string argValue)
		{
			string[] args = argValue.Split(',');
			
			if (args.Length != 2)
			{
				Console.WriteLine("Error: invalid syntax");
				return;
			}
			string zipFile = args[0];
			string path = args[1];
			
			if (!File.Exists(zipFile)) { Console.WriteLine("Error: Could not find {0}", zipFile); return; }
			string dest = "mods{0}{1}".F(Path.DirectorySeparatorChar,path);
			List<string> extracted = new List<string>();
			try
			{
				new ZipInputStream(File.OpenRead(zipFile)).ExtractZip(dest, extracted);
			}
			catch (SharpZipBaseException)
			{
				foreach(var f in extracted)
					File.Delete(f);
				Console.WriteLine("Error: Corrupted archive");
				return;
			}
			Console.WriteLine("Status: Completed");
		}
		
		public static void DownloadUrl(string argValue)
		{
			string[] args = argValue.Split(',');
			
			if (args.Length != 2)
			{
				Console.WriteLine("Error: invalid syntax");
				return;
			}
			string url = args[0];
			string path = args[1].Replace("~",Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			
			WebClient wc = new WebClient();
			wc.DownloadProgressChanged += DownloadProgressChanged;
			wc.DownloadFileCompleted += DownloadFileCompleted;

			Console.WriteLine("Downloading {0} to {1}", url, path);
			Console.WriteLine("Status: Initializing");
			
			wc.DownloadFileAsync(new Uri(url), path);
			while (!completed)
				Thread.Sleep(500);
		}

		static bool completed = false;

		static void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
				Console.WriteLine("Error: {0}", e.Error.Message);
			else
				Console.WriteLine("Status: Completed");
			completed = true;
		}

		static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Console.WriteLine("Status: {0}% {1}/{2} bytes", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
		}

		public static void InstallRAPackages(string path)
		{
			var basePath = "{0}{1}".F(path, Path.DirectorySeparatorChar);
			var toPath = "mods{0}ra{0}packages{0}".F(Path.DirectorySeparatorChar);
			var directCopy = new string[] {"INSTALL/REDALERT.MIX"};
			var extract = new string[] {"conquer.mix", "russian.mix", "allies.mix", "sounds.mix",
										"scores.mix", "snow.mix", "interior.mix", "temperat.mix"};
			if (!Directory.Exists(toPath))
				Directory.CreateDirectory(toPath);
			
			Util.ExtractFromPackage(basePath, "MAIN.MIX", extract, toPath);
			foreach (var file in directCopy)
			{
				if (!File.Exists(basePath+file))
				{
					Console.WriteLine("Error: Could not find "+file);
					return;
				}
				Console.WriteLine("Status: Extracting {0}", file);
				File.Copy(basePath+file,toPath+Path.GetFileName(file).ToLower(), true);
			}
			Console.WriteLine("Status: Completed");
		}

		public static void InstallCncPackages(string path)
		{
			var basePath = "{0}{1}".F(path, Path.DirectorySeparatorChar);
			var toPath = "mods{0}cnc{0}packages{0}".F(Path.DirectorySeparatorChar);
			var directCopy = new string[] {"CONQUER.MIX", "DESERT.MIX", "GENERAL.MIX", "SCORES.MIX",
											   "SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX"};
			var extract = new string[] {"cclocal.mix", "speech.mix", "tempicnh.mix", "updatec.mix"};
			
			if (!Directory.Exists(toPath))
				Directory.CreateDirectory(toPath);
			
			Util.ExtractFromPackage(basePath+"INSTALL", "SETUP.Z", extract, toPath);
			foreach (var file in directCopy)
			{
				if (!File.Exists(basePath+file))
				{
					Console.WriteLine("Error: Could not find "+file);
					return;
				}
				Console.WriteLine("Status: Extracting {0}", file);
				File.Copy(basePath+file,toPath+Path.GetFileName(file).ToLower(), true);
			}
			Console.WriteLine("Status: Completed");
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
	}
}
