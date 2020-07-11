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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class ResourceLayerInfo : TraitInfo, Requires<ResourceTypeInfo>, Requires<BuildingInfluenceInfo>
	{
		public override object Create(ActorInitializer init) { return new ResourceLayer(init.Self); }
	}

	public class ResourceLayer : IWorldLoaded
	{
		static readonly CellContents EmptyCell = default(CellContents);

		readonly World world;
		readonly BuildingInfluence buildingInfluence;

		protected readonly CellLayer<CellContents> Content;

		public bool IsResourceLayerEmpty { get { return resCells < 1; } }

		int resCells;

		public event Action<CPos, ResourceType> CellChanged;

		public ResourceLayer(Actor self)
		{
			world = self.World;
			buildingInfluence = self.Trait<BuildingInfluence>();

			Content = new CellLayer<CellContents>(world.Map);
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
				ResourceType t;
				if (!resources.TryGetValue(w.Map.Resources[cell].Type, out t))
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
					var temp = GetResource(cell);
					temp.Density = Math.Max(density, 1);

					Content[cell] = temp;
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

		public bool CanSpawnResourceAt(ResourceType newResourceType, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			var currentResourceType = GetResourceType(cell);
			return (currentResourceType == newResourceType && !IsFull(cell))
				|| (currentResourceType == null && AllowResourceAt(newResourceType, cell));
		}

		CellContents CreateResourceCell(ResourceType t, CPos cell)
		{
			world.Map.CustomTerrain[cell] = world.Map.Rules.TileSet.GetTerrainIndex(t.Info.TerrainType);
			++resCells;

			return new CellContents
			{
				Type = t
			};
		}

		public void AddResource(ResourceType t, CPos p, int n)
		{
			var cell = Content[p];
			if (cell.Type == null)
				cell = CreateResourceCell(t, p);

			if (cell.Type != t)
				return;

			cell.Density = Math.Min(cell.Type.Info.MaxDensity, cell.Density + n);
			Content[p] = cell;

			if (CellChanged != null)
				CellChanged(p, cell.Type);
		}

		public bool IsFull(CPos cell)
		{
			var cellContents = Content[cell];
			return cellContents.Density == cellContents.Type.Info.MaxDensity;
		}

		public ResourceType Harvest(CPos cell)
		{
			var c = Content[cell];
			if (c.Type == null)
				return null;

			if (--c.Density < 0)
			{
				Content[cell] = EmptyCell;
				world.Map.CustomTerrain[cell] = byte.MaxValue;
				--resCells;
			}
			else
				Content[cell] = c;

			if (CellChanged != null)
				CellChanged(cell, c.Type);

			return c.Type;
		}

		public void Destroy(CPos cell)
		{
			// Don't break other users of CustomTerrain if there are no resources
			var c = Content[cell];
			if (c.Type == null)
				return;

			--resCells;

			// Clear cell
			Content[cell] = EmptyCell;
			world.Map.CustomTerrain[cell] = byte.MaxValue;

			if (CellChanged != null)
				CellChanged(cell, c.Type);
		}

		public CellContents GetResource(CPos cell) { return Content[cell]; }
		public ResourceType GetResourceType(CPos cell) { return Content[cell].Type; }

		public int GetResourceDensity(CPos cell) { return Content[cell].Density; }
		public int GetMaxResourceDensity(CPos cell)
		{
			if (Content[cell].Type == null)
				return 0;

			return Content[cell].Type.Info.MaxDensity;
		}

		public struct CellContents
		{
			public static readonly CellContents Empty = default(CellContents);
			public ResourceType Type;
			public int Density;
		}
	}
}
