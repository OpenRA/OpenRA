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

		readonly List<string> locations = new List<string>();
		bool messageShown;

		readonly string[] harvesterFields =
		{
			"HarvesterEnemyAvoidanceRadius", "AssignRolesInterval"
		};

		readonly string[] supportPowerFields =
		{
			"SupportPowerDecisions"
		};

		readonly string[] baseBuilderFields =
		{
			"BuildingQueues",
			"DefenseQueues",
			"MinimumExcessPower",
			"MaximumExcessPower",
			"ExcessPowerIncrement",
			"ExcessPowerIncreaseThreshold",
			"StructureProductionInactiveDelay",
			"StructureProductionActiveDelay",
			"StructureProductionRandomBonusDelay",
			"StructureProductionResumeDelay",
			"MaximumFailedPlacementAttempts",
			"MaxResourceCellsToCheck",
			"CheckForNewBasesDelay",
			"MinBaseRadius",
			"MaxBaseRadius",
			"MinimumDefenseRadius",
			"MaximumDefenseRadius",
			"RallyPointScanRadius",
			"CheckForWaterRadius",
			"WaterTerrainTypes",
			"NewProductionCashThreshold",
			"BuildingCommonNames",
			"BuildingLimits",
			"BuildingFractions",
		};

		readonly string[] copyBaseBuilderFields =
		{
			"MinBaseRadius",
			"MaxBaseRadius",
		};

		readonly string[] captureManagerFields =
		{
			"CapturingActorTypes",
			"CapturableActorTypes",
			"MinimumCaptureDelay",
			"MaximumCaptureTargetOptions",
			"CheckCaptureTargetsForVisibility",
			"CapturableStances",
		};

		readonly string[] squadManagerFields =
		{
			"SquadSize",
			"SquadSizeRandomBonus",
			"AssignRolesInterval",
			"RushInterval",
			"AttackForceInterval",
			"MinimumAttackForceDelay",
			"RushAttackScanRadius",
			"ProtectUnitScanRadius",
			"MaxBaseRadius",
			"MaximumDefenseRadius",
			"IdleScanRadius",
			"DangerScanRadius",
			"AttackScanRadius",
			"ProtectionScanRadius",
			"UnitsCommonNames",
			"BuildingCommonNames",
		};

		readonly string[] squadManagerCommonNames =
		{
			"ConstructionYard",
			"NavalProduction",
		};

		readonly string[] unitBuilderFields =
		{
			"IdleBaseUnitsMaximum",
			"UnitQueues",
			"UnitsToBuild",
			"UnitLimits",
		};

		readonly string[] mcvManagerFields =
		{
			"AssignRolesInterval",
			"MinBaseRadius",
			"MaxBaseRadius",
			"RestrictMCVDeploymentFallbackToBase",
			"UnitsCommonNames",
			"BuildingCommonNames",
		};

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!messageShown)
				yield return "You may want to check your AI yamls for possible redundant module entries.\n" +
					"Additionally, make sure the Player actor has the ConditionManager trait and add it manually if it doesn't.";

			messageShown = true;

			if (locations.Any())
				yield return "This update rule can only autoamtically update the base HackyAI definitions,\n" +
					"not any overrides in other files (unless they redefine Type).\n" +
					"You will have to manually check and possibly update the following locations:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "Player")
				yield break;

			var dummyAIs = actorNode.ChildrenMatching("DummyAI");
			foreach (var dummyAINode in dummyAIs)
				dummyAINode.RenameKey("DummyBot");

			var hackyAIRemovals = actorNode.ChildrenMatching("-HackyAI");
			foreach (var hackyAIRemovalNode in hackyAIRemovals)
				hackyAIRemovalNode.RenameKey("-ModularBot");

			var hackyAIs = actorNode.ChildrenMatching("HackyAI", includeRemovals: false);
			if (!hackyAIs.Any())
				yield break;

			var addNodes = new List<MiniYamlNode>();

			// We add a 'default' HarvesterBotModule in any case (unless the file doesn't contain any HackyAI base definition),
			// and only add more for AIs that define custom values for one of its fields.
			var defaultHarvNode = new MiniYamlNode("HarvesterBotModule", "");

			// We add a 'default' BuildingRepairBotModule in any case,
			// and just don't enable it for AIs that had 'ShouldRepairBuildings: false'.
			var defaultRepairNode = new MiniYamlNode("BuildingRepairBotModule", "");

			foreach (var hackyAINode in hackyAIs)
			{
				// HackyAIInfo.Name might contain spaces, so Type is better suited to be used as condition name.
				// Type can be 'null' if the place we're updating is overriding the default rules (like a map's rules.yaml).
				// If that's the case, it's better to not perform most of the updates on this particular yaml file,
				// as most - or more likely all - necessary updates will already have been performed on the base ai yaml.
				var aiTypeNode = hackyAINode.LastChildMatching("Type");
				var aiType = aiTypeNode != null ? aiTypeNode.NodeValue<string>() : null;
				if (aiType == null)
				{
					locations.Add("{0} ({1})".F(hackyAINode.Key, hackyAINode.Location.Filename));
					continue;
				}

				var conditionString = "enable-" + aiType + "-ai";

				var addGrantConditionOnBotOwner = true;

				// Don't add GrantConditionOnBotOwner if it's already been added with matching condition
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
					harvNode.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

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
					// We want the default harvester module to be enabled for every AI that didn't customise one of its fields,
					// so we need to update RequiresCondition to be enabled on any of the conditions granted by these AIs,
					// but only if the condition hasn't been added yet.
					var requiresConditionNode = defaultHarvNode.LastChildMatching("RequiresCondition");
					if (requiresConditionNode == null)
						defaultHarvNode.AddNode(new MiniYamlNode("RequiresCondition", conditionString));
					else
					{
						var oldValue = requiresConditionNode.NodeValue<string>();
						if (oldValue.Contains(conditionString))
							continue;

						requiresConditionNode.ReplaceValue(oldValue + " || " + conditionString);
					}
				}

				if (supportPowerFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var spNode = new MiniYamlNode("SupportPowerBotModule@" + aiType, "");
					spNode.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var spf in supportPowerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(spf);
						if (fieldNode != null)
							fieldNode.MoveAndRenameNode(hackyAINode, spNode, "Decisions");
					}

					addNodes.Add(spNode);
				}

				if (baseBuilderFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var bmNode = new MiniYamlNode("BaseBuilderBotModule@" + aiType, "");
					bmNode.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var bmf in baseBuilderFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(bmf);
						if (fieldNode != null)
						{
							if (fieldNode.KeyMatches("BuildingFractions", includeRemovals: false))
							{
								var buildingNodes = fieldNode.Value.Nodes;
								foreach (var n in buildingNodes)
									ConvertFractionToInteger(n);
							}

							if (copyBaseBuilderFields.Any(f => f == bmf))
								bmNode.AddNode(fieldNode);
							else if (fieldNode.KeyMatches("BuildingCommonNames", includeRemovals: false))
								foreach (var n in fieldNode.Value.Nodes)
									bmNode.AddNode(n.Key + "Types", n.Value.Value);
							else
								fieldNode.MoveNode(hackyAINode, bmNode);
						}
					}

					addNodes.Add(bmNode);
				}

				// We want the default repair module to be enabled for every AI that didn't disable 'ShouldRepairBuildings',
				// so we need to update RequiresCondition to be enabled on any of the conditions granted by these AIs,
				// but only if the condition hasn't been added yet.
				var shouldRepairNode = hackyAINode.LastChildMatching("ShouldRepairBuildings");
				var enableBuildingRepair = shouldRepairNode == null || shouldRepairNode.NodeValue<bool>();
				if (enableBuildingRepair)
				{
					var requiresConditionNode = defaultRepairNode.LastChildMatching("RequiresCondition");
					if (requiresConditionNode == null)
						defaultRepairNode.AddNode(new MiniYamlNode("RequiresCondition", conditionString));
					else
					{
						var oldValue = requiresConditionNode.NodeValue<string>();
						if (oldValue.Contains(conditionString))
							continue;

						requiresConditionNode.ReplaceValue(oldValue + " || " + conditionString);
					}
				}

				if (captureManagerFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var node = new MiniYamlNode("CaptureManagerBotModule@" + aiType, "");
					node.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var field in captureManagerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(field);
						if (fieldNode != null)
							fieldNode.MoveNode(hackyAINode, node);
					}

					addNodes.Add(node);
				}

				if (squadManagerFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var node = new MiniYamlNode("SquadManagerBotModule@" + aiType, "");
					node.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var field in squadManagerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(field);
						if (fieldNode != null)
						{
							if (fieldNode.KeyMatches("UnitsCommonNames", includeRemovals: false))
							{
								var mcvNode = fieldNode.LastChildMatching("Mcv");
								var excludeNode = fieldNode.LastChildMatching("ExcludeFromSquads");
								var navalUnitsNode = fieldNode.LastChildMatching("NavalUnits");

								// In the old code, actors listed under Mcv were also excluded from squads.
								// However, Mcv[Types] is moved to McvManagerBotModule now, so we need to add them under ExcludeFromSquads as well.
								if (excludeNode == null && mcvNode != null)
									node.AddNode("ExcludeFromSquadsTypes", mcvNode.Value.Value);
								else if (excludeNode != null && mcvNode != null)
								{
									var mcvValue = mcvNode.NodeValue<string>();
									var excludeValue = excludeNode.NodeValue<string>();
									node.AddNode("ExcludeFromSquadsTypes", excludeValue + ", " + mcvValue);
								}

								if (navalUnitsNode != null)
									node.AddNode("NavalUnitsTypes", navalUnitsNode.Value.Value);
							}
							else if (fieldNode.KeyMatches("BuildingCommonNames", includeRemovals: false))
							{
								foreach (var b in fieldNode.Value.Nodes)
									if (squadManagerCommonNames.Any(f => f == b.Key))
										node.AddNode(b.Key + "Types", b.Value.Value);
							}
							else if (fieldNode.KeyMatches("AssignRolesInterval") || fieldNode.KeyMatches("MaxBaseRadius"))
								node.AddNode(fieldNode.Key, fieldNode.Value.Value);
							else
								fieldNode.MoveNode(hackyAINode, node);
						}
					}

					addNodes.Add(node);
				}

				if (unitBuilderFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var node = new MiniYamlNode("UnitBuilderBotModule@" + aiType, "");
					node.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var field in unitBuilderFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(field);
						if (fieldNode != null)
						{
							if (fieldNode.KeyMatches("UnitsToBuild", includeRemovals: false))
							{
								var unitNodes = fieldNode.Value.Nodes;
								foreach (var n in unitNodes)
									ConvertFractionToInteger(n);
							}

							fieldNode.MoveNode(hackyAINode, node);
						}
					}

					addNodes.Add(node);
				}

				if (mcvManagerFields.Any(f => hackyAINode.ChildrenMatching(f).Any()))
				{
					var node = new MiniYamlNode("McvManagerBotModule@" + aiType, "");
					node.AddNode(new MiniYamlNode("RequiresCondition", conditionString));

					foreach (var field in mcvManagerFields)
					{
						var fieldNode = hackyAINode.LastChildMatching(field);
						if (fieldNode != null)
						{
							if (fieldNode.KeyMatches("UnitsCommonNames", includeRemovals: false))
							{
								var mcvNode = fieldNode.LastChildMatching("Mcv");
								if (mcvNode != null)
									mcvNode.MoveAndRenameNode(hackyAINode, node, "McvTypes");

								// Nothing left that needs UnitCommonNames, so we can finally remove it
								hackyAINode.RemoveNode(fieldNode);
							}
							else if (fieldNode.KeyMatches("BuildingCommonNames", includeRemovals: false))
							{
								foreach (var n in fieldNode.Value.Nodes)
								{
									if (n.KeyMatches("VehiclesFactory"))
										node.AddNode("McvFactoryTypes", n.Value.Value);
									else if (n.KeyMatches("ConstructionYard"))
										node.AddNode("ConstructionYardTypes", n.Value.Value);
								}

								// Nothing left that needs BuildingCommonNames, so we can finally remove it
								hackyAINode.RemoveNode(fieldNode);
							}
							else
								fieldNode.MoveNode(hackyAINode, node);
						}
					}

					addNodes.Add(node);
				}

				hackyAINode.RenameKey("ModularBot");
			}

			// Only add module if any bot is using/enabling it.
			var harvRequiresConditionNode = defaultHarvNode.LastChildMatching("RequiresCondition");
			if (harvRequiresConditionNode != null)
				addNodes.Add(defaultHarvNode);

			// Only add module if any bot is using/enabling it.
			var repRequiresConditionNode = defaultRepairNode.LastChildMatching("RequiresCondition");
			if (repRequiresConditionNode != null)
				addNodes.Add(defaultRepairNode);

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}

		void ConvertFractionToInteger(MiniYamlNode node)
		{
			// Is the value a percentage or a 'real' float?
			var isPercentage = node.NodeValue<string>().Contains("%");
			if (isPercentage)
			{
				// Remove '%' first, then remove potential '.' and finally clamp to minimum of 1, unless the old value was really zero
				var oldValueAsString = node.NodeValue<string>().Split('%')[0];
				var oldValueWasZero = oldValueAsString == "0";
				var newValue = oldValueAsString.Split('.')[0];
				newValue = !oldValueWasZero && newValue == "0" ? "1" : newValue;

				node.ReplaceValue(newValue);
			}
			else
			{
				var oldValueAsFloat = node.NodeValue<float>();
				var oldValueWasZero = node.NodeValue<string>() == "0" || node.NodeValue<string>() == "0.0";
				var newValue = (int)(oldValueAsFloat * 100);

				// Clamp to minimum of 1, unless the old value was really zero
				newValue = !oldValueWasZero && newValue == 0 ? 1 : newValue;

				node.ReplaceValue(newValue.ToString());
			}
		}
	}
}
