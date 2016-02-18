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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	sealed class TerrainRenderer : IDisposable
	{
		readonly World world;
		readonly Dictionary<string, TerrainSpriteLayer> spriteLayers = new Dictionary<string, TerrainSpriteLayer>();
		readonly Theater theater;
		readonly CellLayer<TerrainTile> mapTiles;
		readonly CellLayer<byte> mapHeight;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			this.world = world;
			theater = wr.Theater;
			mapTiles = world.Map.MapTiles.Value;
			mapHeight = world.Map.MapHeight.Value;

			foreach (var template in world.TileSet.Templates)
			{
				var palette = template.Value.Palette ?? TileSet.TerrainPaletteInternalName;
				spriteLayers.GetOrAdd(palette, pal =>
					new TerrainSpriteLayer(world, wr, theater.Sheet, BlendMode.Alpha, wr.Palette(palette), wr.World.Type != WorldType.Editor));
			}

			foreach (var cell in world.Map.AllCells)
				UpdateCell(cell);

			mapTiles.CellEntryChanged += UpdateCell;
			mapHeight.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			var tile = mapTiles[cell];
			var palette = world.TileSet.Templates[tile.Type].Palette ?? TileSet.TerrainPaletteInternalName;
			var sprite = theater.TileSprite(tile);
			foreach (var kv in spriteLayers)
				kv.Value.Update(cell, palette == kv.Key ? sprite : null);
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			foreach (var kv in spriteLayers.Values)
				kv.Draw(wr.Viewport);

			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		public void Dispose()
		{
			mapTiles.CellEntryChanged -= UpdateCell;
			mapHeight.CellEntryChanged -= UpdateCell;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();
		}
	}
}
