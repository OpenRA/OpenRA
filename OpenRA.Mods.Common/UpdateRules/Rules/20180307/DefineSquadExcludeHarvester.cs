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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class DefineSquadExcludeHarvester : UpdateRule
	{
		public override string Name { get { return "Add harvesters to ExcludeFromSquads"; } }
		public override string Description
		{
			get
			{
				return "HackyAI no longer automatically excludes actors with Harvester trait from attack squads.\n" +
					"They need to be explicitly added to ExcludeFromSquads. HackyAI instances are listed for inspection.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The automatic exclusion of harvesters from AI squads has been removed.\n" +
					"You may wish to add your harvester-type actors to `ExcludeFromSquads` under `UnitCommonNames`\n" +
					"on the following definitions:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var hackyAINode in actorNode.ChildrenMatching("HackyAI"))
				locations.Add("{0} ({1})".F(hackyAINode.Key, hackyAINode.Location.Filename));

			yield break;
		}
	}
}
