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
		readonly Vertex[] updateCellVertices = new Vertex[4];
		readonly float terrainPaletteIndex;
		readonly int rowStride;

		readonly WorldRenderer worldRenderer;
		readonly Theater theater;
		readonly CellLayer<TerrainTile> mapTiles;
		readonly Map map;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			worldRenderer = wr;
			theater = wr.Theater;
			map = world.Map;
			mapTiles = map.MapTiles.Value;

			terrainPaletteIndex = wr.Palette("terrain").TextureIndex;
			rowStride = 4 * map.Bounds.Width;

			var vertices = new Vertex[rowStride * map.Bounds.Height];
			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(vertices.Length);

			var nv = 0;
			foreach (var cell in map.Cells)
			{
				GenerateTileVertices(vertices, nv, cell);
				nv += 4;
			}

			vertexBuffer.SetData(vertices, nv);

			map.MapTiles.Value.CellEntryChanged += UpdateCell;
			map.MapHeight.Value.CellEntryChanged += UpdateCell;
		}

		void GenerateTileVertices(Vertex[] vertices, int offset, CPos cell)
		{
			var tile = theater.TileSprite(mapTiles[cell]);
			var pos = worldRenderer.ScreenPosition(map.CenterOfCell(cell)) + tile.Offset - 0.5f * tile.Size;
			Util.FastCreateQuad(vertices, pos, tile, terrainPaletteIndex, offset, tile.Size);
		}

		public void UpdateCell(CPos cell)
		{
			var uv = cell.ToMPos(map.TileShape);
			var offset = rowStride * (uv.V - map.Bounds.Top) + 4 * (uv.U - map.Bounds.Left);

			GenerateTileVertices(updateCellVertices, 0, cell);
			vertexBuffer.SetData(updateCellVertices, offset, 4);
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			var cells = viewport.VisibleCells;

			// Only draw the rows that are visible.
			// VisibleCells is clamped to the map, so additional checks are unnecessary
			var firstRow = cells.TopLeft.ToMPos(map).V - map.Bounds.Top;
			var lastRow = cells.BottomRight.ToMPos(map).V - map.Bounds.Top + 1;

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, rowStride * firstRow, rowStride * (lastRow - firstRow),
				PrimitiveType.QuadList, wr.Theater.Sheet);

			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		public void Dispose()
		{
			vertexBuffer.Dispose();
		}
	}
}
