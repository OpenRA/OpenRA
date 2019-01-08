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
	public class RemoveRepairBuildingsFromAircraft : UpdateRule
	{
		public override string Name { get { return "Removed RepairBuildings from Aircraft"; } }
		public override string Description
		{
			get
			{
				return "Removed RepairBuildings from Aircraft in favor of using Repairable instead.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// Aircraft isn't conditional or otherwise supports multiple traits, so LastChildMatching is fine.
			var aircraftNode = actorNode.LastChildMatching("Aircraft");
			if (aircraftNode != null)
			{
				var repairBuildings = aircraftNode.LastChildMatching("RepairBuildings");
				if (repairBuildings != null)
				{
					var repariableNode = new MiniYamlNode("Repairable", "");
					repairBuildings.MoveAndRenameNode(aircraftNode, repariableNode, "RepairBuildings");

					var voice = aircraftNode.LastChildMatching("Voice");
					if (voice != null)
						repariableNode.AddNode(voice);

					actorNode.AddNode(repariableNode);
				}
			}

			yield break;
		}
	}
}
