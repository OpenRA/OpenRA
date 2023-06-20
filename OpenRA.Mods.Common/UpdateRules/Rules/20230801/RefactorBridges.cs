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
	public class RefactorBridges : UpdateRule
	{
		public override string Name => "Refactor all bridge traits";

		public override string Description =>
			"`NeighbourOffsets` field was removed from `GroundLevelBridge` and `BridgePlaceHolder. \n" +
			"`NorthOffset` and`SouthOffset` fields were removed from `Bridge`.\n" +
			"`RepairPropagationDelay` field was removed from `Bridge as it exists on `BridgeHut`.\n" +
			"`BridgeHut`'s `NeighbourOffsets` field renamed to `BridgeOffsets`.\n" +
			"`LegacyBridgeHut` and `LegacyBridgeLayer` were removed, use `BridgeHut`\n" +
			"and `BridgeLayer` instead.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var trait in actorNode.ChildrenMatching("GroundLevelBridge"))
				trait.RemoveNodes("NeighbourOffsets");

			foreach (var trait in actorNode.ChildrenMatching("BridgePlaceHolder"))
				trait.RemoveNodes("NeighbourOffsets");

			foreach (var trait in actorNode.ChildrenMatching("BridgeHut"))
				trait.RenameChildrenMatching("NeighbourOffsets", "BridgeOffsets");

			foreach (var trait in actorNode.ChildrenMatching("LegacyBridgeLayer"))
			{
				trait.RenameKey("BridgeLayer");

				if (trait.LastChildMatching("Bridges", false) == null)
					trait.AddNode("Bridges", new string[] { "bridge1", "bridge2" });
			}

			foreach (var trait in actorNode.ChildrenMatching("LegacyBridgeHut"))
			{
				trait.RenameKey("BridgeHut");
				trait.AddNode("BridgeOffsets", new CPos[] { new CPos(0, 0) });
				trait.AddNode("DemolishPropagationDelay", 20);
			}

			foreach (var trait in actorNode.ChildrenMatching("Bridge"))
			{
				trait.RemoveNodes("NorthOffset");
				trait.RemoveNodes("SouthOffset");
				trait.RemoveNodes("RepairPropagationDelay");
			}

			yield break;
		}
	}
}
