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
	public class SplitHarvesterSpriteBody : UpdateRule
	{
		public override string Name { get { return "Split fullness display from WithHarvestAnimation to new WithHarvesterSpriteBody"; } }
		public override string Description
		{
			get
			{
				return "WithHarvestAnimation.PrefixByFullness logic was moved to a dedicated WithHarvesterSpriteBody.";
			}
		}

		readonly List<Tuple<string, string>> fullnessPrefixes = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "PrefixByFullness has been removed from WithHarvestAnimation.\n"
				+ "To display fullness levels, use the new WithHarvesterSpriteBody\n"
				+ "to switch between separate image sprites instead (see RA mod harvester for reference).\n"
				+ "The following places most likely need manual changes:\n"
				+ UpdateUtils.FormatMessageList(fullnessPrefixes.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (fullnessPrefixes.Any())
				yield return message;

			fullnessPrefixes.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var harvAnim = actorNode.LastChildMatching("WithHarvestAnimation");
			if (harvAnim != null)
			{
				var fullnessPrefix = harvAnim.LastChildMatching("PrefixByFullness");

				// If PrefixByFullness is empty, no changes are needed.
				if (fullnessPrefix == null)
					yield break;

				harvAnim.RemoveNode(fullnessPrefix);

				fullnessPrefixes.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
			}

			yield break;
		}
	}
}
