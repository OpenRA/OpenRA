#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class ResourceLayerInfo : ITraitInfo, Requires<ResourceTypeInfo>, Requires<BuildingInfluenceInfo>
	{
		public virtual object Create(ActorInitializer init) { return new ResourceLayer(init.Self); }
	}

	public class ResourceLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		static readonly CellContents EmptyCell = new CellContents();

		readonly World world;
		readonly BuildingInfluence buildingInfluence;
		readonly List<CPos> dirty = new List<CPos>();

		protected readonly CellLayer<CellContents> Content;
		protected readonly CellLayer<CellContents> RenderContent;

		public ResourceLayer(Actor self)
		{
			world = self.World;
			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();

			Content = new CellLayer<CellContents>(world.Map);
			RenderContent = new CellLayer<CellContents>(world.Map);
		}

		public void Render(WorldRenderer wr)
		{
			var shroudObscured = world.ShroudObscuresTest;
			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.MapCoords)
			{
				if (shroudObscured(uv))
					continue;

				var c = RenderContent[uv];
				if (c.Sprite != null)
					new SpriteRenderable(c.Sprite, wr.World.Map.CenterOfCell(uv.ToCPos(world.Map)),
						WVec.Zero, -511, c.Type.Palette, 1f, true).Render(wr); // TODO ZOffset is ignored
			}
		}

		int GetAdjacentCellsWith(ResourceType t, CPos cell)
		{
			var sum = 0;
			for (var u = -1; u < 2; u++)
			{
				for (var v = -1; v < 2; v++)
				{
					var c = cell + new CVec(u, v);
					if (Content.Contains(c) && Content[c].Type == t)
						++sum;
				}
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
				if (!resources.TryGetValue(w.Map.MapResources.Value[cell].Type, out t))
					continue;

				if (!AllowResourceAt(t, cell))
					continue;

				Content[cell] = CreateResourceCell(t, cell);
			}

			// Set initial density based on the number of neighboring resources
			foreach (var cell in w.Map.AllCells)
			{
				var type = Content[cell].Type;
				if (type != null)
				{
					// Adjacent includes the current cell, so is always >= 1
					var adjacent = GetAdjacentCellsWith(type, cell);
					var density = int2.Lerp(0, type.Info.MaxDensity, adjacent, 9);
					var temp = Content[cell];
					temp.Density = Math.Max(density, 1);

					RenderContent[cell] = Content[cell] = temp;
					UpdateRenderedSprite(cell);
				}
			}
		}

		protected virtual void UpdateRenderedSprite(CPos cell)
		{
			var t = RenderContent[cell];
			if (t.Density > 0)
			{
				var sprites = t.Type.Variants[t.Variant];
				var frame = int2.Lerp(0, sprites.Length - 1, t.Density - 1, t.Type.Info.MaxDensity);
				t.Sprite = sprites[frame];
			}
			else
				t.Sprite = null;

			RenderContent[cell] = t;
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
					RenderContent[c] = Content[c];
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

			if (!rt.Info.AllowOnRamps)
			{
				var tile = world.Map.MapTiles.Value[cell];
				var tileInfo = world.TileSet.GetTileInfo(tile);
				if (tileInfo != null && tileInfo.RampType > 0)
					return false;
			}

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
			var cell = Content[p];
			if (cell.Type == null)
				cell = CreateResourceCell(t, p);

			if (cell.Type != t)
				return;

			cell.Density = Math.Min(cell.Type.Info.MaxDensity, cell.Density + n);
			Content[p] = cell;

			if (!dirty.Contains(p))
				dirty.Add(p);
		}

		public bool IsFull(CPos cell)
		{
			return Content[cell].Density == Content[cell].Type.Info.MaxDensity;
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
			}
			else
				Content[cell] = c;

			if (!dirty.Contains(cell))
				dirty.Add(cell);

			return c.Type;
		}

		public void Destroy(CPos cell)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (Content[cell].Type == null)
				return;

			// Clear cell
			Content[cell] = EmptyCell;
			world.Map.CustomTerrain[cell] = byte.MaxValue;

			if (!dirty.Contains(cell))
				dirty.Add(cell);
		}

		public ResourceType GetResource(CPos cell) { return Content[cell].Type; }
		public ResourceType GetRenderedResource(CPos cell) { return RenderContent[cell].Type; }
		public int GetResourceDensity(CPos cell) { return Content[cell].Density; }
		public int GetMaxResourceDensity(CPos cell)
		{
			if (Content[cell].Type == null)
				return 0;

			return Content[cell].Type.Info.MaxDensity;
		}

		public struct CellContents
		{
			public static readonly CellContents Empty = new CellContents();
			public ResourceType Type;
			public int Density;
			public string Variant;
			public Sprite Sprite;
		}
	}
}
