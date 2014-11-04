#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Graphics
{
	class TerrainRenderer
	{
		IVertexBuffer<Vertex> vertexBuffer;

		World world;
		Map map;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			this.world = world;
			this.map = world.Map;

			var terrainPalette = wr.Palette("terrain").Index;
			var vertices = new Vertex[4 * map.Bounds.Height * map.Bounds.Width];
			var nv = 0;

			foreach (var cell in map.Cells)
			{
				var tile = wr.Theater.TileSprite(map.MapTiles.Value[cell]);
				var pos = wr.ScreenPosition(map.CenterOfCell(cell)) + tile.offset - 0.5f * tile.size;
				Util.FastCreateQuad(vertices, pos, tile, terrainPalette, nv, tile.size);
				nv += 4;
			}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(vertices.Length);
			vertexBuffer.SetData(vertices, nv);
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			var verticesPerRow = 4*map.Bounds.Width;
			var cells = viewport.VisibleCells;
			var shape = wr.world.Map.TileShape;

			// Only draw the rows that are visible.
			// VisibleCells is clamped to the map, so additional checks are unnecessary
			var firstRow = Map.CellToMap(shape, cells.TopLeft).Y - map.Bounds.Top;
			var lastRow = Map.CellToMap(shape, cells.BottomRight).Y - map.Bounds.Top + 1;

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, verticesPerRow * firstRow, verticesPerRow * (lastRow - firstRow),
				PrimitiveList.QuadList, wr.Theater.Sheet);

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}
	}
}
