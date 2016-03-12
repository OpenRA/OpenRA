#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	sealed class TerrainRenderer : IDisposable
	{
		readonly Map map;
		readonly Dictionary<string, TerrainSpriteLayer> spriteLayers = new Dictionary<string, TerrainSpriteLayer>();
		readonly Theater theater;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			map = world.Map;
			theater = wr.Theater;

			foreach (var template in map.Rules.TileSet.Templates)
			{
				var palette = template.Value.Palette ?? TileSet.TerrainPaletteInternalName;
				spriteLayers.GetOrAdd(palette, pal =>
					new TerrainSpriteLayer(world, wr, theater.Sheet, BlendMode.Alpha, wr.Palette(palette), world.Type != WorldType.Editor));
			}

			foreach (var cell in map.AllCells)
				UpdateCell(cell);

			map.Tiles.CellEntryChanged += UpdateCell;
			map.Height.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			var tile = map.Tiles[cell];
			var palette = TileSet.TerrainPaletteInternalName;
			if (map.Rules.TileSet.Templates.ContainsKey(tile.Type))
				palette = map.Rules.TileSet.Templates[tile.Type].Palette ?? palette;

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
			map.Tiles.CellEntryChanged -= UpdateCell;
			map.Height.CellEntryChanged -= UpdateCell;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();
		}
	}
}
