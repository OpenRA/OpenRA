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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public struct ResourceLayerContents
	{
		public static readonly ResourceLayerContents Empty = default;
		public readonly ResourceType Type;
		public readonly int Density;

		public ResourceLayerContents(ResourceType type, int density)
		{
			Type = type;
			Density = density;
		}
	}

	public interface IResourceLayerInfo : ITraitInfoInterface { }

	[RequireExplicitImplementation]
	public interface IResourceLayer
	{
		event Action<CPos, ResourceType> CellChanged;
		ResourceLayerContents GetResource(CPos cell);
		bool CanAddResource(ResourceType resourceType, CPos cell, int amount = 1);
		int AddResource(ResourceType resourceType, CPos cell, int amount = 1);
		int RemoveResource(ResourceType resourceType, CPos cell, int amount = 1);
		void ClearResources(CPos cell);

		bool IsVisible(CPos cell);
	}

	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class ResourceLayerInfo : TraitInfo, IResourceLayerInfo, Requires<ResourceTypeInfo>, Requires<BuildingInfluenceInfo>
	{
		public override object Create(ActorInitializer init) { return new ResourceLayer(init.Self); }
	}

	public class ResourceLayer : IResourceLayer, IWorldLoaded
	{
		readonly World world;
		readonly BuildingInfluence buildingInfluence;

		protected readonly CellLayer<ResourceLayerContents> Content;

		public bool IsResourceLayerEmpty => resCells < 1;

		int resCells;

		public event Action<CPos, ResourceType> CellChanged;

		public ResourceLayer(Actor self)
		{
			world = self.World;
			buildingInfluence = self.Trait<BuildingInfluence>();

			Content = new CellLayer<ResourceLayerContents>(world.Map);
		}

		int GetAdjacentCellsWith(ResourceType t, CPos cell)
		{
			var sum = 0;
			var directions = CVec.Directions;
			for (var i = 0; i < directions.Length; i++)
			{
				var c = cell + directions[i];
				if (Content.Contains(c) && Content[c].Type == t)
					++sum;
			}

			return sum;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var resources = w.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			foreach (var cell in w.Map.AllCells)
			{
				if (!resources.TryGetValue(w.Map.Resources[cell].Type, out var t))
					continue;

				if (!AllowResourceAt(t, cell))
					continue;

				Content[cell] = CreateResourceCell(t, cell);
			}

			foreach (var cell in w.Map.AllCells)
			{
				var type = GetResourceType(cell);
				if (type != null)
				{
					// Set initial density based on the number of neighboring resources
					// Adjacent includes the current cell, so is always >= 1
					var adjacent = GetAdjacentCellsWith(type, cell);
					var density = int2.Lerp(0, type.Info.MaxDensity, adjacent, 9);
					Content[cell] = new ResourceLayerContents(Content[cell].Type, Math.Max(density, 1));
				}
			}
		}

		public bool AllowResourceAt(ResourceType rt, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!rt.Info.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			if (!rt.Info.AllowUnderActors && world.ActorMap.AnyActorsAt(cell))
				return false;

			if (!rt.Info.AllowUnderBuildings && buildingInfluence.GetBuildingAt(cell) != null)
				return false;

			return rt.Info.AllowOnRamps || world.Map.Ramp[cell] == 0;
		}

		ResourceLayerContents CreateResourceCell(ResourceType t, CPos cell)
		{
			world.Map.CustomTerrain[cell] = world.Map.Rules.TerrainInfo.GetTerrainIndex(t.Info.TerrainType);
			++resCells;

			return new ResourceLayerContents(t, 0);
		}

		public bool CanAddResource(ResourceType resourceType, CPos cell, int amount = 1)
		{
			if (!world.Map.Contains(cell))
				return false;

			var content = Content[cell];
			if (content.Type == null)
				return amount <= resourceType.Info.MaxDensity && AllowResourceAt(resourceType, cell);

			if (content.Type != resourceType)
				return false;

			return content.Density + amount <= resourceType.Info.MaxDensity;
		}

		public int AddResource(ResourceType resourceType, CPos cell, int amount = 1)
		{
			if (!Content.Contains(cell))
				return 0;

			var content = Content[cell];
			if (content.Type == null)
				content = CreateResourceCell(resourceType, cell);

			if (content.Type != resourceType)
				return 0;

			var oldDensity = content.Density;
			var density = Math.Min(content.Type.Info.MaxDensity, oldDensity + amount);
			Content[cell] = new ResourceLayerContents(content.Type, density);

			CellChanged?.Invoke(cell, content.Type);

			return density - oldDensity;
		}

		public int RemoveResource(ResourceType resourceType, CPos cell, int amount = 1)
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

		public void ClearResources(CPos cell)
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

		public bool IsFull(CPos cell)
		{
			var cellContents = Content[cell];
			return cellContents.Density == cellContents.Type.Info.MaxDensity;
		}

		public ResourceType GetResourceType(CPos cell) { return Content[cell].Type; }

		public int GetResourceDensity(CPos cell) { return Content[cell].Density; }

		ResourceLayerContents IResourceLayer.GetResource(CPos cell) { return Content[cell]; }

		bool IResourceLayer.CanAddResource(ResourceType resourceType, CPos cell, int amount) { return CanAddResource(resourceType, cell, amount); }
		int IResourceLayer.AddResource(ResourceType resourceType, CPos cell, int amount) { return AddResource(resourceType, cell, amount); }
		int IResourceLayer.RemoveResource(ResourceType resourceType, CPos cell, int amount) { return RemoveResource(resourceType, cell, amount); }
		void IResourceLayer.ClearResources(CPos cell) { ClearResources(cell); }
		bool IResourceLayer.IsVisible(CPos cell) { return !world.FogObscures(cell); }
	}
}
