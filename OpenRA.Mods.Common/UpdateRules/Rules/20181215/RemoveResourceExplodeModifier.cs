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
	public class RemoveResourceExplodeModifier : UpdateRule
	{
		public override string Name { get { return "Harvester and StoresResources traits no longer force Explodes.EmptyWeapon"; } }
		public override string Description
		{
			get
			{
				return "The hardcoded behaviour forcing Explodes to use EmptyWeapon when the harvester/player has no\n" +
					"resources has been removed. Affected actors are listed so that conditions may be manually defined.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "Review the following definitions and, if the actor uses Harvester or StoresResources,\n" +
					"define a new Explodes trait with the previous EmptyWeapon, enabled by Harvester.EmptyCondition or\n" +
					"GrantConditionOnPlayerResources.Condition (negated):\n" + UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.LastChildMatching("Explodes") != null)
				locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));

			yield break;
		}
	}
}
