#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ResourceLayerInfo : TraitInfo<ResourceLayer>, Requires<ResourceTypeInfo> { }

	public class ResourceLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		static readonly CellContents EmptyCell = new CellContents();

		World world;
		protected CellContents[,] content;
		protected CellContents[,] render;
		List<CPos> dirty;

		public void Render(WorldRenderer wr)
		{
			var clip = wr.Viewport.CellBounds;
			for (var x = clip.Left; x < clip.Right; x++)
			{
				for (var y = clip.Top; y < clip.Bottom; y++)
				{
					var pos = new CPos(x, y);
					if (world.ShroudObscures(pos))
						continue;

					var c = render[x, y];
					if (c.Sprite != null)
						new SpriteRenderable(c.Sprite, pos.CenterPosition,
							WVec.Zero, -511, c.Type.Palette, 1f, true).Render(wr);
				}
			}
		}

		int GetAdjacentCellsWith(ResourceType t, int i, int j)
		{
			var sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (content[i + u, j + v].Type == t)
						++sum;
			return sum;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			this.world = w;
			content = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];
			render = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];
			dirty = new List<CPos>();

			var resources = w.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			var map = w.Map;
			for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
			{
				for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
				{
					var cell = new CPos(x, y);
					ResourceType t;
					if (!resources.TryGetValue(w.Map.MapResources.Value[x, y].Type, out t))
						continue;

					if (!AllowResourceAt(t, cell))
						continue;

					content[x, y] = CreateResourceCell(t, cell);
				}
			}

			// Set initial density based on the number of neighboring resources
			for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
			{
				for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
				{
					var type = content[x, y].Type;
					if (type != null)
					{
						// Adjacent includes the current cell, so is always >= 1
						var adjacent = GetAdjacentCellsWith(type, x, y);
						var density = int2.Lerp(0, type.Info.MaxDensity, adjacent, 9);
						content[x, y].Density = Math.Max(density, 1);

						render[x, y] = content[x, y];
						UpdateRenderedSprite(new CPos(x, y));
					}
				}
			}
		}

		protected virtual void UpdateRenderedSprite(CPos p)
		{
			var t = render[p.X, p.Y];
			if (t.Density > 0)
			{
				var sprites = t.Type.Variants[t.Variant];
				var frame = int2.Lerp(0, sprites.Length - 1, t.Density - 1, t.Type.Info.MaxDensity);
				t.Sprite = sprites[frame];
			}
			else
				t.Sprite = null;

			render[p.X, p.Y] = t;
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
					render[c.X, c.Y] = content[c.X, c.Y];
					UpdateRenderedSprite(c);
					remove.Add(c);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public bool AllowResourceAt(ResourceType rt, CPos a)
		{
			if (!world.Map.IsInMap(a.X, a.Y))
				return false;

			if (!rt.Info.AllowedTerrainTypes.Contains(world.GetTerrainInfo(a).Type))
				return false;

			if (!rt.Info.AllowUnderActors && world.ActorMap.AnyUnitsAt(a))
				return false;

			return true;
		}

		CellContents CreateResourceCell(ResourceType t, CPos p)
		{
			world.Map.CustomTerrain[p.X, p.Y] = world.TileSet.GetTerrainIndex(t.Info.TerrainType);
			return new CellContents
			{
				Type = t,
				Variant = ChooseRandomVariant(t),
			};
		}

		public void AddResource(ResourceType t, CPos p, int n)
		{
			var cell = content[p.X, p.Y];
			if (cell.Type == null)
				cell = CreateResourceCell(t, p);

			if (cell.Type != t)
				return;

			cell.Density = Math.Min(cell.Type.Info.MaxDensity, cell.Density + n);
			content[p.X, p.Y] = cell;

			if (!dirty.Contains(p))
				dirty.Add(p);
		}

		public bool IsFull(int i, int j)
		{
			return content[i, j].Density == content[i, j].Type.Info.MaxDensity;
		}

		public ResourceType Harvest(CPos p)
		{
			var type = content[p.X, p.Y].Type;
			if (type == null)
				return null;

			if (--content[p.X, p.Y].Density < 0)
			{
				content[p.X, p.Y] = EmptyCell;
				world.Map.CustomTerrain[p.X, p.Y] = -1;
			}

			if (!dirty.Contains(p))
				dirty.Add(p);

			return type;
		}

		public void Destroy(CPos p)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (content[p.X, p.Y].Type == null)
				return;

			// Clear cell
			content[p.X, p.Y] = EmptyCell;
			world.Map.CustomTerrain[p.X, p.Y] = -1;

			if (!dirty.Contains(p))
				dirty.Add(p);
		}

		public ResourceType GetResource(CPos p) { return content[p.X, p.Y].Type; }
		public ResourceType GetRenderedResource(CPos p) { return render[p.X, p.Y].Type; }
		public int GetResourceDensity(CPos p) { return content[p.X, p.Y].Density; }
		public int GetMaxResourceDensity(CPos p)
		{
			if (content[p.X, p.Y].Type == null)
				return 0;

			return content[p.X, p.Y].Type.Info.MaxDensity;
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
