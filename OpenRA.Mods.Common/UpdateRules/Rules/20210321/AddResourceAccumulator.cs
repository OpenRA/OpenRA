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

		public override string Description => "DefaultResourceAccumulator was added and traits UseStorage and DiscardExcessResources were moved from Refinery.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			MiniYamlNode resourceAccumulatorNode = null;

			var refineries = actorNode.ChildrenMatching("Refinery");
			var tiberianSunRefineries = actorNode.ChildrenMatching("TiberianSunRefinery");
			var allRefineries = new List<MiniYamlNode>(refineries);
			allRefineries.AddRange(tiberianSunRefineries);

			foreach (var refineryNode in allRefineries)
			{
				resourceAccumulatorNode = new MiniYamlNode("ResourceAccumulator", "");

				var useStorageNode = refineryNode.LastChildMatching("UseStorage");
				var discardExcessResourcesNode = refineryNode.LastChildMatching("DiscardExcessResources");

				if (useStorageNode != null)
				{
					refineryNode.RemoveNode(useStorageNode);
					resourceAccumulatorNode.AddNode("UseStorage", useStorageNode.NodeValue<bool>());
				}

				if (discardExcessResourcesNode != null)
				{
					refineryNode.RemoveNode(discardExcessResourcesNode);
					resourceAccumulatorNode.AddNode("DiscardExcessResources", discardExcessResourcesNode.NodeValue<bool>());
				}

				break;
			}

			if (resourceAccumulatorNode != null)
			{
				actorNode.AddNode(resourceAccumulatorNode);
			}

			yield break;
		}
	}
}
