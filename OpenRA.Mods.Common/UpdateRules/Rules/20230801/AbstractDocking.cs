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
		readonly string[] buildings = { "Building", "D2kBuilding" };
		readonly string[,] moveAndRenameHarvesterValues = new string[4, 2]
		{
			{ "DeliverVoice", "Voice" },
			{ "DeliverLineColor", "DockLineColor" },
			{ "UnloadQueueCostModifier", "OccupancyCostModifier" },
			{ "SearchForDeliveryBuildingDelay", "SearchForDockDelay" }
		};

		readonly Dictionary<string, List<MiniYamlNodeBuilder>> refineryNodes = new();
		public override string Name => "Docking was abstracted from Refinery & Harvester.";

		public override string Description =>
			"Fields moved from Refinery to new trait DockHost, fields moved from Harvester to new trait DockClientManager and to DockHost";

		public IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			grid = modData.Manifest.Get<MapGrid>();
			var harvesters = new Dictionary<string, HashSet<string>>();
			var refineries = new List<string>();
			foreach (var actorNode in resolvedActors)
			{
				var harvesterNode = actorNode.ChildrenMatching("Harvester", includeRemovals: false).FirstOrDefault();
				if (harvesterNode != null)
					harvesters[actorNode.Key] = harvesterNode.ChildrenMatching("DeliveryBuildings", includeRemovals: false)
						.FirstOrDefault()?.NodeValue<HashSet<string>>() ?? new HashSet<string>();

				if (actorNode.ChildrenMatching("Refinery", includeRemovals: false).FirstOrDefault() != null)
					refineries.Add(actorNode.Key.ToLowerInvariant());
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

							var node = new MiniYamlNodeBuilder("Type", deliveryBuilding.ToString());
							if (!refineryNodes[refinery].Any(n => n.Key == node.Key))
								refineryNodes[refinery].Add(node);
						}
					}
				}
			}

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			var refineryNode = actorNode.ChildrenMatching("Refinery", includeRemovals: false).FirstOrDefault();
			if (refineryNode != null)
			{
				var dockNode = new MiniYamlNodeBuilder("DockHost", "");

				var lowActorName = actorNode.Key.ToLowerInvariant();
				if (!refineryNodes.ContainsKey(lowActorName) || !refineryNodes[lowActorName].Any(n => n.Key == "Type"))
					dockNode.AddNode("Type", "Unload");
				else
					dockNode.AddNode(refineryNodes[lowActorName].First(n => n.Key == "Type"));

				foreach (var value in moveRefineyValues)
				{
					foreach (var node in refineryNode.ChildrenMatching(value).ToList())
					{
						dockNode.AddNode(node);
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
						dockNode.AddNode("DockOffset", offset);
				}

				actorNode.AddNode(dockNode);
			}

			var harvesterNode = actorNode.ChildrenMatching("Harvester", includeRemovals: false).FirstOrDefault();
			if (harvesterNode != null)
			{
				var dockClientNode = new MiniYamlNodeBuilder("DockClientManager", "");

				foreach (var value in moveHarvesterValues)
				{
					foreach (var node in harvesterNode.ChildrenMatching(value).ToList())
					{
						dockClientNode.AddNode(node);
						harvesterNode.RemoveNode(node);
					}
				}

				for (var i = 0; i < moveAndRenameHarvesterValues.GetLength(0); i++)
				{
					foreach (var node in harvesterNode.ChildrenMatching(moveAndRenameHarvesterValues[i, 0]).ToList())
					{
						harvesterNode.RemoveNode(node);
						node.RenameKey(moveAndRenameHarvesterValues[i, 1]);
						dockClientNode.AddNode(node);
					}
				}

				harvesterNode.RenameChildrenMatching("DeliveryBuildings", "DockType");
				harvesterNode.RemoveNodes("MaxUnloadQueue");

				actorNode.AddNode(dockClientNode);
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
