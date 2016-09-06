#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class ExtractMapRules : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--map-rules"; } }
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length == 2; }

		void MergeAndPrint(Map map, string key, MiniYaml value)
		{
			var nodes = new List<MiniYamlNode>();
			var includes = new List<string>();
			if (value != null && value.Value != null)
			{
				// The order of the included files matter, so we can defer to system files
				// only as long as they are included first.
				var include = false;
				var files = FieldLoader.GetValue<string[]>("value", value.Value);
				foreach (var f in files)
				{
					include |= map.Package.Contains(f);
					if (include)
						nodes.AddRange(MiniYaml.FromStream(map.Open(f), f));
					else
						includes.Add(f);
				}
			}

			if (value != null)
				nodes.AddRange(value.Nodes);

			var output = new MiniYaml(includes.JoinWith(", "), nodes);
			Console.WriteLine(output.ToLines(key).JoinWith("\n"));
		}

		[Desc("MAPFILE", "Merge custom map rules into a form suitable for including in map.yaml.")]
		void IUtilityCommand.Run(ModData modData, string[] args)
		{
			Game.ModData = modData;

			var map = new Map(modData, modData.ModFiles.OpenPackage(args[1], new Folder(".")));
			MergeAndPrint(map, "Rules", map.RuleDefinitions);
			MergeAndPrint(map, "Sequences", map.SequenceDefinitions);
			MergeAndPrint(map, "VoxelSequences", map.VoxelSequenceDefinitions);
			MergeAndPrint(map, "Weapons", map.WeaponDefinitions);
			MergeAndPrint(map, "Voices", map.VoiceDefinitions);
			MergeAndPrint(map, "Music", map.MusicDefinitions);
			MergeAndPrint(map, "Notifications", map.NotificationDefinitions);
		}
	}
}
