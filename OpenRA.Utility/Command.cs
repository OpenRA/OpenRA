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
using System.Windows.Forms;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.GameRules;

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

		static void InstallPackages(string fromPath, string toPath,
			string[] filesToCopy, string[] filesToExtract, string packageToMount)
		{
			if (!Directory.Exists(toPath))
				Directory.CreateDirectory(toPath);

			Util.ExtractFromPackage(fromPath, packageToMount, filesToExtract, toPath);
			foreach (var file in filesToCopy)
			{
				if (!File.Exists(Path.Combine(fromPath, file)))
				{
					Console.WriteLine("Error: Could not find {0}", file);
					return;
				}

				Console.WriteLine("Status: Extracting {0}", file.ToLowerInvariant());
				File.Copy(
					Path.Combine(fromPath, file),
					Path.Combine(toPath, file.ToLowerInvariant()), true);
			}

			Console.WriteLine("Status: Completed");
		}
		
		public static void InstallRAPackages(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			InstallPackages(args[1], args[2],
				new string[] { "INSTALL/REDALERT.MIX" },
				new string[] { "conquer.mix", "russian.mix", "allies.mix", "sounds.mix",
					"scores.mix", "snow.mix", "interior.mix", "temperat.mix" },
				"MAIN.MIX");
		}

		public static void InstallCncPackages(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			InstallPackages(args[1], args[2],
				new string[] { "CONQUER.MIX", "DESERT.MIX", "GENERAL.MIX", "SCORES.MIX",
					"SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX" },
				new string[] { "cclocal.mix", "speech.mix", "tempicnh.mix", "updatec.mix" },
				"INSTALL/SETUP.Z");
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
			var section = args[2].Split('.')[0];
			var field = args[2].Split('.')[1];
			string expandedPath = args[1].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			var settings = new Settings(Path.Combine(expandedPath,"settings.yaml"), Arguments.Empty);
			var result = settings.Sections[section].GetType().GetField(field).GetValue(settings.Sections[section]);
			Console.WriteLine(result);
		}
		
		public static void AuthenticateAndExtractZip(string[] args)
		{			
			Util.CallWithAdmin("--extract-zip-inner \"{0}\" \"{1}\"".F(args[1], args[2]));
		}
		
		public static void AuthenticateAndInstallRAPackages(string[] args)
		{			
			Util.CallWithAdmin("--install-ra-packages-inner \"{0}\" \"{1}\"".F(args[1], args[2]));
		}
		
		public static void AuthenticateAndInstallCncPackages(string[] args)
		{			
			Util.CallWithAdmin("--install-cnc-packages-inner \"{0}\" \"{1}\"".F(args[1], args[2]));
		}
	}
}
