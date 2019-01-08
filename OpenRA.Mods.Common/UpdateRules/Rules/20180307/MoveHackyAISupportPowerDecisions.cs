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
	public class MoveHackyAISupportPowerDecisions : UpdateRule
	{
		public override string Name { get { return "Move HackyAI SupportPowerDecisions to a trait property"; } }
		public override string Description
		{
			get
			{
				return "The SupportPowerDefinitions on HackyAI are moved from top-level trait properties\n" +
					"to children of a single SupportPowerDecisions property.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var hackyAINode in actorNode.ChildrenMatching("HackyAI"))
			{
				var children = hackyAINode.ChildrenMatching("SupportPowerDecision");
				if (!children.Any())
					continue;

				var parent = hackyAINode.LastChildMatching("SupportPowerDecisions");
				if (parent == null)
				{
					parent = new MiniYamlNode("SupportPowerDecisions", "");
					hackyAINode.AddNode(parent);
				}

				foreach (var child in children.ToList())
				{
					var split = child.Key.Split('@');
					child.MoveAndRenameNode(hackyAINode, parent, split.Length > 1 ? split[1] : "Default", false);
				}
			}

			yield break;
		}
	}
}
