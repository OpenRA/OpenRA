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
	public class StreamlineRepairableTraits : UpdateRule
	{
		public override string Name { get { return "Streamline RepairableNear and Repairable"; } }
		public override string Description
		{
			get
			{
				return "Renamed Repairable.RepairBuildings and RepairableNear.Buildings to RepairActors,\n" +
				"for consistency with RearmActors (and since repairing at other actors should already be possible).\n" +
				"Additionally, removed internal 'fix' and 'spen, syrd' default values.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// Repairable isn't conditional or otherwise supports multiple traits, so LastChildMatching should be fine.
			var repairableNode = actorNode.LastChildMatching("Repairable");
			if (repairableNode != null)
			{
				var repairBuildings = repairableNode.LastChildMatching("RepairBuildings");
				if (repairBuildings != null)
					repairBuildings.RenameKey("RepairActors");
				else
					repairableNode.AddNode(new MiniYamlNode("RepairActors", "fix"));
			}

			// RepairableNear isn't conditional or otherwise supports multiple traits, so LastChildMatching should be fine.
			var repairableNearNode = actorNode.LastChildMatching("RepairableNear");
			if (repairableNearNode != null)
			{
				var repairBuildings = repairableNearNode.LastChildMatching("Buildings");
				if (repairBuildings != null)
					repairBuildings.RenameKey("RepairActors");
				else
					repairableNearNode.AddNode(new MiniYamlNode("RepairActors", "spen, syrd"));
			}

			yield break;
		}
	}
}
