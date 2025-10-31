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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.FileSystem
{
	[Desc("A file system that loads game assets installed by the user into their support directory.")]
	public class ContentInstallerFileSystemLoader : IFileSystemLoader, IFileSystemExternalContent
	{
		[FieldLoader.Require]
		[Desc("Mod to use for content installation.")]
		public readonly string ContentInstallerMod = null;

		[FieldLoader.Require]
		[Desc("A list of mod-provided packages. Anything required to display the initial load screen must be listed here.")]
		public readonly Dictionary<string, string> SystemPackages = null;

		[Desc("A list of user-installed packages. If missing (and not marked as optional), these will trigger the content installer.")]
		public readonly Dictionary<string, string> ContentPackages = null;

		[Desc("Files that aren't mounted as packages, but still need to trigger the content installer if missing.")]
		public readonly Dictionary<string, string> RequiredContentFiles = null;

		bool isContentAvailable = true;

		public void Mount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			foreach (var kv in SystemPackages)
				fileSystem.Mount(kv.Key, kv.Value);

			if (ContentPackages != null)
			{
				foreach (var kv in ContentPackages)
				{
					try
					{
						fileSystem.Mount(kv.Key, kv.Value);
					}
					catch
					{
						isContentAvailable = false;
					}
				}
			}

			if (RequiredContentFiles != null)
				foreach (var kv in RequiredContentFiles)
					if (!fileSystem.Exists(kv.Key))
						isContentAvailable = false;
		}

		bool IFileSystemExternalContent.InstallContentIfRequired(ModData modData)
		{
			if (!isContentAvailable)
				Game.InitializeMod(ContentInstallerMod, new Arguments());

			return !isContentAvailable;
		}
	}
}
