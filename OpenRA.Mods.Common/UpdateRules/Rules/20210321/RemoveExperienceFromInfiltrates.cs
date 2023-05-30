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
	public class RemoveExperienceFromInfiltrates : UpdateRule
	{
		public override string Name => "Removes PlayerExperience property from Infiltrates.";

		public override string Description => "Infiltrates property PlayerExperience was removed, it was replaced by adding PlayerExperience to all InfiltrateFor* Traits.";

		readonly List<string> locations = new();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The 'PlayerExperience' fields have been removed from the 'Infiltrates' trait\n" +
					"and added to InfiltrateFor* traits. If you want to keep 'PlayerExperience' you will\n" +
					"need to add it to each of the InfiltrateFor* traits. Properties removed from:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var removed = false;
			foreach (var node in actorNode.ChildrenMatching("Infiltrates"))
				if (node.RemoveNodes("PlayerExperience") > 0)
					removed = true;

			if (removed)
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
