#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class MergeRepairableTraits : UpdateRule
	{
		public override string Name { get { return "Merged RepairableNear into Repairable"; } }
		public override string Description
		{
			get
			{
				return "Merged RepairableNear into Repairable.\n" +
				"Additionally, renamed Repairable.RepairBuildings to RepairActors,\n" +
				"for consistency with RearmActors and since repairing at other actors should already be possible.\n" +
				"Finally, removed 'fix' default value from the latter.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// Repairable isn't conditional or otherwise supports multiple traits, so LastChildMatching is fine.
			var repairableNode = actorNode.LastChildMatching("Repairable");
			if (repairableNode != null)
			{
				var repairBuildings = repairableNode.LastChildMatching("RepairBuildings");
				if (repairBuildings != null)
					repairBuildings.RenameKey("RepairActors");
				else
					repairableNode.AddNode(new MiniYamlNode("RepairActors", "fix"));
			}

			// RepairableNear isn't conditional or otherwise supports multiple traits, so LastChildMatching is fine.
			var repairableNearNode = actorNode.LastChildMatching("RepairableNear");
			if (repairableNearNode != null)
			{
				var repairBuildings = repairableNearNode.LastChildMatching("Buildings");
				if (repairBuildings != null)
					repairBuildings.RenameKey("RepairActors");
				else
					repairableNearNode.AddNode(new MiniYamlNode("RepairActors", "spen, syrd"));

				var closeEnough = repairableNearNode.LastChildMatching("CloseEnough");
				if (closeEnough == null)
					repairableNearNode.AddNode(new MiniYamlNode("CloseEnough", "4c0"));

				repairableNearNode.AddNode(new MiniYamlNode("RequiresEnteringResupplier", "false"));
				repairableNearNode.RenameKey("Repairable");
			}

			yield break;
		}
	}
}
