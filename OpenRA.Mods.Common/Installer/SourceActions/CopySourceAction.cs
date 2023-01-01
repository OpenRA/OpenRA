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
using OpenRA.Mods.Common.Widgets.Logic;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Installer
{
	public class CopySourceAction : ISourceAction
	{
		public void RunActionOnSource(MiniYaml actionYaml, string path, ModData modData, List<string> extracted, Action<string> updateMessage)
		{
			var sourceDir = Path.Combine(path, actionYaml.Value);
			foreach (var node in actionYaml.Nodes)
			{
				var sourcePath = FS.ResolveCaseInsensitivePath(Path.Combine(sourceDir, node.Value.Value));
				var targetPath = Platform.ResolvePath(node.Key);
				if (File.Exists(targetPath))
				{
					Log.Write("install", "Ignoring installed file " + targetPath);
					continue;
				}

				Log.Write("install", $"Copying {sourcePath} -> {targetPath}");
				extracted.Add(targetPath);
				Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

				using (var source = File.OpenRead(sourcePath))
				using (var target = File.OpenWrite(targetPath))
				{
					var displayFilename = Path.GetFileName(targetPath);
					var length = source.Length;

					Action<long> onProgress = null;
					if (length < InstallFromSourceLogic.ShowPercentageThreshold)
						updateMessage(modData.Translation.GetString(InstallFromSourceLogic.CopyingFilename, Translation.Arguments("filename", displayFilename)));
					else
						onProgress = b => updateMessage(modData.Translation.GetString(InstallFromSourceLogic.CopyingFilenameProgress, Translation.Arguments("filename", displayFilename, "progress", 100 * b / length)));

					InstallerUtils.CopyStream(source, target, length, onProgress);
				}
			}
		}
	}
}
