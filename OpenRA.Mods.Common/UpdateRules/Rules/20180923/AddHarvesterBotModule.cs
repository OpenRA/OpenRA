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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddHarvesterBotModule : UpdateRule
	{
		public override string Name { get { return "Split HackyAI harvester handling to HarvesterBotModule"; } }
		public override string Description
		{
			get
			{
				return "Some properties and all harvester handling have been moved from the HackyAI trait\n" +
					"to the new HarvesterBotModule.";
			}
		}

		bool messageShown;

		readonly string[] harvesterFields =
		{
			"HarvesterEnemyAvoidanceRadius", "AssignRolesInterval"
		};

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "You may want to check your AI yamls for possible redundant module entries.";

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
			addNodes.Add(defaultHarvNode);

			foreach (var hackyAINode in hackyAIs)
			{
				// Name might contain spaces, so Type is better suited to be used as suffix on the module name
				var aiType = hackyAINode.LastChildMatching("Type").Value.Value;

				if (harvesterFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var harvNode = new MiniYamlNode("HarvesterBotModule@" + aiType, "");
					var harvNodeName = new MiniYamlNode("Name", "harvester-module-" + aiType);
					harvNode.AddNode(harvNodeName);
					var aiModulesNode = hackyAINode.LastChildMatching("Modules");
					if (aiModulesNode == null)
					{
						var enableModule = new MiniYamlNode("Modules", "harvester-module-" + aiType);
						hackyAINode.AddNode(enableModule);
					}
					else
						aiModulesNode.ReplaceValue(aiModulesNode.NodeValue<string>() + ", harvester-module-" + aiType);

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
					// Technically we have no guarantee that all *BotModule update rules are run in the expected order,
					// so even though it might end up being redundant due to the internal default(s),
					// we add 'Modules: harvester-module' here if 'Modules' isn't already present,
					// and just add 'harvester-module' to the end of the list if it is.
					var aiModulesNode = hackyAINode.LastChildMatching("Modules");
					if (aiModulesNode == null)
					{
						var enableModule = new MiniYamlNode("Modules", "harvester-module");
						hackyAINode.AddNode(enableModule);
					}
					else
						aiModulesNode.ReplaceValue(aiModulesNode.NodeValue<string>() + ", harvester-module");
				}
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
