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
using System.Windows.Forms;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	static class Command
	{
		public static void ExtractZip(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}
			
			var zipFile = args[1];
			var dest = args[2];
			
			if (!File.Exists(zipFile))
			{
				Console.WriteLine("Error: Could not find {0}", zipFile);
				return;
			}
			
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
		
		public static void InstallRAPackages(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			var basePath = "{0}{1}".F(args[1], Path.DirectorySeparatorChar);
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

		public static void InstallCncPackages(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}
			
			var basePath = "{0}{1}".F(args[1], Path.DirectorySeparatorChar);
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
		
		public static void DisplayFilepicker(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}
			
			var dialog = new OpenFileDialog();
			dialog.Title = args[1];
			
			if (dialog.ShowDialog() == DialogResult.OK)
				Console.WriteLine(dialog.FileName);
		}

		public static void Settings(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}
			
			string expandedPath = args[1].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			
			string settingsFile = expandedPath + Path.DirectorySeparatorChar + "settings.yaml";
			if (!File.Exists(settingsFile))
			{
				Console.WriteLine("Error: Could not locate settings file at {0}", settingsFile);
				return;
			}
			
			List<MiniYamlNode> settingsYaml = MiniYaml.FromFile(settingsFile);
			Queue<String> settingKey = new Queue<string>(args[2].Split('.'));

			string s = settingKey.Dequeue();
			MiniYaml n = settingsYaml.Where(x => x.Key == s).Select(x => x.Value).FirstOrDefault();

			if (n == null)
			{
				Console.WriteLine("Error: Could not find {0} in {1}", args[2], settingsFile);
				return;
			}

			while (settingKey.Count > 0)
			{
				s = settingKey.Dequeue();
				if (!n.NodesDict.TryGetValue(s, out n))
				{
					Console.WriteLine("Error: Could not find {0} in {1}", args[2], settingsFile);
					return;
				}
			}

			Console.WriteLine(n.Value);
		}
	}
}
