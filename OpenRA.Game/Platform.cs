#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

	public static class Platform
	{
		public const string SupportDirPrefix = "^";
		public static PlatformType CurrentPlatform { get { return currentPlatform.Value; } }
		public static readonly Guid SessionGUID = Guid.NewGuid();

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
		/// The directory will automatically be created if it does not exist when this is queried.
		/// </summary>
		public static string SupportDir { get { return supportDir.Value; } }
		static Lazy<string> supportDir = Exts.Lazy(GetSupportDir);
		static string supportDirOverride;

		/// <summary>
		/// Specify a custom support directory that already exists on the filesystem.
		/// MUST be called before Platform.SupportDir is first accessed.
		/// </summary>
		public static void OverrideSupportDir(string path)
		{
			if (!Directory.Exists(path))
				throw new DirectoryNotFoundException(path);

			if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
					!path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				path += Path.DirectorySeparatorChar;

			supportDirOverride = path;
		}

		static string GetSupportDir()
		{
			// Use the custom override if it has been defined
			if (supportDirOverride != null)
				return supportDirOverride;

			// Use a local directory in the game root if it exists (shared with the system support dir)
			var localSupportDir = Path.Combine(GameDir, "Support");
			if (Directory.Exists(localSupportDir))
				return localSupportDir + Path.DirectorySeparatorChar;

			// The preferred support dir location for Windows and Linux was changed in mid 2019 to match modern platform conventions
			string preferredSupportDir;
			string fallbackSupportDir;
			switch (CurrentPlatform)
			{
				case PlatformType.Windows:
				{
					preferredSupportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRA");
					fallbackSupportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "OpenRA");
					break;
				}

				case PlatformType.OSX:
				{
					preferredSupportDir = fallbackSupportDir = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.Personal),
						"Library", "Application Support", "OpenRA");
					break;
				}

				case PlatformType.Linux:
				{
					fallbackSupportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".openra");

					var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
					if (string.IsNullOrEmpty(xdgConfigHome))
						xdgConfigHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");

					preferredSupportDir = Path.Combine(xdgConfigHome, "openra");

					break;
				}

				default:
				{
					preferredSupportDir = fallbackSupportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".openra");
					break;
				}
			}

			// Use the fallback directory if it exists and the preferred one does not
			if (!Directory.Exists(preferredSupportDir) && Directory.Exists(fallbackSupportDir))
				return fallbackSupportDir + Path.DirectorySeparatorChar;

			return preferredSupportDir + Path.DirectorySeparatorChar;
		}

		/// <summary>
		/// Directory containing system-wide support files (mod metadata).
		/// This directory is not guaranteed to exist or be writable.
		/// Consumers are expected to check the validity of the returned value, and
		/// fall back to the user support directory if necessary.
		/// </summary>
		public static string SystemSupportDir { get { return systemSupportDir.Value; } }
		static Lazy<string> systemSupportDir = Exts.Lazy(GetSystemSupportDir);

		static string GetSystemSupportDir()
		{
			// Use a local directory in the game root if it exists (shared with the system support dir)
			var localSupportDir = Path.Combine(GameDir, "Support");
			if (Directory.Exists(localSupportDir))
				return localSupportDir + Path.DirectorySeparatorChar;

			switch (CurrentPlatform)
			{
				case PlatformType.Windows:
					return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OpenRA") + Path.DirectorySeparatorChar;
				case PlatformType.OSX:
					return "/Library/Application Support/OpenRA/";
				default:
					return "/var/games/openra/";
			}
		}

		public static string GameDir
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

			// Paths starting with ^ are relative to the support dir
			if (IsPathRelativeToSupportDirectory(path))
				path = SupportDir + path.Substring(1);

			// Paths starting with . are relative to the game dir
			if (path == ".")
				return GameDir;

			if (path.StartsWith("./", StringComparison.Ordinal) || path.StartsWith(".\\", StringComparison.Ordinal))
				path = GameDir + path.Substring(2);

			return path;
		}

		/// <summary>Replace special character prefixes with full paths.</summary>
		public static string ResolvePath(params string[] path)
		{
			return ResolvePath(Path.Combine(path));
		}

		/// <summary>
		/// Replace the full path prefix with the special notation characters ^ or .
		/// and transforms \ path separators to / on Windows
		/// </summary>
		public static string UnresolvePath(string path)
		{
			// Use a case insensitive comparison on windows to avoid problems
			// with inconsistent drive letter case
			var compare = CurrentPlatform == PlatformType.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if (path.StartsWith(SupportDir, compare))
				path = SupportDirPrefix + path.Substring(SupportDir.Length);

			if (path.StartsWith(GameDir, compare))
				path = "./" + path.Substring(GameDir.Length);

			if (CurrentPlatform == PlatformType.Windows)
				path = path.Replace('\\', '/');

			return path;
		}

		public static bool IsPathRelativeToSupportDirectory(string path)
		{
			return path.StartsWith(SupportDirPrefix, StringComparison.Ordinal);
		}
	}
}
