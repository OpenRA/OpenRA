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
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Widgets.Logic;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Installer
{
	public class ExtractBlastSourceAction : ISourceAction
	{
		public void RunActionOnSource(MiniYaml actionYaml, string path, ModData modData, List<string> extracted, Action<string> updateMessage)
		{
			// Yaml path may be specified relative to a named directory (e.g. ^SupportDir) or the detected source path
			var sourcePath = actionYaml.Value.StartsWith("^") ? Platform.ResolvePath(actionYaml.Value) : FS.ResolveCaseInsensitivePath(Path.Combine(path, actionYaml.Value));

			using (var source = File.OpenRead(sourcePath))
			{
				source.Position = 12;
				var numFiles = source.ReadUInt16();
				source.Position = 51;
				source.Position = source.ReadUInt32();

				var entries = new Dictionary<string, (uint Length, uint Offset)>();

				for (var i = 0; i < numFiles; i++)
				{
					source.Position += 7;
					var entry = (source.ReadUInt32(), source.ReadUInt32());
					source.Position += 14;
					var key = source.ReadASCII(source.ReadByte());
					source.Position += 13;

					// This does not apply on game relevant data.
					if (!entries.ContainsKey(key))
						entries.Add(key, entry);
				}

				foreach (var node in actionYaml.Nodes)
				{
					var targetPath = Platform.ResolvePath(node.Key);

					if (File.Exists(targetPath))
					{
						Log.Write("install", "Skipping installed file " + targetPath);
						continue;
					}

					var entry = entries[node.Value.Value];

					source.Position = entry.Offset;

					extracted.Add(targetPath);
					Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
					var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));

					Action<long> onProgress = null;
					if (entry.Length < InstallFromSourceLogic.ShowPercentageThreshold)
						updateMessage(modData.Translation.GetString(InstallFromSourceLogic.Extracing, Translation.Arguments("filename", displayFilename)));
					else
						onProgress = b => updateMessage(modData.Translation.GetString(InstallFromSourceLogic.ExtracingProgress, Translation.Arguments("filename", displayFilename, "progress", 100 * b / entry.Length)));

					using (var target = File.OpenWrite(targetPath))
					{
						Log.Write("install", $"Extracting {sourcePath} -> {targetPath}");
						Blast.Decompress(source, target, (read, _) => onProgress?.Invoke(read));
					}
				}
			}
		}
	}
}
