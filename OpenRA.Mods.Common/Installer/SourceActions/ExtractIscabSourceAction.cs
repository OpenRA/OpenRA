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
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Widgets.Logic;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Installer
{
	public class ExtractIscabSourceAction : ISourceAction
	{
		public void RunActionOnSource(MiniYaml actionYaml, string path, ModData modData, List<string> extracted, Action<string> updateMessage)
		{
			// Yaml path may be specified relative to a named directory (e.g. ^SupportDir) or the detected source path
			var sourcePath = actionYaml.Value.StartsWith("^") ? Platform.ResolvePath(actionYaml.Value) : FS.ResolveCaseInsensitivePath(Path.Combine(path, actionYaml.Value));

			var volumeNode = actionYaml.Nodes.FirstOrDefault(n => n.Key == "Volumes");
			if (volumeNode == null)
				throw new InvalidDataException("extract-iscab entry doesn't define a Volumes node");

			var extractNode = actionYaml.Nodes.FirstOrDefault(n => n.Key == "Extract");
			if (extractNode == null)
				throw new InvalidDataException("extract-iscab entry doesn't define an Extract node");

			var volumes = new Dictionary<int, Stream>();
			try
			{
				foreach (var node in volumeNode.Value.Nodes)
				{
					var volume = FieldLoader.GetValue<int>("(key)", node.Key);
					var stream = File.OpenRead(FS.ResolveCaseInsensitivePath(Path.Combine(path, node.Value.Value)));
					volumes.Add(volume, stream);
				}

				using (var source = File.OpenRead(sourcePath))
				{
					var reader = new InstallShieldCABCompression(source, volumes);
					foreach (var node in extractNode.Value.Nodes)
					{
						var targetPath = Platform.ResolvePath(node.Key);

						if (File.Exists(targetPath))
						{
							Log.Write("install", "Skipping installed file " + targetPath);
							continue;
						}

						extracted.Add(targetPath);
						Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
						using (var target = File.OpenWrite(targetPath))
						{
							Log.Write("install", $"Extracting {sourcePath} -> {targetPath}");
							var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));
							Action<int> onProgress = percent => updateMessage(modData.Translation.GetString(InstallFromSourceLogic.ExtracingProgress, Translation.Arguments("filename", displayFilename, "progress", percent)));
							reader.ExtractFile(node.Value.Value, target, onProgress);
						}
					}
				}
			}
			finally
			{
				foreach (var kv in volumes)
					kv.Value.Dispose();
			}
		}
	}
}
