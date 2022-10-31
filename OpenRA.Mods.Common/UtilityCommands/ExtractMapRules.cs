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
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class ExtractMapRules : IUtilityCommand
	{
		string IUtilityCommand.Name => "--map-rules";
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length == 2; }

		void MergeAndPrint(IMap map, string key, MiniYaml value)
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
					include |= ((Map)map).Package.Contains(f);
					if (include)
						nodes.AddRange(MiniYaml.FromStream(((Map)map).Open(f), f, false));
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
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;

			var map = modData.MapLoader.Load(modData, new Folder(Platform.EngineDir).OpenPackage(args[1], modData.ModFiles));
			var imap = (IMap)map;
			MergeAndPrint(imap, "Rules", map.RuleDefinitions);
			MergeAndPrint(imap, "Sequences", map.SequenceDefinitions);
			MergeAndPrint(imap, "ModelSequences", map.ModelSequenceDefinitions);
			MergeAndPrint(imap, "Weapons", map.WeaponDefinitions);
			MergeAndPrint(imap, "Voices", map.VoiceDefinitions);
			MergeAndPrint(imap, "Music", map.MusicDefinitions);
			MergeAndPrint(imap, "Notifications", map.NotificationDefinitions);
		}
	}
}
