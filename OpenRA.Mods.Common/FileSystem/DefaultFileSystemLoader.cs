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
using System.IO;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.FileSystem
{
	[RequireExplicitImplementation]
	public interface IFileSystemExternalContent
	{
		public bool InstallContentIfRequired(ModData modData);
	}

	public class DefaultFileSystemLoader : IFileSystemLoader, IFileSystemExternalContent
	{
		public readonly Dictionary<string, string> Packages = null;

		public void Mount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			if (Packages != null)
				foreach (var kv in Packages)
					fileSystem.Mount(kv.Key, kv.Value);
		}

		bool IFileSystemExternalContent.InstallContentIfRequired(ModData modData)
		{
			// If a ModContent section is defined then we need to make sure that the
			// required content is installed or switch to the defined content installer.
			if (!modData.Manifest.Contains<ModContent>())
				return false;

			var content = modData.Manifest.Get<ModContent>();
			var contentInstalled = content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));

			if (contentInstalled)
				return false;

			Game.InitializeMod(content.ContentInstallerMod, new Arguments("Content.Mod=" + modData.Manifest.Id));
			return true;
		}
	}
}
