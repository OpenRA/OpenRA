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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveAttackIgnoresVisibility : UpdateRule
	{
		public override string Name { get { return "IgnoresVisibility has been removed from Attack* traits."; } }
		public override string Description
		{
			get
			{
				return "The IgnoresVisibility flag has been removed from the Attack* traits as part of a\n" +
				       "wider rework of the fog-targeting behaviour. Mods that rely on this logic must\n" +
				       "implement their own Attack* trait, similar to the AttackSwallow trait.";
			}
		}

		static readonly string[] Traits =
		{
			"AttackFrontal",
			"AttackFollow",
			"AttackTurreted",
			"AttackOmni",
			"AttackBomber",
			"AttackPopupTurreted",
			"AttackTesla",
			"AttackSwallow"
		};

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The IgnoresVisibility flag has been removed from the targeting logic on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations) + "\n\n" +
					"You may wish to enable TargetFrozenActors, or implement a custom Attack* trait like AttackSwallow\n" +
					"if you require visibility to be completely ignored.";

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var trait in Traits)
				foreach (var t in actorNode.ChildrenMatching(trait))
					if (t.RemoveNodes("IgnoresVisibility") > 0)
						locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));

			yield break;
		}
	}
}
