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
using System.IO;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class CopyIsometricSelectableHeight : UpdateRule
	{
		public override string Name => "Copy IsometricSelectable.Height from art*.ini definitions.";

		public override string Description =>
			"Reads building Height entries art.ini/artfs.ini/artmd.ini from the current working directory\n" +
			"and adds IsometricSelectable definitions to matching actors.";

		static readonly string[] SourceFiles = { "art.ini", "artfs.ini", "artmd.ini" };

		readonly Dictionary<string, int> selectionHeight = new Dictionary<string, int>();

		bool complete;

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			if (complete)
				yield break;

			var grid = Game.ModData.Manifest.Get<MapGrid>();
			foreach (var filename in SourceFiles)
			{
				if (!File.Exists(filename))
					continue;

				var file = new IniFile(File.Open(filename, FileMode.Open));
				foreach (var section in file.Sections)
				{
					if (!section.Contains("Height"))
						continue;

					selectionHeight[section.Name] = (int)(float.Parse(section.GetValue("Height", "1")) * grid.TileSize.Height);
				}
			}
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			// Rule only applies to the default ruleset - skip maps
			complete = true;
			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (complete || actorNode.LastChildMatching("IsometricSelectable") != null)
				yield break;

			if (!selectionHeight.TryGetValue(actorNode.Key.ToLowerInvariant(), out var height))
				yield break;

			// Don't redefine the default value
			if (height == 24)
				yield break;

			var selection = new MiniYamlNode("IsometricSelectable", "");
			selection.AddNode("Height", FieldSaver.FormatValue(height));

			actorNode.AddNode(selection);
		}
	}
}
