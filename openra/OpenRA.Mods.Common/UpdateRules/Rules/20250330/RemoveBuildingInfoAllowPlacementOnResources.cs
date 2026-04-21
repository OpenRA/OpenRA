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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	/// <summary>
	/// Replaces the BaseAttackNotifier with a new AttackNotifier that uses the
	/// new attack system.
	/// </summary>
	public class RemoveBuildingInfoAllowPlacementOnResources : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Remove AllowPlacementOnResources from BuildingInfo";
		public override string Description => "Removes AllowPlacementOnResources from BuildingInfo and adds terrains with resources" +
			"to TerrainTypes (if a Building trait uses AllowPlacementOnResources: true).";

		readonly HashSet<string> terrainTypesWithResources = [];

		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			var resourceLayerInfo = modData.DefaultRules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>();
			if (resourceLayerInfo == null)
				yield break;

			foreach (var resourceType in resourceLayerInfo.ResourceTypes.Values)
			{
				terrainTypesWithResources.Add(resourceType.TerrainType);
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var buildingInfo in actorNode.ChildrenMatching("Building"))
			{
				if (buildingInfo.RemoveNodes("AllowPlacementOnResources") == 0)
					continue;

				var terrainTypesNode = buildingInfo.Value.NodeWithKeyOrDefault("TerrainTypes");
				if (terrainTypesNode == null)
				{
					terrainTypesNode = new MiniYamlNodeBuilder("TerrainTypes", "");
					buildingInfo.AddNode(terrainTypesNode);
				}

				var allowedTerrainTypes = terrainTypesNode?.NodeValue<List<string>>() ?? [];
				foreach (var terrainType in terrainTypesWithResources)
				{
					if (!allowedTerrainTypes.Contains(terrainType))
						allowedTerrainTypes.Add(terrainType);
				}

				terrainTypesNode.ReplaceValue(string.Join(", ", allowedTerrainTypes));
			}

			yield break;
		}
	}
}
