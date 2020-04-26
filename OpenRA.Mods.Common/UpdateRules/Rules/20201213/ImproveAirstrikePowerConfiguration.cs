#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ImproveAirstrikePowerConfiguration : UpdateRule
	{
		public override string Name => "Improve AirstrikePower configurability.";

		public override string Description => "Add squadron configuration to AirstrikePower for each individual aircraft.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var airstrike in actorNode.ChildrenMatching("AirstrikePower"))
			{
				if (airstrike.Key.StartsWith("-"))
					continue;

				var prerequisites = airstrike.ChildrenMatching("Prerequisites").FirstOrDefault();
				if (prerequisites != null && prerequisites.Value.Value == "~disabled")
					continue;

				var typeNode = airstrike.LastChildMatching("UnitType");
				var countNode = airstrike.LastChildMatching("SquadSize");
				var offsetNode = airstrike.LastChildMatching("SquadOffset");

				var unitType = typeNode?.NodeValue<string>() ?? "badr.bomber";
				var squadSize = countNode?.NodeValue<int>() ?? 1;
				var offset = offsetNode?.NodeValue<WVec>() ?? new WVec(-1536, 1536, 0);

				var squadNode = new MiniYamlNode("Squad", "");
				for (var i = -squadSize / 2; i <= squadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (squadSize & 1) == 0)
						continue;

					var squadMemberNodeKey = squadSize == 1 ? "SquadMember" : "SquadMember@{0}".F(i + squadSize / 2);
					var squadMemberNode = new MiniYamlNode(squadMemberNodeKey, "");
					squadMemberNode.AddNode(new MiniYamlNode("UnitType", FieldSaver.FormatValue(unitType)));

					if (i != 0)
					{
						squadMemberNode.AddNode(new MiniYamlNode("SpawnOffset", FieldSaver.FormatValue(new WVec(Math.Abs(i) * offset.X, i * offset.Y, 0))));
						squadMemberNode.AddNode(new MiniYamlNode("TargetOffset", FieldSaver.FormatValue(new WVec(0, i * offset.Y, 0))));
					}

					squadNode.AddNode(squadMemberNode);
				}

				airstrike.AddNode(squadNode);
				airstrike.RemoveNode(typeNode);
				airstrike.RemoveNode(countNode);
				airstrike.RemoveNode(offsetNode);
			}

			yield break;
		}
	}
}
