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
	public class ExtractHackyAIModules : UpdateRule
	{
		public override string Name { get { return "Split HackyAI logic handling to BotModules"; } }
		public override string Description
		{
			get
			{
				return "Most properties and logic are being moved from HackyAI\n" +
					"to *BotModules.";
			}
		}

		bool messageShown;

		readonly string[] harvesterFields =
		{
			"HarvesterEnemyAvoidanceRadius", "AssignRolesInterval"
		};

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "You may want to check your AI yamls for possible redundant module entries.\n" +
				"Additionally, make sure the Player actor has the ConditionManager trait and add it manually if it doesn't.";

			if (!messageShown)
				yield return message;

			messageShown = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "Player")
				yield break;

			var hackyAIs = actorNode.ChildrenMatching("HackyAI");
			if (!hackyAIs.Any())
				yield break;

			var addNodes = new List<MiniYamlNode>();

			// We add a 'default' HarvesterBotModule in any case,
			// and only add more for AIs that define custom values for one of its fields.
			var defaultHarvNode = new MiniYamlNode("HarvesterBotModule", "");

			foreach (var hackyAINode in hackyAIs)
			{
				// HackyAIInfo.Name might contain spaces, so Type is better suited to be used as condition name
				var aiType = hackyAINode.LastChildMatching("Type").NodeValue<string>();
				var conditionString = "enable-" + aiType + "-ai";
				var requiresCondition = new MiniYamlNode("RequiresCondition", conditionString);

				var addGrantConditionOnBotOwner = true;
				var grantBotConditions = actorNode.ChildrenMatching("GrantConditionOnBotOwner");
				foreach (var grant in grantBotConditions)
					if (grant.LastChildMatching("Condition").NodeValue<string>() == conditionString)
						addGrantConditionOnBotOwner = false;

				if (addGrantConditionOnBotOwner)
				{
					var grantNode = new MiniYamlNode("GrantConditionOnBotOwner@" + aiType, "");
					var grantCondition = new MiniYamlNode("Condition", conditionString);
					var bot = new MiniYamlNode("Bots", aiType);
					grantNode.AddNode(grantCondition);
					grantNode.AddNode(bot);
					addNodes.Add(grantNode);
				}

				if (harvesterFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var harvNode = new MiniYamlNode("HarvesterBotModule@" + aiType, "");
					harvNode.AddNode(requiresCondition);

					foreach (var hf in harvesterFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(hf);
						if (fieldNode != null)
						{
							if (hf == "AssignRolesInterval")
								fieldNode.MoveAndRenameNode(hackyAINode, harvNode, "ScanForIdleHarvestersInterval");
							else
								fieldNode.MoveNode(hackyAINode, harvNode);
						}
					}

					addNodes.Add(harvNode);
				}
				else
				{
					// We want the default module to be enabled for every AI that didn't customise one of its fields,
					// so we need to update RequiresCondition to be enabled on any of the conditions granted by these AIs,
					// but only if the condition hasn't been added yet.
					var requiresConditionNode = defaultHarvNode.LastChildMatching("RequiresCondition");
					if (requiresConditionNode == null)
						defaultHarvNode.AddNode(requiresCondition);
					else
					{
						var oldValue = requiresConditionNode.NodeValue<string>();
						if (oldValue.Contains(conditionString))
							continue;

						requiresConditionNode.ReplaceValue(oldValue + " || " + conditionString);
					}
				}
			}

			addNodes.Add(defaultHarvNode);

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
