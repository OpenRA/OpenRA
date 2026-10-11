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
using System.Runtime.InteropServices;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.FileSystem
{
	[RequireExplicitImplementation]
	public interface IFileSystemExternalContent
	{
		bool InstallContentIfRequired(ModData modData);
		void ManageContent(ModData modData);
	}

	public class DefaultFileSystemLoader : IFileSystemLoader
	{
		[FieldLoader.LoadUsing(nameof(LoadPackages))]
		public readonly ImmutableArray<KeyValuePair<string, string>> Packages = default;

		static object LoadPackages(MiniYaml yaml)
		{
			var packageNode = yaml.NodeWithKeyOrDefault(nameof(Packages));
			if (packageNode == null)
				return default(ImmutableArray<KeyValuePair<string, string>>);

			var packages = new KeyValuePair<string, string>[packageNode.Value.Nodes.Length];
			for (var i = 0; i < packageNode.Value.Nodes.Length; i++)
			{
				var node = packageNode.Value.Nodes[i];
				packages[i] = KeyValuePair.Create(node.Key, node.Value.Value);
			}

			return ImmutableCollectionsMarshal.AsImmutableArray(packages);
		}

		public void Mount(Manifest manifest, OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			if (Packages != null)
				foreach (var kv in Packages)
					fileSystem.Mount(kv.Key, kv.Value);
		}
	}
}
