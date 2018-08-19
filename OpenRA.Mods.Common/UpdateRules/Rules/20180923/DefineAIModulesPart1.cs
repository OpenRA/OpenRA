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
	public class DefineAIModulesPart1 : UpdateRule
	{
		public override string Name { get { return "Introduce AI modules for various aspects"; } }
		public override string Description
		{
			get
			{
				return "A large number of properties have been moved from the HackyAI trait\n" +
					"to their respective new *Module traits. New Modules are:\n" +
					"AIHarvesterModule, AICaptureModule, AISupportPowerModule and AIDamageReactionModule.";
			}
		}

		bool messageShown;

		readonly string[] harvesterFields =
		{
			"HarvesterEnemyAvoidanceRadius"
		};

		readonly string[] captureFields =
		{
			"MinimumCaptureDelay", "MaximumCaptureTargetOptions",
			"CheckCaptureTargetsForVisibility", "CapturableStances",
			"CapturingActorTypes", "CapturableActorTypes"
		};

		readonly string[] supportPowerFields =
		{
			"SupportPowerDecisions"
		};

		readonly string[] damageReactionFields =
		{
			"ProtectUnitScanRadius", "ShouldRepairBuildings"
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

			// We add 'default' AIHarvesterModule and AIDamageReactionModule modules in any case,
			// and only add more for AIs that define custom values for their respective fields.
			// We don't do that for the other two module types because in their case, no custom values means they're disabled for that AI.
			var defaultHarvNode = new MiniYamlNode("AIHarvesterModule", "");
			var defaultDamageReactionNode = new MiniYamlNode("AIDamageReactionModule", "");
			addNodes.Add(defaultHarvNode);
			addNodes.Add(defaultDamageReactionNode);

			foreach (var hackyAINode in hackyAIs)
			{
				var aiName = hackyAINode.LastChildMatching("Name").Value.Value;

				if (harvesterFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var harvNode = new MiniYamlNode("AIHarvesterModule@" + aiName, "");
					var harvNodeName = new MiniYamlNode("Name", aiName);
					harvNode.AddNode(harvNodeName);
					var enableModule = new MiniYamlNode("HarvesterModule", aiName);
					hackyAINode.AddNode(enableModule);

					foreach (var hf in harvesterFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(hf);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, harvNode);
					}

					addNodes.Add(harvNode);
				}

				if (captureFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var captNode = new MiniYamlNode("AICaptureModule@" + aiName, "");
					var captNodeName = new MiniYamlNode("Name", aiName);
					captNode.AddNode(captNodeName);
					var enableModule = new MiniYamlNode("CaptureModule", aiName);
					hackyAINode.AddNode(enableModule);

					foreach (var cf in captureFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(cf);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, captNode);
					}

					addNodes.Add(captNode);
				}

				if (supportPowerFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var spNode = new MiniYamlNode("AISupportPowerModule@" + aiName, "");
					var spNodeName = new MiniYamlNode("Name", aiName);
					spNode.AddNode(spNodeName);
					var enableModule = new MiniYamlNode("SupportPowerModule", aiName);
					hackyAINode.AddNode(enableModule);

					foreach (var spf in supportPowerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(spf);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, spNode);
					}

					addNodes.Add(spNode);
				}

				if (damageReactionFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var drNode = new MiniYamlNode("AIDamageReactionModule@" + aiName, "");
					var drNodeName = new MiniYamlNode("Name", aiName);
					drNode.AddNode(drNodeName);
					var enableModule = new MiniYamlNode("DamageReactionModule", aiName);
					hackyAINode.AddNode(enableModule);

					foreach (var drf in supportPowerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(drf);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, drNode);
					}

					addNodes.Add(drNode);
				}
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
