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

using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.FileSystem
{
	public class RegistryDirectoryContentOrigin : ContentOrigin
	{
		[FieldLoader.Require]
		[Desc("The registry key to query")]
		public readonly string RegistryKey;

		[FieldLoader.Require]
		[Desc("The value to read from the registry key")]
		public readonly string RegistryValue;

		[Desc("List of registry tree prefixes to search for RegistryKey.")]
		public readonly ImmutableArray<string> RegistryPrefixes = ["HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\"];

		[Desc("Mount the volume using this explicit mount name.")]
		public readonly string Mount;

		public override bool TryMount(OpenRA.FileSystem.FileSystem fileSystem) => ResolveOrigin(fileSystem, true);
		public override bool CanMount(OpenRA.FileSystem.FileSystem fileSystem) => ResolveOrigin(fileSystem, false);

		bool ResolveOrigin(OpenRA.FileSystem.FileSystem fileSystem, bool mount)
		{
			// RuntimeInformation is used instead of Platform.CurrentInfo to silence a Microsoft.Win32.Registry runtime warning
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return false;

			foreach (var prefix in RegistryPrefixes)
			{
				if (Microsoft.Win32.Registry.GetValue(prefix + RegistryKey, RegistryValue, null) is not string path)
					continue;

				// Resolve 8.3 format (DOS-style) paths to the full path.
				path = Path.GetFullPath(path);
				if (!Directory.Exists(path))
					continue;

				var folder = new Folder(path);
				if (!IDFilesMatch(folder, IDFiles))
					continue;

				if (mount)
					fileSystem.Mount(folder, Mount);

				return true;
			}

			return false;
		}
	}
}
