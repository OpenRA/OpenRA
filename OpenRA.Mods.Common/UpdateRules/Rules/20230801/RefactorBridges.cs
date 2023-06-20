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
		public override string Name => "`GroundLevelBridge` and `BridgePlaceHolder segments are now automatically joined";

		public override string Description =>
			"`NeighbourOffsets` was removed from `GroundLevelBridge` and `BridgePlaceHolder.\n" +
			"`BridgeHut`'s `NeighbourOffsets` renamed to `BridgeOffsets`";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var trait in actorNode.ChildrenMatching("GroundLevelBridge"))
				trait.RemoveNodes("NeighbourOffsets");

			foreach (var trait in actorNode.ChildrenMatching("BridgePlaceHolder"))
				trait.RemoveNodes("NeighbourOffsets");

			foreach (var trait in actorNode.ChildrenMatching("BridgeHut"))
				trait.RenameChildrenMatching("NeighbourOffsets", "BridgeOffsets");

			yield break;
		}
	}
}
