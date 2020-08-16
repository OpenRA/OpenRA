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
		protected readonly TileSet Tileset;
		protected readonly Dictionary<int, ResourceType> Resources;
		protected readonly CellLayer<ResourceLayerContents> Tiles;

		public int NetWorth { get; protected set; }

		bool disposed;

		public event Action<CPos, ResourceType> CellChanged;

		ResourceLayerContents IResourceLayer.GetResource(CPos cell) { return Tiles[cell]; }
		bool IResourceLayer.IsVisible(CPos cell) { return Map.Contains(cell); }

		public EditorResourceLayer(Actor self)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			Map = self.World.Map;
			Tileset = self.World.Map.Rules.TileSet;

			Tiles = new CellLayer<ResourceLayerContents>(Map);
			Resources = self.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

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
			if (Resources.TryGetValue(tile.Type, out var type))
			{
				newTile = new ResourceLayerContents
				{
					Type = type,
					Density = CalculateCellDensity(type, cell)
				};

				newTerrain = Tileset.GetTerrainIndex(type.Info.TerrainType);
			}

			// Nothing has changed
			if (newTile.Type == t.Type && newTile.Density == t.Density)
				return;

			UpdateNetWorth(t.Type, t.Density, newTile.Type, newTile.Density);
			Tiles[uv] = newTile;
			Map.CustomTerrain[uv] = newTerrain;
			CellChanged?.Invoke(cell, type);

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
				neighbouringTile.Density = density;
				Tiles[neighbouringCell] = neighbouringTile;

				CellChanged?.Invoke(neighbouringCell, type);
			}
		}

		void UpdateNetWorth(ResourceType oldType, int oldDensity, ResourceType newType, int newDensity)
		{
			// Density + 1 as workaround for fixing ResourceLayer.Harvest as it would be very disruptive to balancing
			if (oldType != null && oldDensity > 0)
				NetWorth -= (oldDensity + 1) * oldType.Info.ValuePerUnit;

			if (newType != null && newDensity > 0)
				NetWorth += (newDensity + 1) * newType.Info.ValuePerUnit;
		}

		public int CalculateCellDensity(ResourceType type, CPos c)
		{
			var resources = Map.Resources;
			if (type == null || resources[c].Type != type.Info.ResourceType)
				return 0;

			// Set density based on the number of neighboring resources
			var adjacent = 0;
			for (var u = -1; u < 2; u++)
			{
				for (var v = -1; v < 2; v++)
				{
					var cell = c + new CVec(u, v);
					if (resources.Contains(cell) && resources[cell].Type == type.Info.ResourceType)
						adjacent++;
				}
			}

			return Math.Max(int2.Lerp(0, type.Info.MaxDensity, adjacent, 9), 1);
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
