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
	public class UpdateRepairableBuildingProperty : UpdateRule
	{
		public override string Name => "Replaces PlayerExperience property with PlayerExperiencePercentage.";

		public override string Description => "PlayerExperience was replaced with PlayerExperiencePercentage which bases score on cash spent.";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "'PlayerExperience' was replaced with 'PlayerExperiencePercentage' which bases score\n" +
					"on cash spent. You may want to review score gains.\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var removed = false;

			foreach (var actor in actorNode.ChildrenMatching("RepairableBuilding"))
			{
				foreach (var p in actor.ChildrenMatching("PlayerExperience"))
				{
					p.Key = "PlayerExperiencePercentage";
					p.Value.Value = "5";
					removed = true;
				}
			}

			if (removed)
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
