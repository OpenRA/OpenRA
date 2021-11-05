#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	class TSEditorResourceLayerInfo : EditorResourceLayerInfo, Requires<EditorActorLayerInfo>
	{
		public readonly string VeinType = "Veins";

		[ActorReference]
		[Desc("Actor types that should be treated as veins for adjacency.")]
		public readonly HashSet<string> VeinholeActors = new HashSet<string> { };

		public override object Create(ActorInitializer init) { return new TSEditorResourceLayer(init.Self, this); }
	}

	class TSEditorResourceLayer : EditorResourceLayer
	{
		readonly TSEditorResourceLayerInfo info;
		readonly EditorActorLayer actorLayer;

		public TSEditorResourceLayer(Actor self, TSEditorResourceLayerInfo info)
			: base(self, info)
		{
			this.info = info;
			actorLayer = self.Trait<EditorActorLayer>();
		}

		bool IsValidVeinNeighbour(CPos cell, CPos neighbour)
		{
			if (!Map.Contains(neighbour))
				return false;

			// Cell is automatically valid if it contains a veinhole actor
			if (actorLayer.PreviewsAt(neighbour).Any(a => info.VeinholeActors.Contains(a.Info.Name)))
				return true;

			// Neighbour must be flat or a cardinal slope, unless the resource cell itself is a slope
			if (Map.Ramp[cell] == 0 && Map.Ramp[neighbour] > 4)
				return false;

			var terrainInfo = Map.Rules.TerrainInfo;
			var terrainType = terrainInfo.TerrainTypes[terrainInfo.GetTerrainInfo(Map.Tiles[neighbour]).TerrainType].Type;
			return info.ResourceTypes[info.VeinType].AllowedTerrainTypes.Contains(terrainType);
		}

		protected override bool AllowResourceAt(string resourceType, CPos cell)
		{
			var mapResources = Map.Resources;
			if (!mapResources.Contains(cell))
				return false;

			// Resources are allowed on flat terrain and cardinal slopes
			if (Map.Ramp[cell] > 4)
				return false;

			if (!info.ResourceTypes.TryGetValue(resourceType, out var resourceInfo))
				return false;

			// Ignore custom terrain types when spawning resources in the editor
			var terrainInfo = Map.Rules.TerrainInfo;
			var terrainType = terrainInfo.TerrainTypes[terrainInfo.GetTerrainInfo(Map.Tiles[cell]).TerrainType].Type;
			if (!resourceInfo.AllowedTerrainTypes.Contains(terrainType))
				return false;

			// Veins must be placed next to a compatible border cell
			if (resourceType == info.VeinType)
			{
				var neighboursValid = Common.Util.ExpandFootprint(cell, false)
					.All(c => IsValidVeinNeighbour(cell, c));

				if (!neighboursValid)
					return false;
			}

			// TODO: Check against actors in the EditorActorLayer
			return true;
		}

		protected override int AddResource(string resourceType, CPos cell, int amount = 1)
		{
			var added = base.AddResource(resourceType, cell, amount);

			// Update neighbouring cells if needed to provide space for vein borders
			var resourceIsVeins = resourceType == info.VeinType;
			foreach (var c in Common.Util.ExpandFootprint(cell, false))
			{
				var resourceIndex = Map.Resources[c].Type;
				if (resourceIndex == 0 || !ResourceTypesByIndex.TryGetValue(resourceIndex, out var neighourResourceType))
					neighourResourceType = null;

				var neighbourIsVeins = neighourResourceType == info.VeinType;
				if (resourceIsVeins ^ neighbourIsVeins)
					ClearResources(c);
			}

			return added;
		}
	}
}
