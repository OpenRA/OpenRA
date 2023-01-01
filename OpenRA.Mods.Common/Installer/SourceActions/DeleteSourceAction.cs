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
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.Common.Installer
{
	public class DeleteSourceAction : ISourceAction
	{
		public void RunActionOnSource(MiniYaml actionYaml, string path, ModData modData, List<string> extracted, Action<string> updateMessage)
		{
			// Yaml path must be specified relative to a named directory (e.g. ^SupportDir)
			if (!actionYaml.Value.StartsWith("^"))
				return;

			var sourcePath = Platform.ResolvePath(actionYaml.Value);

			Log.Write("debug", $"Deleting {sourcePath}");
			File.Delete(sourcePath);
		}
	}
}
