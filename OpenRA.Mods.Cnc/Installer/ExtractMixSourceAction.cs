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
using System.Linq;
using OpenRA.Mods.Cnc.FileSystem;
using OpenRA.Mods.Common.Installer;
using OpenRA.Mods.Common.Widgets.Logic;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Cnc.Installer
{
	public class ExtractMixSourceAction : ISourceAction
	{
		public void RunActionOnSource(MiniYaml actionYaml, string path, ModData modData, List<string> extracted, Action<string> updateMessage)
		{
			// Yaml path may be specified relative to a named directory (e.g. ^SupportDir) or the detected source path
			var sourcePath = actionYaml.Value.StartsWith("^") ? Platform.ResolvePath(actionYaml.Value) : FS.ResolveCaseInsensitivePath(Path.Combine(path, actionYaml.Value));

			using (var source = File.OpenRead(sourcePath))
			{
				var mixFile = new MixLoader.MixFile(source, Path.GetFileName(sourcePath), actionYaml.Nodes.Select(e => e.Value.Value).ToArray());

				foreach (var node in actionYaml.Nodes)
				{
					var targetPath = Platform.ResolvePath(node.Key);

					if (File.Exists(targetPath))
					{
						Log.Write("install", "Skipping installed file " + targetPath);
						continue;
					}

					using (var stream = mixFile.GetStream(node.Value.Value))
					{
						extracted.Add(targetPath);
						Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
						var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));

						Action<long> onProgress = null;
						if (stream.Length < InstallFromSourceLogic.ShowPercentageThreshold)
							updateMessage(modData.Translation.GetString(InstallFromSourceLogic.Extracing, Translation.Arguments("filename", displayFilename)));
						else
							onProgress = b => updateMessage(modData.Translation.GetString(InstallFromSourceLogic.ExtracingProgress, Translation.Arguments("filename", displayFilename, "progress", 100 * b / stream.Length)));

						using (var target = File.OpenWrite(targetPath))
						{
							Log.Write("install", $"Extracting {sourcePath} -> {targetPath}");

							InstallerUtils.CopyStream(stream, target, stream.Length, onProgress);
						}
					}
				}
			}
		}
	}
}
