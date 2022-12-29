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
	public class RemoveDomainIndex : UpdateRule
	{
		public override string Name => "Remove DomainIndex from World and add path finder overlays.";

		public override string Description => "The DomainIndex trait was removed from World. Two overlay traits were added at the same time.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("DomainIndex") > 0)
			{
				actorNode.AddNode(new MiniYamlNode("PathFinderOverlay", ""));
				actorNode.AddNode(new MiniYamlNode("HierarchicalPathFinderOverlay", ""));
			}

			yield break;
		}
	}
}
