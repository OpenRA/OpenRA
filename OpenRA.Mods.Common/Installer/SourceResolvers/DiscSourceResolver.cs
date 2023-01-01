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
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Common.Installer
{
	public class DiscSourceResolver : ISourceResolver
	{
		public string FindSourcePath(ModContent.ModSource source)
		{
			var volumes = DriveInfo.GetDrives()
				.Where(d =>
				{
					if (d.DriveType == DriveType.CDRom && d.IsReady)
						return true;

					// HACK: the "TFD" DVD is detected as a fixed udf-formatted drive on OSX
					if (d.DriveType == DriveType.Fixed && d.DriveFormat == "udf")
						return true;

					return false;
				})
				.Select(v => v.RootDirectory.FullName);

			if (Platform.CurrentPlatform == PlatformType.Linux)
			{
				// Outside of Gnome, most mounting tools on Linux don't set DriveType.CDRom
				// so provide a fallback by allowing users to manually mount images on known paths
				volumes = volumes.Concat(new[]
				{
					"/media/openra",
					"/media/" + Environment.UserName + "/openra",
					"/mnt/openra"
				});
			}

			foreach (var volume in volumes)
				if (InstallerUtils.IsValidSourcePath(volume, source))
					return volume;

			return null;
		}

		public Availability GetAvailability()
		{
			return Availability.GameSource;
		}
	}
}
