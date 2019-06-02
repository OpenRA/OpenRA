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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ExtractBotProductionConsiderations : UpdateRule
	{
		public override string Name { get { return "Extract BotProductionConsiderations"; } }
		public override string Description
		{
			get
			{
				return "UnitsToBuild/UnitLimits/UnitDelays have been extracted from UnitBuilderBotModule\n" +
				"and BuildingFractions/BuildingLimits/BuildingDelays from BaseBuilderBotModule\n" +
				"into a new BotProductionConsideration trait.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var actors = new Dictionary<string, MiniYaml>();
			var addNodes = new List<MiniYamlNode>();

			var modules = new List<Tuple<string, string, string, string, string[], string>>
			{
				new Tuple<string, string, string, string, string[], string>(
					"UnitBuilderBotModule", "UnitsToBuild", "UnitLimits", "UnitDelays",
					new[] { "UnitQueues" }, "units"),
				new Tuple<string, string, string, string, string[], string>(
					"BaseBuilderBotModule", "BuildingFractions", "BuildingLimits", "BuildingDelays",
					new[] { "BuildingQueues", "DefenseQueues" }, "buildings")
			};

			foreach (var module in modules)
			{
				foreach (var moduleNode in actorNode.ChildrenMatching(module.Item1))
				{
					actors.Clear();

					var fractionsNode = moduleNode.LastChildMatching(module.Item2);
					if (fractionsNode != null)
					{
						foreach (var kv in fractionsNode.Value.Nodes)
							actors[kv.Key] = new MiniYaml(kv.Value.Value);
						moduleNode.RemoveNode(fractionsNode);
					}

					var limitsNode = moduleNode.LastChildMatching(module.Item3);
					if (limitsNode != null)
					{
						MiniYaml sharesNode;
						foreach (var kv in limitsNode.Value.Nodes)
							if (actors.TryGetValue(kv.Key, out sharesNode))
								sharesNode.Nodes.Add(new MiniYamlNode("Limit", kv.Value.Value));
						moduleNode.RemoveNode(limitsNode);
					}

					var delayNode = moduleNode.LastChildMatching(module.Item4);
					if (delayNode != null)
					{
						MiniYaml sharesNode;
						foreach (var kv in delayNode.Value.Nodes)
							if (actors.TryGetValue(kv.Key, out sharesNode))
								sharesNode.Nodes.Add(new MiniYamlNode("Delay", kv.Value.Value));
						moduleNode.RemoveNode(delayNode);
					}

					var requiresNode = moduleNode.LastChildMatching("RequiresCondition");

					var considerationKey = "BotProductionConsideration@" + module.Item6;
					var suffix = moduleNode.KeySuffix();
					if (!string.IsNullOrEmpty(suffix))
						considerationKey += "-" + suffix;

					var considerationNodes = new List<MiniYamlNode>();
					if (requiresNode != null)
						considerationNodes.Add(requiresNode);

					var queues = new HashSet<string>();
					foreach (var queueKey in module.Item5)
					{
						var queueNode = moduleNode.LastChildMatching(queueKey);
						if (queueNode != null)
							foreach (var queue in FieldLoader.GetValue<string[]>(queueKey, queueNode.Value.Value))
								queues.Add(queue);
					}

					considerationNodes.Add(new MiniYamlNode("Queues", FieldSaver.FormatValue(queues)));

					considerationNodes.Add(new MiniYamlNode("Actors", "", actors.Select(
						kv => new MiniYamlNode(kv.Key, kv.Value)).ToList()));

					addNodes.Add(new MiniYamlNode(considerationKey, "", considerationNodes));
				}
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
