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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.FileSystem
{
	[RequireExplicitImplementation]
	public interface IFileSystemExternalContent
	{
		bool InstallContentIfRequired(ModData modData);
	}

	public class DefaultFileSystemLoader : IFileSystemLoader
	{
		public readonly Dictionary<string, string> Packages = null;

		public void Mount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			if (Packages != null)
				foreach (var kv in Packages)
					fileSystem.Mount(kv.Key, kv.Value);
		}
	}
}
