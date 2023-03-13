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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveSequenceHasEmbeddedPalette : UpdateRule
	{
		public override string Name => "Remove sequence HasEmbeddedPalette property.";

		public override string Description =>
			"The PaletteFromEmbeddedSpritePalette trait no longer references a sequence.\n" +
			"Image and Sequence are replaced by Filename and Frame.";

		readonly HashSet<string> locations = new();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The PaletteFromEmbeddedSpritePalette trait no longer references a sequence.\n" +
				             "You must manually define Filename (and Frame if needed) on the following actors:\n" +
							UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateSequenceNode(ModData modData, MiniYamlNode imageNode)
		{
			foreach (var sequenceNode in imageNode.Value.Nodes)
				sequenceNode.RemoveNodes("HasEmbeddedPalette");

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.ChildrenMatching("PaletteFromEmbeddedSpritePalette"))
			{
				traitNode.RemoveNodes("Image");
				traitNode.RemoveNodes("Sequence");
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
			}

			yield break;
		}
	}
}
