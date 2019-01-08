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
	public class RemovedDemolishLocking : UpdateRule
	{
		public override string Name { get { return "Traits are no longer automatically disabled during the Demolition countdown"; } }
		public override string Description
		{
			get
			{
				return "Traits are no longer force-disabled during the Demolishing trait destruction countdown.\n" +
					"This affects the Production*, Transforms, Sellable, EjectOnDeath and ToggleConditionOnOrder traits.\n" +
					"Affected actors are listed so that conditions may be manually defined.";
			}
		}

		static readonly string[] Traits =
		{
			"Production",
			"ProductionAirdrop",
			"ProductionFromMapEdge",
			"ProductionParadrop",
			"Transforms",
			"Sellable",
			"ToggleConditionOnOrder",
			"EjectOnDeath"
		};

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "Review the following definitions and, if the actor is Demolishable,\n" +
				    "define Demolishable.Condition and use this condition to disable them:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var used = new List<string>();
			foreach (var t in Traits)
				if (actorNode.LastChildMatching(t, includeRemovals: false) != null)
					used.Add(t);

			if (used.Any())
			{
				var location = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);
				locations[location] = used;
			}

			yield break;
		}
	}
}
