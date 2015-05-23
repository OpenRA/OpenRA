#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Attach this to the world actor. Required for LaysTerrain to work.")]
	public class BuildableTerrainLayerInfo : TraitInfo<BuildableTerrainLayer> { }
	public class BuildableTerrainLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		Dictionary<CPos, Sprite> tiles;
		Dictionary<CPos, Sprite> dirty;
		Theater theater;
		TileSet tileset;
		Map map;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			theater = wr.Theater;
			tileset = w.TileSet;
			map = w.Map;
			tiles = new Dictionary<CPos, Sprite>();
			dirty = new Dictionary<CPos, Sprite>();
		}

		public void AddTile(CPos cell, TerrainTile tile)
		{
			map.CustomTerrain[cell] = tileset.GetTerrainIndex(tile);

			// Terrain tiles define their origin at the topleft
			var s = theater.TileSprite(tile);
			dirty[cell] = new Sprite(s.sheet, s.bounds, float2.Zero, s.channel, s.blendMode);
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				if (!self.World.FogObscures(kv.Key))
				{
					tiles[kv.Key] = kv.Value;
					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			var pal = wr.Palette("terrain");

			foreach (var kv in tiles)
			{
				if (!wr.Viewport.VisibleCells.Contains(kv.Key))
					continue;

				if (wr.world.ShroudObscures(kv.Key))
					continue;

				new SpriteRenderable(kv.Value, wr.world.Map.CenterOfCell(kv.Key),
					WVec.Zero, -511, pal, 1f, true).Render(wr);
			}
		}
	}
}
