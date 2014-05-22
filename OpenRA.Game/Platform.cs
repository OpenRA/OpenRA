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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenRA
{
	public enum PlatformType { Unknown, Windows, OSX, Linux }

	public static class Platform
	{
		public static PlatformType CurrentPlatform { get { return currentPlatform.Value; } }

		static Lazy<PlatformType> currentPlatform = Exts.Lazy(GetCurrentPlatform);

		static PlatformType GetCurrentPlatform()
		{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					return PlatformType.Windows;

				try
				{
					var psi = new ProcessStartInfo("uname", "-s");
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = true;
					var p = Process.Start(psi);
					var kernelName = p.StandardOutput.ReadToEnd();
					if (kernelName.Contains("Linux") || kernelName.Contains("BSD"))
						return PlatformType.Linux;
					if (kernelName.Contains("Darwin"))
						return PlatformType.OSX;
				}
				catch {	}

				return PlatformType.Unknown;
		}

		public static string RuntimeVersion
		{
			get
			{
				var mono = Type.GetType("Mono.Runtime");
				if (mono == null)
					return ".NET CLR {0}".F(Environment.Version);

				var version = mono.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
				if (version == null)
					return "Mono (unknown version) CLR {0}".F(Environment.Version);

				return "Mono {0} CLR {1}".F(version.Invoke(null, null), Environment.Version); 
			}
		}

		public static void ShowFatalErrorDialog()
		{
			var process = "OpenRA.CrashDialog.exe";
			var args = "";

			if (CurrentPlatform == PlatformType.OSX)
			{
				// Winforms requires X11 under OSX, which may not exist.
				// Show a native dialog using applescript instead.
				var title = "Fatal Error";
				var logsButton = "View Logs";
				var faqButton = "View FAQ";
				var quitButton = "Quit";
				var message = "OpenRA has encountered a fatal error.\nRefer to the crash logs and FAQ for more information.";

				var faqPath = Game.Settings.Debug.FatalErrorDialogFaq;
				var logsPath = "file://" + Platform.GetFolderPath(UserFolder.Logs);

				var iconPath = new [] { "./OpenRA.icns", "./packaging/osx/template.app/Contents/Resources/OpenRA.icns" }.FirstOrDefault(f => File.Exists(f));
				var icon = iconPath != null ? "with icon alias (POSIX file \\\"file://{0}\\\")".F(Environment.CurrentDirectory + "/" + iconPath) : "";

				process = "/usr/bin/osascript";
				args = (
					"-e \"repeat\"\n" +
					"-e \"  tell application \\\"Finder\\\"\"\n" +
					"-e \"    set question to display dialog \\\"{1}\\\" with title \\\"{0}\\\" {2} buttons {{\\\"{3}\\\", \\\"{5}\\\", \\\"{7}\\\"}} default button 3\"\n" +
					"-e \"    if button returned of question is equal to \\\"{3}\\\" then open (POSIX file \\\"{4}\\\")\"\n" +
					"-e \"    if button returned of question is equal to \\\"{5}\\\" then open location \\\"{6}\\\"\"\n" +
					"-e \"    if button returned of question is equal to \\\"{7}\\\" then exit repeat\"\n" +
					"-e \"    activate\"\n" +
					"-e \"  end tell\"\n" +
					"-e \"end repeat\""
				).F(title, message, icon, logsButton, logsPath, faqButton, faqPath, quitButton);
			}

			if (CurrentPlatform == PlatformType.Linux)
				process = "error-dialog.sh";

			var psi = new ProcessStartInfo(process, args);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			Process.Start(psi);
		}

		#region Paths
		public static string SupportDir
		{
			get
			{
				// Use a local directory in the game root if it exists
				if (Directory.Exists("Support"))
					return "Support" + Path.DirectorySeparatorChar;

				var dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

				switch (CurrentPlatform)
				{
					case PlatformType.Windows:
						dir += Path.DirectorySeparatorChar + "OpenRA";
						break;
					case PlatformType.OSX:
						dir += "/Library/Application Support/OpenRA";
						break;
					case PlatformType.Linux:
					default:
						dir += "/.openra";
						break;
				}

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				return dir + Path.DirectorySeparatorChar;
			}
		}

		static ModMetadata GetCurrentModMetadata()
		{
			if (Game.CurrentModMetadata != null)
				return Game.CurrentModMetadata;

			throw new InvalidOperationException("No mod has been initialized yet.");
		}

		static PathSettings GetPathSettings()
		{
			if (Game.Settings != null && Game.Settings.Paths != null)
				return Game.Settings.Paths;

			return new PathSettings();
		}

		static string GetBasePath(PathSettings paths)
		{
			if (!UpgradeFileSystemDone)
			{
				UpgradeFileSystemDone = true;
				UpgradeFileSystem();
			}

			if (paths == null)
				throw new ArgumentNullException("paths");

			if (!string.IsNullOrWhiteSpace(paths.Base))
				return paths.Base.Trim();

			return Platform.SupportDir;
		}

		static string GetModPath(PathSettings paths)
		{
			if (paths == null)
				throw new ArgumentNullException("paths");

			if (!string.IsNullOrEmpty(paths.Mods) && paths.Mods[0] == '$')
				throw new InvalidDataException("The path for the mods cannot be resolved (it is not allowed to be relative to the current mod).");

			var modsPath = ResolvePath(paths.Mods, paths);

			return Path.Combine(modsPath, GetCurrentModMetadata().Id);
		}

		public static bool IsPathOptional(string path)
		{
			if (path == null)
				throw new ArgumentNullException("path");

			return path.Length > 0 && path[0] == '~';
		}

		// Translates arbitrary paths to filesystem paths.
		// Example: ^logs => "userdir/logs"
		//          @replays => "userdir/mods/mod/replays/version"
		//          $ModCache/news.yaml => "userdir/mods/mod/cache/news.yaml"
		public static string ResolvePath(string path, PathSettings paths = null)
		{
			if (path == null)
				throw new ArgumentNullException("path");

			if (paths == null)
				paths = GetPathSettings();

			path = path.Trim();

			// Ignore optional flag
			if (path.Length > 0 && path[0] == '~')
				path = path.Substring(1);

			if (path.Length == 0)
				return GetBasePath(paths);

			var prefix = path[0];

			// ^ : relative to the base path
			if (prefix == '^')
				return Path.Combine(GetBasePath(paths), path.Substring(1));

			// @ : relative to the current mod
			if (prefix == '@')
				return Path.Combine(GetModPath(paths), path.Substring(1));

			// $ : predefined folder
			if (prefix == '$')
			{
				path = path.Substring(1);
				var parts = path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, 2);
				var predefinedName = parts[0];
				var folder = Enum<UserFolder>.Parse(predefinedName);

				parts[0] = GetFolderPath(folder);
				return Path.Combine(parts);
			}

			return path;
		}

		public static string GetFolderPath(UserFolder folder, bool createFolder = false)
		{
			var paths = GetPathSettings();
			string path;

			switch (folder)
			{
				case UserFolder.Base:
					path = GetBasePath(paths);
					break;

				case UserFolder.Logs:
					path = ResolvePath(paths.Logs, paths);
					break;

				case UserFolder.ModContent:
					path = ResolvePath(paths.ModContent, paths);
					break;

				case UserFolder.ModReplays:
					path = Path.Combine(ResolvePath(paths.ModReplays, paths), GetCurrentModMetadata().Version);
					break;

				case UserFolder.ModMaps:
					path = ResolvePath(paths.ModMaps, paths);
					break;

				case UserFolder.ModCache:
					path = ResolvePath(paths.ModCache, paths);
					break;

				default:
					throw new ArgumentException("Unhandled " + folder.GetType().Name + " value.", "folder");
			}

			if (createFolder)
				Directory.CreateDirectory(path);

			return path;
		}

		public static string GetFilePath(UserFile file, bool createFolder = false)
		{
			var paths = GetPathSettings();
			string path;

			switch (file)
			{
				case UserFile.Motd:
					path = ResolvePath(paths.Motd, paths);
					break;

				case UserFile.ModMirrors:
					path = ResolvePath(paths.ModMirrors, paths);
					break;

				case UserFile.ModNews:
					path = ResolvePath(paths.ModNews, paths);
					break;

				default:
					throw new ArgumentException("Unhandled " + file.GetType().Name + " value.", "folder");
			}

			if (createFolder)
				Directory.CreateDirectory(Path.GetDirectoryName(path));

			return path;
		}

		static bool UpgradeFileSystemDone;
		static void UpgradeFileSystem()
		{
			if (!Directory.Exists(Path.Combine(SupportDir, "Content")))
				return;

			Directory.Move(Path.Combine(SupportDir, "Content"), Path.Combine(SupportDir, "old_content_dir"));

			var paths = GetPathSettings();
			var mods = new string[] { "ra", "cnc", "ts", "d2k" };

			try { Directory.Move(Path.Combine(SupportDir, "Logs"), GetFolderPath(UserFolder.Logs)); } catch {} 
			try
			{
				var modsDir = Directory.CreateDirectory(ResolvePath(paths.Mods));

				foreach (var mod in mods)
				{
					try
					{
						var modDir = modsDir.CreateSubdirectory(mod);
						try { Directory.Move(Path.Combine(SupportDir, "old_content_dir", mod), Path.Combine(modDir.FullName, "content")); } catch {} 
						try { Directory.Move(Path.Combine(SupportDir, "Cache", mod), Path.Combine(modDir.FullName, "cache")); } catch {} 
						try { Directory.Move(Path.Combine(SupportDir, "maps", mod), Path.Combine(modDir.FullName, "maps")); } catch {} 

						var repDir = new DirectoryInfo(Path.Combine(SupportDir, "Replays", mod));
						if (repDir.Exists)
						{
							foreach (var repsForModVer in repDir.EnumerateDirectories())
							{
								var newRepDir = modDir.CreateSubdirectory("replays");
								repsForModVer.MoveTo(Path.Combine(newRepDir.FullName, repsForModVer.Name));
							}
						}
					}
					catch {}
				}
			}
			catch {}
			try { Directory.Move(Path.Combine(SupportDir, "Replays"), Path.Combine(SupportDir, "old_replays_dir")); } catch {} 
			try { Directory.Move(Path.Combine(SupportDir, "Cache"), Path.Combine(SupportDir, "old_cache_dir")); } catch {} 
			try { Directory.Move(Path.Combine(SupportDir, "maps"), Path.Combine(SupportDir, "old_maps_dir")); } catch {} 
		}
		#endregion
	}

	public enum UserFolder
	{
		Base,
		Logs,
		ModContent,
		ModReplays,
		ModMaps,
		ModCache
	}

	public enum UserFile
	{
		Motd,
		ModMirrors,
		ModNews
	}
}
