#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public struct ResourceLayerContents
	{
		public static readonly ResourceLayerContents Empty = default;
		public readonly string Type;
		public readonly int Density;

		public ResourceLayerContents(string type, int density)
		{
			Type = type;
			Density = density;
		}
	}

	[Desc("Attach this to the world actor.")]
	public class ResourceLayerInfo : TraitInfo, IResourceLayerInfo, Requires<ResourceTypeInfo>, Requires<BuildingInfluenceInfo>
	{
		public override object Create(ActorInitializer init) { return new ResourceLayer(init.Self); }
	}

	public class ResourceLayer : IResourceLayer, IWorldLoaded
	{
		readonly World world;
		readonly BuildingInfluence buildingInfluence;
		protected readonly Dictionary<string, ResourceTypeInfo> ResourceInfo;
		protected readonly CellLayer<ResourceLayerContents> Content;

		int resCells;

		public event Action<CPos, string> CellChanged;

		public ResourceLayer(Actor self)
		{
			world = self.World;
			buildingInfluence = self.Trait<BuildingInfluence>();
			ResourceInfo = self.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.Type, r => r.Info);

			Content = new CellLayer<ResourceLayerContents>(world.Map);
		}

		int GetAdjacentCellsWith(string resourceType, CPos cell)
		{
			var sum = 0;
			var directions = CVec.Directions;
			for (var i = 0; i < directions.Length; i++)
			{
				var c = cell + directions[i];
				if (Content.Contains(c) && Content[c].Type == resourceType)
					++sum;
			}

			return sum;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var resources = w.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r.Info.Type);

			foreach (var cell in w.Map.AllCells)
			{
				if (!resources.TryGetValue(w.Map.Resources[cell].Type, out var resourceType))
					continue;

				if (!AllowResourceAt(resourceType, cell))
					continue;

				Content[cell] = CreateResourceCell(resourceType, cell);
			}

			foreach (var cell in w.Map.AllCells)
			{
				var resourceType = Content[cell].Type;
				if (resourceType != null && ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				{
					// Set initial density based on the number of neighboring resources
					// Adjacent includes the current cell, so is always >= 1
					var adjacent = GetAdjacentCellsWith(resourceType, cell);
					var density = int2.Lerp(0, resourceInfo.MaxDensity, adjacent, 9);
					Content[cell] = new ResourceLayerContents(Content[cell].Type, Math.Max(density, 1));
				}
			}
		}

		public bool AllowResourceAt(string resourceType, CPos cell)
		{
			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return false;

			if (!world.Map.Contains(cell))
				return false;

			if (!resourceInfo.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			if (!resourceInfo.AllowUnderActors && world.ActorMap.AnyActorsAt(cell))
				return false;

			if (!resourceInfo.AllowUnderBuildings && buildingInfluence.GetBuildingAt(cell) != null)
				return false;

			return resourceInfo.AllowOnRamps || world.Map.Ramp[cell] == 0;
		}

		ResourceLayerContents CreateResourceCell(string resourceType, CPos cell)
		{
			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
			{
				world.Map.CustomTerrain[cell] = byte.MaxValue;
				return ResourceLayerContents.Empty;
			}

			world.Map.CustomTerrain[cell] = world.Map.Rules.TerrainInfo.GetTerrainIndex(resourceInfo.TerrainType);
			++resCells;

			return new ResourceLayerContents(resourceType, 0);
		}

		bool CanAddResource(string resourceType, CPos cell, int amount = 1)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return false;

			var content = Content[cell];
			if (content.Type == null)
				return amount <= resourceInfo.MaxDensity && AllowResourceAt(resourceType, cell);

			if (content.Type != resourceType)
				return false;

			return content.Density + amount <= resourceInfo.MaxDensity;
		}

		int AddResource(string resourceType, CPos cell, int amount = 1)
		{
			if (!Content.Contains(cell))
				return 0;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return 0;

			var content = Content[cell];
			if (content.Type == null)
				content = CreateResourceCell(resourceType, cell);

			if (content.Type != resourceType)
				return 0;

			var oldDensity = content.Density;
			var density = Math.Min(resourceInfo.MaxDensity, oldDensity + amount);
			Content[cell] = new ResourceLayerContents(content.Type, density);

			CellChanged?.Invoke(cell, content.Type);

			return density - oldDensity;
		}

		int RemoveResource(string resourceType, CPos cell, int amount = 1)
		{
			if (!Content.Contains(cell))
				return 0;

			var content = Content[cell];
			if (content.Type == null || content.Type != resourceType)
				return 0;

			var oldDensity = content.Density;
			var density = Math.Max(0, oldDensity - amount);

			if (density == 0)
			{
				Content[cell] = ResourceLayerContents.Empty;
				world.Map.CustomTerrain[cell] = byte.MaxValue;
				--resCells;

				CellChanged?.Invoke(cell, null);
			}
			else
			{
				Content[cell] = new ResourceLayerContents(content.Type, density);
				CellChanged?.Invoke(cell, content.Type);
			}

			return oldDensity - density;
		}

		void ClearResources(CPos cell)
		{
			if (!Content.Contains(cell))
				return;

			// Don't break other users of CustomTerrain if there are no resources
			var content = Content[cell];
			if (content.Type == null)
				return;

			Content[cell] = ResourceLayerContents.Empty;
			world.Map.CustomTerrain[cell] = byte.MaxValue;
			--resCells;

			CellChanged?.Invoke(cell, null);
		}

		ResourceLayerContents IResourceLayer.GetResource(CPos cell) { return Content.Contains(cell) ? Content[cell] : default; }
		int IResourceLayer.GetMaxDensity(string resourceType)
		{
			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return 0;

			return resourceInfo.MaxDensity;
		}

		bool IResourceLayer.CanAddResource(string resourceType, CPos cell, int amount) { return CanAddResource(resourceType, cell, amount); }
		int IResourceLayer.AddResource(string resourceType, CPos cell, int amount) { return AddResource(resourceType, cell, amount); }
		int IResourceLayer.RemoveResource(string resourceType, CPos cell, int amount) { return RemoveResource(resourceType, cell, amount); }
		void IResourceLayer.ClearResources(CPos cell) { ClearResources(cell); }
		bool IResourceLayer.IsVisible(CPos cell) { return !world.FogObscures(cell); }
		bool IResourceLayer.IsEmpty => resCells < 1;
	}
}
