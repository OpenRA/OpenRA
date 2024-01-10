#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class AbstractDocking : UpdateRule, IBeforeUpdateActors
	{
		readonly string[] moveRefineyValues = { "DockAngle", "IsDragRequired", "DragOffset", "DragLength" };
		readonly string[] moveHarvesterValues = { "EnterCursor", "EnterBlockedCursor" };
		readonly string[] moveRepairableNearValues = { "Voice", "RequireForceMoveCondition", "EnterCursor", "EnterBlockedCursor" };
		readonly string[] buildings = { "Building", "D2kBuilding" };
		readonly string[,] moveAndRenameHarvesterValues = new string[4, 2]
		{
			{ "DeliverVoice", "Voice" },
			{ "DeliverLineColor", "LinkLineColor" },
			{ "UnloadQueueCostModifier", "OccupancyCostModifier" },
			{ "SearchForDeliveryBuildingDelay", "SearchForLinkDelay" }
		};

		readonly Dictionary<string, List<MiniYamlNodeBuilder>> refineryNodes = new();
		readonly Dictionary<string, List<MiniYamlNodeBuilder>> repairableNearNodes = new();
		public override string Name => "Docking was abstracted from Refinery & Harvester & RepairableNear.";

		public override string Description =>
			"Properties moved from Refinery to new trait DockHost,\n" +
			"Properties moved from Harvester to new trait LinkClientManager and to DockHost\n" +
			"Properties moved from RepairableNear to LinkClientManager and LinkOnRepairCursor.";

		public IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			grid = modData.Manifest.Get<MapGrid>();
			var harvesters = new Dictionary<string, HashSet<string>>();
			var refineries = new List<string>();
			var repairableNears = new Dictionary<string, HashSet<string>>();
			var repairNearBuilding = new Dictionary<string, WDist>();
			foreach (var actorNode in resolvedActors)
			{
				var harvesterNode = actorNode.ChildrenMatching("Harvester", includeRemovals: false).FirstOrDefault();
				if (harvesterNode != null)
					harvesters[actorNode.Key] = harvesterNode.ChildrenMatching("DeliveryBuildings", includeRemovals: false)
						.FirstOrDefault()?.NodeValue<HashSet<string>>() ?? new HashSet<string>();

				if (actorNode.ChildrenMatching("Refinery", includeRemovals: false).Any())
					refineries.Add(actorNode.Key.ToLowerInvariant());

				var repairableNearNode = actorNode.ChildrenMatching("RepairableNear", includeRemovals: false).FirstOrDefault();
				if (repairableNearNode != null)
				{
					var repair = repairableNearNode.ChildrenMatching("RepairActors", includeRemovals: false).FirstOrDefault()?.NodeValue<HashSet<string>>();
					var closeEnough = repairableNearNode.ChildrenMatching("CloseEnough", includeRemovals: false).FirstOrDefault()?.NodeValue<WDist>() ?? WDist.FromCells(4);
					if (repair != null)
					{
						repairableNears[actorNode.Key] = repair;
						foreach (var repairBuilding in repair)
						{
							var repairBuildingLower = repairBuilding.ToLowerInvariant();
							if (repairNearBuilding.TryGetValue(repairBuildingLower, out var value))
							{
								if (value > closeEnough)
									repairNearBuilding[repairBuildingLower] = closeEnough;
							}
							else
								repairNearBuilding.Add(repairBuildingLower, closeEnough);
						}
					}
				}
			}

			foreach (var harvester in harvesters)
			{
				foreach (var deliveryBuildingHigh in harvester.Value)
				{
					var deliveryBuilding = deliveryBuildingHigh.ToLowerInvariant();
					foreach (var refinery in refineries)
					{
						if (refinery == deliveryBuilding)
						{
							if (!refineryNodes.ContainsKey(refinery))
								refineryNodes[refinery] = new List<MiniYamlNodeBuilder>();

							var node = new MiniYamlNodeBuilder("Type", deliveryBuilding);
							if (!refineryNodes[refinery].Any(n => n.Key == node.Key))
								refineryNodes[refinery].Add(node);
						}
					}
				}
			}

			foreach (var repairBuilding in repairNearBuilding)
			{
				repairableNearNodes[repairBuilding.Key] = new List<MiniYamlNodeBuilder>
				{
					new("Type", repairBuilding.Key),
					new("Range", FieldSaver.FormatValue(repairBuilding.Value))
				};
			}

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("Cloak"))
				foreach (var uncloak in node.ChildrenMatching("UncloakOn"))
					uncloak.Value.Value = FieldSaver.FormatValue(uncloak.NodeValue<string[]>()
						.Select(v => v == "Dock" ? "Link" : v).ToArray());

			var refineryNode = actorNode.ChildrenMatching("Refinery", includeRemovals: false).FirstOrDefault();
			if (refineryNode != null)
			{
				var linkNode = new MiniYamlNodeBuilder("LinkHost", "");

				var lowActorName = actorNode.Key.ToLowerInvariant();
				if (!refineryNodes.TryGetValue(lowActorName, out var nodes) || !nodes.Any(n => n.Key == "Type"))
					linkNode.AddNode("Type", "Unload");
				else
					linkNode.AddNode(nodes.First(n => n.Key == "Type"));

				foreach (var value in moveRefineyValues)
				{
					foreach (var node in refineryNode.ChildrenMatching(value).ToList())
					{
						linkNode.AddNode(node);
						refineryNode.RemoveNode(node);
					}
				}

				var oldOffset = CVec.Zero;
				var dockOffsetNode = refineryNode.ChildrenMatching("DockOffset", includeRemovals: false).FirstOrDefault();
				if (dockOffsetNode != null)
				{
					oldOffset = dockOffsetNode.NodeValue<CVec>();
					refineryNode.RemoveNode(dockOffsetNode);
				}

				var buildingNode = actorNode.Value.Nodes.FirstOrDefault(n => buildings.Any(b => n.KeyMatches(b, includeRemovals: false)));
				if (buildingNode != null)
				{
					var dimensions = buildingNode.ChildrenMatching("Dimensions", includeRemovals: false).FirstOrDefault()?.NodeValue<CVec>() ?? new CVec(1, 1);
					var localCenterOffset = buildingNode.ChildrenMatching("LocalCenterOffset", includeRemovals: false).FirstOrDefault()?.NodeValue<WVec>() ?? WVec.Zero;

					var offset = CenterOfCell(oldOffset) - CenterOfCell(CVec.Zero) - BuildingCenter(dimensions, localCenterOffset);
					if (offset != WVec.Zero)
						linkNode.AddNode("DockOffset", offset);
				}

				actorNode.AddNode(linkNode);
			}

			MiniYamlNodeBuilder linkClientNode = null;
			var harvesterNode = actorNode.ChildrenMatching("Harvester", includeRemovals: false).FirstOrDefault();
			if (harvesterNode != null)
			{
				linkClientNode = new MiniYamlNodeBuilder("LinkClientManager", "");

				foreach (var value in moveHarvesterValues)
				{
					foreach (var node in harvesterNode.ChildrenMatching(value).ToList())
					{
						linkClientNode.AddNode(node);
						harvesterNode.RemoveNode(node);
					}
				}

				for (var i = 0; i < moveAndRenameHarvesterValues.GetLength(0); i++)
				{
					foreach (var node in harvesterNode.ChildrenMatching(moveAndRenameHarvesterValues[i, 0]).ToList())
					{
						harvesterNode.RemoveNode(node);
						node.RenameKey(moveAndRenameHarvesterValues[i, 1]);
						linkClientNode.AddNode(node);
					}
				}

				harvesterNode.RenameChildrenMatching("DeliveryBuildings", "LinkType");
				harvesterNode.RemoveNodes("MaxUnloadQueue");

				actorNode.AddNode(linkClientNode);
			}

			var repairableNearNode = actorNode.ChildrenMatching("RepairableNear", includeRemovals: false).FirstOrDefault();
			if (repairableNearNode != null)
			{
				linkClientNode ??= new MiniYamlNodeBuilder("LinkClientManager", "");

				foreach (var value in moveRepairableNearValues)
				{
					foreach (var node in repairableNearNode.ChildrenMatching(value).ToList())
					{
						linkClientNode.AddNode(node);
						repairableNearNode.RemoveNode(node);
					}
				}

				repairableNearNode.RemoveNodes("CloseEnough");
				repairableNearNode.RenameKey("RepairableLinkClient");

				var repairActorsNode = repairableNearNode.ChildrenMatching("RepairActors", includeRemovals: false).FirstOrDefault();
				repairActorsNode?.RenameKey("LinkType");

				actorNode.AddNode(linkClientNode);

				var repairCursor = new MiniYamlNodeBuilder("LinkOnRepairCursor", "");
				repairCursor.AddNode("Type", repairActorsNode.Value.Value);
				actorNode.AddNode(repairCursor);
			}

			var atPosition = actorNode.Key.IndexOf('@');
			var actorName = atPosition > 0 ? actorNode.Key[..atPosition].ToLowerInvariant() : actorNode.Key.ToLowerInvariant();
			var repairNearActor = repairableNearNodes.FirstOrDefault(n => n.Key == actorName);
			if (repairNearActor.Key != null)
			{
				var linkHostNode = new MiniYamlNodeBuilder("ProximityLinkHost", "");
				linkHostNode.Value.Nodes.Add(repairNearActor.Value[0]);
				linkHostNode.Value.Nodes.Add(repairNearActor.Value[1]);
				actorNode.AddNode(linkHostNode);
			}

			yield break;
		}

		MapGrid grid;
		public WVec CenterOfCell(CVec cell)
		{
			if (grid.Type == MapGridType.Rectangular)
				return new WVec(1024 * cell.X + 512, 1024 * cell.Y + 512, 0);

			return new WVec(724 * (cell.X - cell.Y + 1), 724 * (cell.X + cell.Y + 1), 0);
		}

		public WVec BuildingCenter(CVec dimensions, WVec localCenterOffset)
		{
			return (CenterOfCell(dimensions) - CenterOfCell(new CVec(1, 1))) / 2 + localCenterOffset;
		}
	}
}
