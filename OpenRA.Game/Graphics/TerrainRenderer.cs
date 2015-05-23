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
		readonly IVertexBuffer<Vertex> vertexBuffer;
		readonly World world;
		readonly Map map;

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
				var pos = wr.ScreenPosition(map.CenterOfCell(cell)) + tile.Offset - 0.5f * tile.Size;
				Util.FastCreateQuad(vertices, pos, tile, terrainPalette, nv, tile.Size);
				nv += 4;
			}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(vertices.Length);
			vertexBuffer.SetData(vertices, nv);
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			var verticesPerRow = 4 * map.Bounds.Width;
			var cells = viewport.VisibleCells;

			// Only draw the rows that are visible.
			// VisibleCells is clamped to the map, so additional checks are unnecessary
			var firstRow = cells.TopLeft.ToMPos(map).V - map.Bounds.Top;
			var lastRow = cells.BottomRight.ToMPos(map).V - map.Bounds.Top + 1;

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, verticesPerRow * firstRow, verticesPerRow * (lastRow - firstRow),
				PrimitiveType.QuadList, wr.Theater.Sheet);

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		public void Dispose()
		{
			vertexBuffer.Dispose();
		}
	}
}
