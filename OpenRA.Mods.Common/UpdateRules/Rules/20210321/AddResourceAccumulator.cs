#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class AddResourceAccumulator : UpdateRule
	{
		public override string Name => "Refinery had changes to use a ResourceAccumulator trait for altering player resources.";

		public override string Description => "ResourceAccumulator was added and fields UseStorage and DiscardExcessResources were moved there from Refinery.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var refineryNode = actorNode.LastChildMatching("Refinery") ?? actorNode.LastChildMatching("TiberianSunRefinery");
			if (refineryNode != null)
			{
				var resourceAccumulatorNode = new MiniYamlNode("ResourceAccumulator", "");
				refineryNode.AddNode(resourceAccumulatorNode);

				var useStorageNode = refineryNode.LastChildMatching("UseStorage");

				if (useStorageNode != null)
				{
					refineryNode.RemoveNode(useStorageNode);
					resourceAccumulatorNode.AddNode("UseStorage", useStorageNode.NodeValue<bool>());
				}

				var discardExcessResourcesNode = refineryNode.LastChildMatching("DiscardExcessResources");

				if (discardExcessResourcesNode != null)
				{
					refineryNode.RemoveNode(discardExcessResourcesNode);
					resourceAccumulatorNode.AddNode("DiscardExcessResources", discardExcessResourcesNode.NodeValue<bool>());
				}
			}

			yield break;
		}
	}
}
