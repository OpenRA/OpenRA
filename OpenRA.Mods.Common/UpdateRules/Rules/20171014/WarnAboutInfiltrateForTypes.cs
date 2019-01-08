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
	public class WarnAboutInfiltrateForTypes : UpdateRule
	{
		public override string Name { get { return "Introduced Types field to InfiltrateFor* traits"; } }
		public override string Description
		{
			get
			{
				return "InfiltrateFor* traits now have a Types field and infiltration will only have the desired\n" +
					"effect if the Types include the type  of the infiltrator.";
			}
		}

		readonly string[] infiltrateForTraits =
		{
			"InfiltrateForCash", "InfiltrateForDecoration",
			"InfiltrateForExploration", "InfiltrateForPowerOutage",
			"InfiltrateForSupportPower",
		};

		readonly List<Tuple<string, string>> infiltrateForLocations = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message1 = "You need to define Types on the InfiltrateFor* trait(s) on the following actors:\n"
				+ UpdateUtils.FormatMessageList(infiltrateForLocations.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (infiltrateForLocations.Any())
				yield return message1;

			infiltrateForLocations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var t in infiltrateForTraits)
			{
				if (actorNode.LastChildMatching(t) != null)
				{
					infiltrateForLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
					yield break;
				}
			}

			yield break;
		}
	}
}
