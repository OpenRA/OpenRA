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
using System.Collections.Immutable;

namespace OpenRA.Mods.Common.FileSystem
{
	[Desc("A file system that loads game assets installed by the user into their support directory.")]
	public class ContentInstallerFileSystemLoader : IFileSystemLoader, IFileSystemExternalContent
	{
		[FieldLoader.Require]
		[Desc("Mod to use for content installation.")]
		public readonly string ContentInstallerMod = null;

		[Desc("A list of mod-provided packages. Anything required to display the initial load screen must be listed here.")]
		[FieldLoader.LoadUsing(nameof(LoadSystemPackages))]
		public readonly ImmutableArray<KeyValuePair<string, string>> SystemPackages = default;

		[Desc("A list of user-installed packages. If missing (and not marked as optional), these will trigger the content installer.")]
		[FieldLoader.LoadUsing(nameof(LoadContentPackages))]
		public readonly ImmutableArray<KeyValuePair<string, string>> ContentPackages = default;

		[Desc("Files that aren't mounted as packages, but still need to trigger the content installer if missing.")]
		[FieldLoader.LoadUsing(nameof(LoadRequiredContentFiles))]
		public readonly ImmutableArray<KeyValuePair<string, string>> RequiredContentFiles = default;

		bool isContentAvailable = true;

		static object LoadSystemPackages(MiniYaml yaml)
		{
			return LoadPackages(yaml, nameof(SystemPackages), true);
		}

		static object LoadContentPackages(MiniYaml yaml)
		{
			return LoadPackages(yaml, nameof(ContentPackages), false);
		}

		static object LoadRequiredContentFiles(MiniYaml yaml)
		{
			return LoadPackages(yaml, nameof(RequiredContentFiles), false);
		}

		static object LoadPackages(MiniYaml yaml, string key, bool required)
		{
			var packageNode = yaml.NodeWithKeyOrDefault(key);
			if (packageNode == null)
			{
				if (required)
					throw new FieldLoader.MissingFieldsException([key]);
				return default(ImmutableArray<KeyValuePair<string, string>>);
			}

			var packages = new List<KeyValuePair<string, string>>(packageNode.Value.Nodes.Length);
			foreach (var node in packageNode.Value.Nodes)
				packages.Add(KeyValuePair.Create(node.Key, node.Value.Value));

			return packages.ToImmutableArray();
		}

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
			if (!isContentAvailable && Game.Mods.TryGetValue(ContentInstallerMod, out var mod))
				Game.InitializeMod(mod, new Arguments());

			return !isContentAvailable;
		}

		void IFileSystemExternalContent.ManageContent(ModData modData)
		{
			// Switching mods changes the world state (by disposing it),
			// so we can't do this inside the input handler.
			if (Game.Mods.TryGetValue(ContentInstallerMod, out var mod))
				Game.RunAfterTick(() => Game.InitializeMod(mod, new Arguments()));
		}
	}
}
