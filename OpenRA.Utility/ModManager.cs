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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace OpenRA.Utility
{
	public static class ModManager
	{
		static bool Confirmation(string message, params object[] fmt)
		{
			Console.Write(message + " [y/n]: ", fmt);
			var input = Console.ReadLine().ToLowerInvariant()[0];
			return input == 'y';
		}

		[Desc("PATH NAME", "Copies file structure from PATH into mods/NAME/")]
		public static void InstallMod(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Incorrect syntax!");
				return;
			}

			var sourcePath = args[1];
			var modName = args[2];
			var destPath = "mods/{0}/".F(modName);

			if (!Directory.Exists(sourcePath))
			{
				Console.WriteLine("{0} could not be found.", sourcePath);
				return;
			}

			if (Directory.Exists(destPath) && !Confirmation("This mod ({0}) is already installed, overwrite?", modName))
				return;

			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);

			Console.Write("Copying files...");
			RecursiveFileStructureCopy(sourcePath, destPath);
			Console.WriteLine("\tcomplete!");
		}

		static void RecursiveFileStructureCopy(string source, string dest)
		{
			var sourceDirInfo = new DirectoryInfo(Path.GetFullPath(source));
			var sourceDirs = sourceDirInfo.GetDirectories();

			foreach (var dir in sourceDirs)
			{
				if (dir.Name == ".git")
					continue;

				var replace = dir.FullName.Replace(source, dest);
				if (!Directory.Exists(replace))
					Directory.CreateDirectory(replace);

				var sourceFilesInDirs = new DirectoryInfo(dir.FullName).GetFiles();

				foreach (var file in sourceFilesInDirs)
					File.Copy(file.FullName, file.FullName.Replace(source, dest), true);

				RecursiveFileStructureCopy(dir.FullName, replace);
			}

			var sourceFiles = sourceDirInfo.GetFiles();

			foreach (var file in sourceFiles)
				File.Copy(file.FullName, file.FullName.Replace(source, dest), true);
		}

		[Desc("MOD", "Deletes MOD and all of its files.")]
		public static void DeleteMod(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: No mod name given!");
				return;
			}

			var blacklist = new[] { "ra", "cnc", "d2k", "ts", "common", "modchooser" };

			var modName = args[1];
			if (blacklist.Contains(modName))
			{
				Console.WriteLine("{0} cannot be uninstalled. It is a protected mod.", modName);
				return;
			}

			var path = "mods/{0}/".F(modName);
			if (!Directory.Exists(path))
			{
				Console.WriteLine("{0} could not be found.", path);
				return;
			}

			if (Repository.IsValid(path))
				Console.WriteLine("Warning: This is a git mod!");

			if (!Confirmation("Are you sure you want to delete {0}?\nThis operation cannot be undone.", modName))
				return;

			var dirInfo = new DirectoryInfo(Path.GetFullPath(path));

			foreach (var dir in dirInfo.GetDirectories())
				Directory.Delete(dir.FullName, true);

			foreach (var file in dirInfo.GetFiles())
				File.Delete(file.FullName);

			Directory.Delete(path);

			Console.WriteLine("{0} uninstalled!", modName);
		}

		[Desc("MOD", "Update MOD via git remote branch 'origin/master'.")]
		public static void UpdateGitMod(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: No mod name given!");
				return;
			}

			var modName = args[1];
			var path = "mods/{0}/".F(modName);

			if (!Repository.IsValid(path))
			{
				Console.WriteLine("{0} is not a valid git mod.", path);
				return;
			}

			using (var repo = new Repository(path))
			{
				var origin = repo.Network.Remotes["origin"];
				repo.Network.Fetch(origin);
				repo.Reset(ResetMode.Hard, "origin/master");
			}

			Console.WriteLine("Update complete.");
		}

		[Desc("SOURCEPATH", "Install mod from SOURCEPATH via git.")]
		public static void InstallGitMod(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: No path given!");
				return;
			}

			var url = args[1];
			var splitOn = "oramod-";

			if (!url.Contains(splitOn))
			{
				Console.WriteLine("The given path does not contain `{0}`!", splitOn);
				return;
			}

			if (url.EndsWith(".git"))
				url = url.Substring(0, url.Length - 4);

			var modName = url.Split(new string[] {splitOn}, StringSplitOptions.None)[1];
			var modDirectory = "mods/{0}/".F(modName);

			if (Directory.Exists(modDirectory))
			{
				if (!Confirmation("This mod ({0}) is already installed, overwrite?", modName))
					return;

				Console.Write("Purging {0}...", modDirectory);
				var dirInfo = new DirectoryInfo(Path.GetFullPath(modDirectory));

				foreach (var dir in dirInfo.GetDirectories())
					Directory.Delete(dir.FullName, true);

				foreach (var file in dirInfo.GetFiles())
					File.Delete(file.FullName);

				Console.WriteLine("\tcomplete!");
			}

			Console.Write("Installing {0}...", modName);

			var path = Repository.Clone(url, modDirectory);
			using (var repo = new Repository(path))
				repo.Reset(ResetMode.Hard, "origin/master");

			Console.WriteLine("\tcomplete!");
		}
	}
}
