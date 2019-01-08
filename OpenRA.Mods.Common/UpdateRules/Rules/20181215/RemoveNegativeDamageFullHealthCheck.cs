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
	public class RemoveNegativeDamageFullHealthCheck : UpdateRule
	{
		public override string Name { get { return "Negative damage weapons are now valid against full-health targets."; } }
		public override string Description
		{
			get
			{
				return "Negative-damage weapons are no longer automatically invalid against targets that have full health.\n" +
				       "Previous behaviour can be restored by enabling a Targetable trait using GrantConditionOnDamageState.\n" +
				       "Affected weapons are listed so that conditions may be manually defined.";
			}
		}

		static readonly string[] DamageWarheads =
		{
			"TargetDamage",
			"SpreadDamage",
			"HealthPercentageDamage"
		};

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The following weapons may now target actors that have full health. Review their\n" +
					"target types and, if necessary, use GrantConditionOnDamageState to enable\n" +
					"a conditional Targetable trait with the appropriate target type when damaged:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			var used = new List<string>();
			foreach (var node in weaponNode.ChildrenMatching("Warhead"))
			{
				foreach (var warhead in DamageWarheads)
					if (node.NodeValue<string>() == warhead && node.ChildrenMatching("Damage").Any(d => d.NodeValue<int>() < 0))
						used.Add(node.Key);

				if (used.Any())
				{
					var location = "{0} ({1})".F(weaponNode.Key, node.Location.Filename);
					locations[location] = used;
				}
			}

			yield break;
		}
	}
}
