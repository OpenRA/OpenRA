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
	public class ExtractResourceStorageFromHarvester : UpdateRule
	{
		public override string Name => "Renames StoresResources to StoresPlayerResources and extracts StoresResources from Harvester.";

		public override string Description =>
			"Resource storage was extracted from Harvester. WithHarvesterPipsDecoration was also renamed to WithStoresResourcesPipsDecoration.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			actorNode.RenameChildrenMatching("StoresResources", "StoresPlayerResources");
			actorNode.RenameChildrenMatching("WithHarvesterPipsDecoration", "WithStoresResourcesPipsDecoration");

			var harvester = actorNode.LastChildMatching("Harvester", false);
			if (harvester == null)
				yield break;

			var storesResources = new MiniYamlNodeBuilder("StoresResources", "");
			var capacity = harvester.LastChildMatching("Capacity", false);
			if (capacity != null)
			{
				storesResources.AddNode(capacity);
				harvester.RemoveNode(capacity);
			}

			var resources = harvester.LastChildMatching("Resources", false);
			if (resources != null)
				storesResources.AddNode(resources);

			actorNode.AddNode(storesResources);

			yield break;
		}
	}
}
