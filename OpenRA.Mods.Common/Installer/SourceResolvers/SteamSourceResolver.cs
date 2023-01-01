#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace OpenRA.Mods.Common.Installer
{
	public class SteamSourceResolver : ISourceResolver
	{
		public string FindSourcePath(ModContent.ModSource modSource)
		{
			modSource.Type.ToDictionary().TryGetValue("AppId", out var appId);

			if (appId == null)
				return null;

			foreach (var steamDirectory in SteamDirectory())
			{
				var manifestPath = Path.Combine(steamDirectory, "steamapps", $"appmanifest_{appId.Value}.acf");

				if (!File.Exists(manifestPath))
					continue;

				var data = ParseGameManifest(manifestPath);

				if (!data.TryGetValue("StateFlags", out var stateFlags) || stateFlags != "4")
					continue;

				if (!data.TryGetValue("installdir", out var installDir))
					continue;

				if (installDir != null)
					return Path.Combine(steamDirectory, "steamapps", "common", installDir);
			}

			return null;
		}

		public Availability GetAvailability()
		{
			return Availability.DigitalInstall;
		}

		static IEnumerable<string> SteamDirectory()
		{
			var candidatePaths = new List<string>();

			switch (Platform.CurrentPlatform)
			{
				case PlatformType.Windows:
				{
					// We need an extra check for the platform here to silence a warning when the registry is accessed
					// TODO: Remove this once our platform checks use the same method
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						break;

					var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

					foreach (var prefix in prefixes)
					{
						if (Registry.GetValue($"{prefix}Valve\\Steam", "InstallPath", null) is string path)
							candidatePaths.Add(path);
					}

					break;
				}

				case PlatformType.OSX:
					candidatePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam"));

					break;

				case PlatformType.Linux:
					candidatePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "root"));

					break;

				default:
					break;
			}

			foreach (var libraryPath in candidatePaths.Where(Directory.Exists))
			{
				yield return libraryPath;

				var libraryFoldersPath = Path.Combine(libraryPath, "steamapps", "libraryfolders.vdf");

				if (!File.Exists(libraryFoldersPath))
					continue;

				foreach (var e in ParseLibraryManifest(libraryFoldersPath).Where(e => e.Item1 == "path"))
					yield return e.Item2;
			}
		}

		static Dictionary<string, string> ParseGameManifest(string path)
		{
			var regex = new Regex("^\\s*\"(?<key>[^\"]*)\"\\s*\"(?<value>[^\"]*)\"\\s*$");
			var result = new Dictionary<string, string>();

			using (var s = new FileStream(path, FileMode.Open))
			{
				foreach (var line in s.ReadAllLines())
				{
					var match = regex.Match(line);

					if (match.Success)
						result[match.Groups["key"].Value] = match.Groups["value"].Value;
				}
			}

			return result;
		}

		static List<Tuple<string, string>> ParseLibraryManifest(string path)
		{
			var regex = new Regex("^\\s*\"(?<key>[^\"]*)\"\\s*\"(?<value>[^\"]*)\"\\s*$");
			var result = new List<Tuple<string, string>>();

			using (var s = new FileStream(path, FileMode.Open))
			{
				foreach (var line in s.ReadAllLines())
				{
					var match = regex.Match(line);

					if (match.Success)
						result.Add(new Tuple<string, string>(match.Groups["key"].Value, match.Groups["value"].Value));
				}
			}

			return result;
		}
	}
}
