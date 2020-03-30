#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class SplitRepairDecoration : UpdateRule
	{
		public override string Name { get { return "WithBuildingRepairDecoration trait split from RepairableBuilding"; } }
		public override string Description
		{
			get
			{
				return "Rendering for the building repair indicator has been moved to a new\n" +
					"WithBuildingRepairDecoration trait. The indicator definitions are automatically\n" +
					"migrated from RepairableBuilding to WithBuildingRepairDecoration.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// RepairableBuilding is hardcoded to only support one instance per actor
			var rb = actorNode.LastChildMatching("RepairableBuilding");
			if (rb != null)
			{
				var imageNode = rb.LastChildMatching("IndicatorImage");
				var sequenceNode = rb.LastChildMatching("IndicatorSequence");
				var paletteNode = rb.LastChildMatching("IndicatorPalette");
				var palettePrefixNode = rb.LastChildMatching("IndicatorPalettePrefix");

				var decoration = new MiniYamlNode("WithBuildingRepairDecoration", "");
				decoration.AddNode("Image", imageNode != null ? imageNode.Value.Value : "allyrepair");
				decoration.AddNode("Sequence", sequenceNode != null ? sequenceNode.Value.Value : "repair");
				decoration.AddNode("ReferencePoint", "Center");

				if (paletteNode != null)
				{
					decoration.AddNode("Palette", paletteNode.Value.Value);
				}
				else
				{
					decoration.AddNode("Palette", palettePrefixNode != null ? palettePrefixNode.Value.Value : "player");
					decoration.AddNode("IsPlayerPalette", true);
				}

				actorNode.AddNode(decoration);

				rb.RemoveNode(imageNode);
				rb.RemoveNode(sequenceNode);
				rb.RemoveNode(paletteNode);
				rb.RemoveNode(palettePrefixNode);
			}

			if (actorNode.LastChildMatching("-RepairableBuilding") != null)
				actorNode.AddNode("-WithBuildingRepairDecoration", "");

			yield break;
		}
	}
}
