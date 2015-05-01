#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Attach this to the world actor. Required for LaysTerrain to work.")]
	public class BuildableTerrainLayerInfo : TraitInfo<BuildableTerrainLayer> { }
	public class BuildableTerrainLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		readonly Dictionary<CPos, Sprite> tiles = new Dictionary<CPos, Sprite>();
		readonly Dictionary<CPos, Sprite> tilesDirty = new Dictionary<CPos, Sprite>();
		Theater theater;
		TileSet tileset;
		Map map;
		VertexCache vertexCache;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			theater = wr.Theater;
			tileset = w.TileSet;
			map = w.Map;
			vertexCache = new VertexCache(map);
		}

		public void AddTile(CPos cell, TerrainTile tile)
		{
			map.CustomTerrain[cell] = tileset.GetTerrainIndex(tile);

			// Terrain tiles define their origin at the topleft
			var s = theater.TileSprite(tile);
			tilesDirty[cell] = new Sprite(s.Sheet, s.Bounds, float2.Zero, s.Channel, s.BlendMode);
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in tilesDirty)
			{
				var cell = kv.Key;
				if (!self.World.FogObscures(cell))
				{
					tiles[cell] = kv.Value;
					vertexCache.Invalidate(cell);
					remove.Add(cell);
				}
			}

			foreach (var r in remove)
				tilesDirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			var world = wr.World;
			var pal = wr.Palette("terrain");
			var visibleCells = wr.Viewport.VisibleCells;
			var shroudObscured = world.ShroudObscuresTest(visibleCells);
			foreach (var kv in tiles)
			{
				var uv = kv.Key.ToMPos(world.Map);
				if (!visibleCells.Contains(uv) || shroudObscured(uv))
					continue;
				vertexCache.RenderCenteredOverCell(wr, kv.Value, pal, uv);
			}
		}
	}
}
