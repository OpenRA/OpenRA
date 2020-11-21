#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace OpenRA
{
	public enum PlatformType { Unknown, Windows, OSX, Linux }

	public enum SupportDirType { System, ModernUser, LegacyUser, User }

	public static class Platform
	{
		public static PlatformType CurrentPlatform { get { return currentPlatform.Value; } }
		public static readonly Guid SessionGUID = Guid.NewGuid();

		static Lazy<PlatformType> currentPlatform = Exts.Lazy(GetCurrentPlatform);

		static bool engineDirAccessed;
		static string engineDir;

		static bool supportDirInitialized;
		static string systemSupportPath;
		static string legacyUserSupportPath;
		static string modernUserSupportPath;
		static string userSupportPath;

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
				if (kernelName.Contains("Darwin"))
					return PlatformType.OSX;

				return PlatformType.Linux;
			}
			catch { }

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

		/// <summary>
		/// Directory containing user-specific support files (settings, maps, replays, game data, etc).
		/// </summary>
		public static string SupportDir { get { return GetSupportDir(SupportDirType.User); } }

		public static string GetSupportDir(SupportDirType type)
		{
			if (!supportDirInitialized)
				InitializeSupportDir();

			switch (type)
			{
				case SupportDirType.System: return systemSupportPath;
				case SupportDirType.LegacyUser: return legacyUserSupportPath;
				case SupportDirType.ModernUser: return modernUserSupportPath;
				default: return userSupportPath;
			}
		}

		static void InitializeSupportDir()
		{
			// The preferred support dir location for Windows and Linux was changed in mid 2019 to match modern platform conventions
			switch (CurrentPlatform)
			{
				case PlatformType.Windows:
				{
					modernUserSupportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRA");
					legacyUserSupportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "OpenRA");
					systemSupportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OpenRA") + Path.DirectorySeparatorChar;
					break;
				}

				case PlatformType.OSX:
				{
					modernUserSupportPath = legacyUserSupportPath = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.Personal),
						"Library", "Application Support", "OpenRA");

					systemSupportPath = "/Library/Application Support/OpenRA/";
					break;
				}

				case PlatformType.Linux:
				{
					legacyUserSupportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".openra");

					var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
					if (string.IsNullOrEmpty(xdgConfigHome))
						xdgConfigHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");

					modernUserSupportPath = Path.Combine(xdgConfigHome, "openra");
					systemSupportPath = "/var/games/openra/";

					break;
				}

				default:
				{
					modernUserSupportPath = legacyUserSupportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".openra");
					systemSupportPath = "/var/games/openra/";
					break;
				}
			}

			// Use a local directory in the game root if it exists (shared with the system support dir)
			var localSupportDir = Path.Combine(EngineDir, "Support");
			if (Directory.Exists(localSupportDir))
				userSupportPath = systemSupportPath = localSupportDir + Path.DirectorySeparatorChar;

			// Use the fallback directory if it exists and the preferred one does not
			else if (!Directory.Exists(modernUserSupportPath) && Directory.Exists(legacyUserSupportPath))
				userSupportPath = legacyUserSupportPath + Path.DirectorySeparatorChar;
			else
				userSupportPath = modernUserSupportPath + Path.DirectorySeparatorChar;

			supportDirInitialized = true;
		}

		/// <summary>
		/// Specify a custom support directory that already exists on the filesystem.
		/// Cannot be called after Platform.SupportDir / GetSupportDir have been accessed.
		/// </summary>
		public static void OverrideSupportDir(string path)
		{
			if (supportDirInitialized)
				throw new InvalidOperationException("Attempted to override user support directory after it has already been accessed.");

			if (!Directory.Exists(path))
				throw new DirectoryNotFoundException(path);

			if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
					!path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				path += Path.DirectorySeparatorChar;

			InitializeSupportDir();
			userSupportPath = path;
		}

		public static string EngineDir
		{
			get
			{
				// Engine directory defaults to the location of the binaries,
				// unless OverrideGameDir is called during startup.
				if (!engineDirAccessed)
					engineDir = BinDir;

				engineDirAccessed = true;
				return engineDir;
			}
		}

		/// <summary>
		/// Specify a custom engine directory that already exists on the filesystem.
		/// Cannot be called after Platform.EngineDir has been accessed.
		/// </summary>
		public static void OverrideEngineDir(string path)
		{
			if (engineDirAccessed)
				throw new InvalidOperationException("Attempted to override engine directory after it has already been accessed.");

			// Note: Relative paths are interpreted as being relative to BinDir, not the current working dir.
			if (!Path.IsPathRooted(path))
				path = Path.Combine(BinDir, path);

			if (!Directory.Exists(path))
				throw new DirectoryNotFoundException(path);

			if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
			    !path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				path += Path.DirectorySeparatorChar;

			engineDirAccessed = true;
			engineDir = path;
		}

		public static string BinDir
		{
			get
			{
				var dir = AppDomain.CurrentDomain.BaseDirectory;

				// Add trailing DirectorySeparator for some buggy AppPool hosts
				if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
					dir += Path.DirectorySeparatorChar;

				return dir;
			}
		}

		/// <summary>Replaces special character prefixes with full paths.</summary>
		public static string ResolvePath(string path)
		{
			path = path.TrimEnd(' ', '\t');

			if (path == "^SupportDir")
				return SupportDir;

			if (path == "^EngineDir")
				return EngineDir;

			if (path == "^BinDir")
				return BinDir;

			if (path.StartsWith("^SupportDir|", StringComparison.Ordinal))
				path = SupportDir + path.Substring(12);

			if (path.StartsWith("^EngineDir|", StringComparison.Ordinal))
				path = EngineDir + path.Substring(11);

			if (path.StartsWith("^BinDir|", StringComparison.Ordinal))
				path = BinDir + path.Substring(8);

			return path;
		}
	}
}
