#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	// Referenced from ModMetadata, so needs to be in OpenRA.Game :(
	public class ContentInstaller : IGlobalModData
	{
		public enum FilenameCase { Input, ForceLower, ForceUpper }

		public readonly string[] TestFiles = { };
		public readonly string[] DiskTestFiles = { };
		public readonly string PackageToExtractFromCD = null;
		public readonly bool OverwriteFiles = true;

		public readonly FilenameCase OutputFilenameCase = FilenameCase.ForceLower;
		public readonly Dictionary<string, string[]> CopyFilesFromCD = new Dictionary<string, string[]>();
		public readonly Dictionary<string, string[]> ExtractFilesFromCD = new Dictionary<string, string[]>();

		public readonly string PackageMirrorList = null;

		public readonly string MusicPackageMirrorList = null;
		public readonly int ShippedSoundtracks = 0;

		/// <summary> InstallShield .CAB file IDs, used to extract Mod-specific files. </summary>
		public readonly HashSet<int> InstallShieldCABFileIds = new HashSet<int>();

		/// <summary> InstallShield .CAB file IDs, used to extract Mod-specific archives and extract contents of ExtractFilesFromCD. </summary>
		public readonly HashSet<string> InstallShieldCABFilePackageIds = new HashSet<string>();
	}
}
