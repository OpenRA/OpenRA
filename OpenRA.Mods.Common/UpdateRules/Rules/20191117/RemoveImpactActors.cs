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
	public class RemoveImpactActors : UpdateRule
	{
		public override string Name { get { return "Remove ImpactActors boolean from CreateEffect warhead"; } }
		public override string Description
		{
			get
			{
				return "The ImpactActors boolean has been removed for code consistency reasons.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			yield return "Note that due to larger internal refactoring of the warhead code,\n" +
				"certain edge-case setups like submerged subs or aircraft husk explosions\n" +
				"may no longer work as expected. Use the new ValidImpactTypes flag list to reconfigure such setups.\n" +
				"Note that the impact type 'Actor' requires the targeted actor to have a valid target type.\n" +
				"See the RA mods' new setup for examples.";

			if (locations.Any())
				yield return "The following weapons' CreateEffect warhead(s) had ImpactActors: false.\n" +
					"Review their target types and, if necessary, change the target types\n" +
					"to make them trigger on any actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			var addLocation = false;
			foreach (var node in weaponNode.ChildrenMatching("Warhead"))
			{
				if (node.NodeValue<string>() == "CreateEffect")
				{
					foreach (var impactActorsNode in node.ChildrenMatching("ImpactActors"))
						if (!impactActorsNode.NodeValue<bool>())
							addLocation = true;

					node.RemoveNodes("ImpactActors");
				}

				if (addLocation)
					locations.Add("{0} ({1})".F(weaponNode.Key, node.Location.Filename));
			}

			yield break;
		}
	}
}
