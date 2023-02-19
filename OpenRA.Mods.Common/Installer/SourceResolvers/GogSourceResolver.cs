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
using Microsoft.Win32;

namespace OpenRA.Mods.Common.Installer
{
	public class GogSourceResolver : ISourceResolver
	{
		public string FindSourcePath(ModContent.ModSource modSource)
		{
			modSource.Type.ToDictionary().TryGetValue("AppId", out var appId);

			if (appId == null)
				return null;

			if (Platform.CurrentPlatform != PlatformType.Windows)
				return null;

			// We need an extra check for the platform here to silence a warning when the registry is accessed
			// TODO: Remove this once our platform checks use the same method
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return null;

			var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

			foreach (var prefix in prefixes)
				if (Registry.GetValue($"{prefix}GOG.com\\Games\\{appId.Value}", "path", null) is string installDir)
					return installDir;

			return null;
		}

		public Availability GetAvailability()
		{
			return Platform.CurrentPlatform != PlatformType.Windows ? Availability.DigitalInstall : Availability.Unavailable;
		}
	}
}
