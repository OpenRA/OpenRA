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
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorResourceLayerInfo : TraitInfo, IResourceLayerInfo, Requires<ResourceTypeInfo>
	{
		public override object Create(ActorInitializer init) { return new EditorResourceLayer(init.Self); }
	}

	public class EditorResourceLayer : IResourceLayer, IWorldLoaded, INotifyActorDisposing
	{
		protected readonly Map Map;
		protected readonly Dictionary<string, ResourceTypeInfo> ResourceInfo;
		protected readonly Dictionary<int, string> Resources;
		protected readonly CellLayer<ResourceLayerContents> Tiles;

		public int NetWorth { get; protected set; }

		bool disposed;

		public event Action<CPos, string> CellChanged;

		ResourceLayerContents IResourceLayer.GetResource(CPos cell) { return Tiles.Contains(cell) ? Tiles[cell] : default; }
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
		bool IResourceLayer.IsVisible(CPos cell) { return Map.Contains(cell); }
		bool IResourceLayer.IsEmpty => false;

		public EditorResourceLayer(Actor self)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			Map = self.World.Map;
			Tiles = new CellLayer<ResourceLayerContents>(Map);
			ResourceInfo = self.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.Type, r => r.Info);
			Resources = ResourceInfo.Values
				.ToDictionary(r => r.ResourceType, r => r.Type);

			Map.Resources.CellEntryChanged += UpdateCell;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Editor)
				return;

			foreach (var cell in Map.AllCells)
				UpdateCell(cell);
		}

		public void UpdateCell(CPos cell)
		{
			var uv = cell.ToMPos(Map);
			if (!Map.Resources.Contains(uv))
				return;

			var tile = Map.Resources[uv];
			var t = Tiles[uv];

			var newTile = ResourceLayerContents.Empty;
			var newTerrain = byte.MaxValue;
			if (Resources.TryGetValue(tile.Type, out var resourceType) && ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
			{
				newTile = new ResourceLayerContents(resourceType, CalculateCellDensity(resourceType, cell));
				newTerrain = Map.Rules.TerrainInfo.GetTerrainIndex(resourceInfo.TerrainType);
			}

			// Nothing has changed
			if (newTile.Type == t.Type && newTile.Density == t.Density)
				return;

			UpdateNetWorth(t.Type, t.Density, newTile.Type, newTile.Density);
			Tiles[uv] = newTile;
			Map.CustomTerrain[uv] = newTerrain;
			CellChanged?.Invoke(cell, newTile.Type);

			// Neighbouring cell density depends on this cell
			foreach (var d in CVec.Directions)
			{
				var neighbouringCell = cell + d;
				if (!Tiles.Contains(neighbouringCell))
					continue;

				var neighbouringTile = Tiles[neighbouringCell];
				var density = CalculateCellDensity(neighbouringTile.Type, neighbouringCell);
				if (neighbouringTile.Density == density)
					continue;

				UpdateNetWorth(neighbouringTile.Type, neighbouringTile.Density, neighbouringTile.Type, density);
				Tiles[neighbouringCell] = new ResourceLayerContents(neighbouringTile.Type, density);

				CellChanged?.Invoke(neighbouringCell, neighbouringTile.Type);
			}
		}

		void UpdateNetWorth(string oldResourceType, int oldDensity, string newResourceType, int newDensity)
		{
			// Density + 1 as workaround for fixing ResourceLayer.Harvest as it would be very disruptive to balancing
			if (oldResourceType != null && oldDensity > 0 && ResourceInfo.TryGetValue(oldResourceType, out var oldResourceInfo))
				NetWorth -= (oldDensity + 1) * oldResourceInfo.ValuePerUnit;

			if (newResourceType != null && newDensity > 0 && ResourceInfo.TryGetValue(newResourceType, out var newResourceInfo))
				NetWorth += (newDensity + 1) * newResourceInfo.ValuePerUnit;
		}

		public int CalculateCellDensity(string resourceType, CPos c)
		{
			var resources = Map.Resources;
			if (resourceType == null || !ResourceInfo.TryGetValue(resourceType, out var resourceInfo) || resources[c].Type != resourceInfo.ResourceType)
				return 0;

			// Set density based on the number of neighboring resources
			var adjacent = 0;
			for (var u = -1; u < 2; u++)
			{
				for (var v = -1; v < 2; v++)
				{
					var cell = c + new CVec(u, v);
					if (resources.Contains(cell) && resources[cell].Type == resourceInfo.ResourceType)
						adjacent++;
				}
			}

			return Math.Max(int2.Lerp(0, resourceInfo.MaxDensity, adjacent, 9), 1);
		}

		bool AllowResourceAt(string resourceType, CPos cell)
		{
			var mapResources = Map.Resources;
			if (!mapResources.Contains(cell))
				return false;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return false;

			// Ignore custom terrain types when spawning resources in the editor
			var terrainInfo = Map.Rules.TerrainInfo;
			var terrainType = terrainInfo.TerrainTypes[terrainInfo.GetTerrainInfo(Map.Tiles[cell]).TerrainType].Type;
			if (!resourceInfo.AllowedTerrainTypes.Contains(terrainType))
				return false;

			// TODO: Check against actors in the EditorActorLayer
			return resourceInfo.AllowOnRamps || Map.Ramp[cell] == 0;
		}

		bool CanAddResource(string resourceType, CPos cell, int amount = 1)
		{
			var resources = Map.Resources;
			if (!resources.Contains(cell))
				return false;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return false;

			// The editor allows the user to replace one resource type with another, so treat mismatching resource type as an empty cell
			var content = resources[cell];
			if (content.Type != resourceInfo.ResourceType)
				return amount <= resourceInfo.MaxDensity && AllowResourceAt(resourceType, cell);

			var oldDensity = content.Type == resourceInfo.ResourceType ? content.Index : 0;
			return oldDensity + amount <= resourceInfo.MaxDensity;
		}

		int AddResource(string resourceType, CPos cell, int amount = 1)
		{
			var resources = Map.Resources;
			if (!resources.Contains(cell))
				return 0;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return 0;

			// The editor allows the user to replace one resource type with another, so treat mismatching resource type as an empty cell
			var content = resources[cell];
			var oldDensity = content.Type == resourceInfo.ResourceType ? content.Index : 0;
			var density = (byte)Math.Min(resourceInfo.MaxDensity, oldDensity + amount);
			Map.Resources[cell] = new ResourceTile((byte)resourceInfo.ResourceType, density);

			return density - oldDensity;
		}

		int RemoveResource(string resourceType, CPos cell, int amount = 1)
		{
			var resources = Map.Resources;
			if (!resources.Contains(cell))
				return 0;

			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				return 0;

			var content = resources[cell];
			if (content.Type == 0 || content.Type != resourceInfo.ResourceType)
				return 0;

			var oldDensity = content.Index;
			var density = (byte)Math.Max(0, oldDensity - amount);
			resources[cell] = density > 0 ? new ResourceTile((byte)resourceInfo.ResourceType, density) : default;

			return oldDensity - density;
		}

		void ClearResources(CPos cell)
		{
			Map.Resources[cell] = default;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			Map.Resources.CellEntryChanged -= UpdateCell;

			disposed = true;
		}
	}
}
