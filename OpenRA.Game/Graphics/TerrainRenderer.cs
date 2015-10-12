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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	sealed class TerrainRenderer : IDisposable
	{
		readonly TerrainSpriteLayer terrain;
		readonly Theater theater;
		readonly CellLayer<TerrainTile> mapTiles;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			theater = wr.Theater;
			mapTiles = world.Map.MapTiles.Value;

			terrain = new TerrainSpriteLayer(world, wr, theater.Sheet, BlendMode.Alpha,
				wr.Palette("terrain"), wr.World.Type != WorldType.Editor);

			foreach (var cell in world.Map.AllCells)
				UpdateCell(cell);

			world.Map.MapTiles.Value.CellEntryChanged += UpdateCell;
			world.Map.MapHeight.Value.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			terrain.Update(cell, theater.TileSprite(mapTiles[cell]));
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			terrain.Draw(viewport);
			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		public void Dispose()
		{
			terrain.Dispose();
		}
	}
}
