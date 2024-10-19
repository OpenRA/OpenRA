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
	public class ContentInstallerFileSystemLoader : IFileSystemLoader, IFileSystemExternalContent
	{
		[FieldLoader.Require]
		public readonly string ContentInstallerMod = null;

		[FieldLoader.Require]
		public readonly Dictionary<string, string> Packages = null;

		public readonly Dictionary<string, string> ContentPackages = null;

		public readonly Dictionary<string, string> ContentFiles = null;

		bool contentAvailable = true;

		public void Mount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			foreach (var kv in Packages)
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
						contentAvailable = false;
					}
				}
			}

			if (ContentFiles != null)
				foreach (var kv in ContentFiles)
					if (!fileSystem.Exists(kv.Key))
						contentAvailable = false;
		}

		bool IFileSystemExternalContent.InstallContentIfRequired(ModData modData)
		{
			if (!contentAvailable)
				Game.InitializeMod(ContentInstallerMod, new Arguments());

			return !contentAvailable;
		}
	}
}
