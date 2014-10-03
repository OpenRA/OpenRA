#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class ResourceLayerInfo : TraitInfo<ResourceLayer>, Requires<ResourceTypeInfo> { }

	public class ResourceLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		static readonly CellContents EmptyCell = new CellContents();

		World world;

		BuildingInfluence buildingInfluence;

		protected CellLayer<CellContents> content;
		protected CellLayer<CellContents> render;
		List<CPos> dirty;

		public void Render(WorldRenderer wr)
		{
			var shroudObscured = world.ShroudObscuresTest(wr.Viewport.VisibleCells);
			foreach (var cell in wr.Viewport.VisibleCells)
			{
				if (shroudObscured(cell))
					continue;

				var c = render[cell];
				if (c.Sprite != null)
					new SpriteRenderable(c.Sprite, wr.world.Map.CenterOfCell(cell),
						WVec.Zero, -511, c.Type.Palette, 1f, true).Render(wr); // TODO ZOffset is ignored
			}
		}

		int GetAdjacentCellsWith(ResourceType t, CPos cell)
		{
			var sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (content[cell + new CVec(u, v)].Type == t)
						++sum;

			return sum;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			this.world = w;

			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();

			content = new CellLayer<CellContents>(w.Map);
			render = new CellLayer<CellContents>(w.Map);
			dirty = new List<CPos>();

			var resources = w.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			foreach (var cell in w.Map.Cells)
			{
				ResourceType t;
				if (!resources.TryGetValue(w.Map.MapResources.Value[cell].Type, out t))
					continue;

				if (!AllowResourceAt(t, cell))
					continue;

				content[cell] = CreateResourceCell(t, cell);
			}

			// Set initial density based on the number of neighboring resources
			foreach (var cell in w.Map.Cells)
			{
				var type = content[cell].Type;
				if (type != null)
				{
					// Adjacent includes the current cell, so is always >= 1
					var adjacent = GetAdjacentCellsWith(type, cell);
					var density = int2.Lerp(0, type.Info.MaxDensity, adjacent, 9);
					var temp = content[cell];
					temp.Density = Math.Max(density, 1);

					render[cell] = content[cell] = temp;
					UpdateRenderedSprite(cell);
				}
			}
		}

		protected virtual void UpdateRenderedSprite(CPos cell)
		{
			var t = render[cell];
			if (t.Density > 0)
			{
				var sprites = t.Type.Variants[t.Variant];
				var frame = int2.Lerp(0, sprites.Length - 1, t.Density - 1, t.Type.Info.MaxDensity);
				t.Sprite = sprites[frame];
			}
			else
				t.Sprite = null;

			render[cell] = t;
		}

		protected virtual string ChooseRandomVariant(ResourceType t)
		{
			return t.Variants.Keys.Random(Game.CosmeticRandom);
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var c in dirty)
			{
				if (!self.World.FogObscures(c))
				{
					render[c] = content[c];
					UpdateRenderedSprite(c);
					remove.Add(c);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public bool AllowResourceAt(ResourceType rt, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!rt.Info.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			if (!rt.Info.AllowUnderActors && world.ActorMap.AnyUnitsAt(cell))
				return false;

			if (!rt.Info.AllowUnderBuildings && buildingInfluence.GetBuildingAt(cell) != null)
				return false;

			return true;
		}

		public bool CanSpawnResourceAt(ResourceType newResourceType, CPos cell)
		{
			var currentResourceType = GetResource(cell);
			return (currentResourceType == newResourceType && !IsFull(cell))
				|| (currentResourceType == null && AllowResourceAt(newResourceType, cell));
		}

		CellContents CreateResourceCell(ResourceType t, CPos cell)
		{
			world.Map.CustomTerrain[cell] = world.TileSet.GetTerrainIndex(t.Info.TerrainType);

			return new CellContents
			{
				Type = t,
				Variant = ChooseRandomVariant(t),
			};
		}

		public void AddResource(ResourceType t, CPos p, int n)
		{
			var cell = content[p];
			if (cell.Type == null)
				cell = CreateResourceCell(t, p);

			if (cell.Type != t)
				return;

			cell.Density = Math.Min(cell.Type.Info.MaxDensity, cell.Density + n);
			content[p] = cell;

			if (!dirty.Contains(p))
				dirty.Add(p);
		}

		public bool IsFull(CPos cell)
		{
			return content[cell].Density == content[cell].Type.Info.MaxDensity;
		}

		public ResourceType Harvest(CPos cell)
		{
			var c = content[cell];
			if (c.Type == null)
				return null;

			if (--c.Density < 0)
			{
				content[cell] = EmptyCell;
				world.Map.CustomTerrain[cell] = byte.MaxValue;
			}
			else
				content[cell] = c;

			if (!dirty.Contains(cell))
				dirty.Add(cell);

			return c.Type;
		}

		public void Destroy(CPos cell)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (content[cell].Type == null)
				return;

			// Clear cell
			content[cell] = EmptyCell;
			world.Map.CustomTerrain[cell] = byte.MaxValue;

			if (!dirty.Contains(cell))
				dirty.Add(cell);
		}

		public ResourceType GetResource(CPos cell) { return content[cell].Type; }
		public ResourceType GetRenderedResource(CPos cell) { return render[cell].Type; }
		public int GetResourceDensity(CPos cell) { return content[cell].Density; }
		public int GetMaxResourceDensity(CPos cell)
		{
			if (content[cell].Type == null)
				return 0;

			return content[cell].Type.Info.MaxDensity;
		}

		public struct CellContents
		{
			public ResourceType Type;
			public int Density;
			public string Variant;
			public Sprite Sprite;
		}
	}
}
