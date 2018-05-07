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
	public class AddCaptureBotModule : UpdateRule
	{
		public override string Name { get { return "Split HackyAI capture handling to CaptureBotModule"; } }
		public override string Description
		{
			get
			{
				return "All properties and handling related to bot capturing have been moved\n" +
					"from HackyAI to the new CaptureBotModule.";
			}
		}

		bool messageShown;

		readonly string[] captureFields =
		{
			"MinimumCaptureDelay", "MaximumCaptureTargetOptions",
			"CheckCaptureTargetsForVisibility", "CapturableStances",
			"CapturingActorTypes", "CapturableActorTypes"
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

			foreach (var hackyAINode in hackyAIs)
			{
				// Name might contain spaces, so Type is better suited to be used as suffix on the module name
				var aiType = hackyAINode.LastChildMatching("Type").Value.Value;
				if (captureFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var captNode = new MiniYamlNode("CaptureBotModule@" + aiType, "");
					var captNodeName = new MiniYamlNode("Name", "capture-module-" + aiType);
					captNode.AddNode(captNodeName);
					var aiModulesNode = hackyAINode.LastChildMatching("Modules");
					if (aiModulesNode == null)
					{
						var enableModule = new MiniYamlNode("Modules", "capture-module-" + aiType);
						hackyAINode.AddNode(enableModule);
					}
					else
						aiModulesNode.ReplaceValue(aiModulesNode.NodeValue<string>() + ", capture-module-" + aiType);

					foreach (var cf in captureFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(cf);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, captNode);
					}

					addNodes.Add(captNode);
				}
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
