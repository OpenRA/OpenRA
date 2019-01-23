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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveAttackSuicides : UpdateRule
	{
		public override string Name { get { return "AttackSuicides trait has been removed."; } }
		public override string Description
		{
			get
			{
				return "The AttackSuicides trait has been removed, and should be replaced by\n" +
				       "AttackFrontal + GrantConditionOnAttack + GrantConditionOnDeploy + a dummy\n" +
				       "weapon for targeting. Affected actors are listed so that these traits can be defined.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The AttackSuicides trait has been removed from the following actors.\n" +
				             "You must manually define AttackFrontal, GrantConditionOnAttack, GrantConditionOnDeploy\n" +
				             "traits and create a dummy weapon to use for targeting:\n" +
				             UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("AttackSuicides") > 0)
				locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));

			yield break;
		}
	}
}
