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

using System.Runtime.InteropServices;

namespace OpenRA.Mods.Common.Installer
{
	public class RegistryDirectorySourceResolver : ISourceResolver
	{
		public string FindSourcePath(ModContent.ModSource source)
		{
			if (source.RegistryKey == null)
				return null;

			if (Platform.CurrentPlatform != PlatformType.Windows)
				return null;

			// We need an extra check for the platform here to silence a warning when the registry is accessed
			// TODO: Remove this once our platform checks use the same method
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return null;

			foreach (var prefix in source.RegistryPrefixes)
			{
				if (!(Microsoft.Win32.Registry.GetValue(prefix + source.RegistryKey, source.RegistryValue, null) is string path))
					continue;

				return InstallerUtils.IsValidSourcePath(path, source) ? path : null;
			}

			return null;
		}

		public Availability GetAvailability()
		{
			return Platform.CurrentPlatform != PlatformType.Windows ? Availability.DigitalInstall : Availability.Unavailable;
		}
	}
}
