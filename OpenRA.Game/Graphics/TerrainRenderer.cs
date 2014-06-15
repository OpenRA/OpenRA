#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

			for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
			{
				for (var i = map.Bounds.Left; i < map.Bounds.Right; i++)
				{
					var tile = wr.Theater.TileSprite(map.MapTiles.Value[i, j]);
					var pos = wr.ScreenPosition(new CPos(i, j).CenterPosition) - 0.5f * tile.size;
					Util.FastCreateQuad(vertices, pos, tile, terrainPalette, nv, tile.size);
					nv += 4;
				}
			}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(vertices.Length);
			vertexBuffer.SetData(vertices, nv);
		}

		public void Draw(WorldRenderer wr, Viewport viewport)
		{
			var verticesPerRow = 4*map.Bounds.Width;
			var bounds = viewport.CellBounds;
			var firstRow = bounds.Top - map.Bounds.Top;
			var lastRow = bounds.Bottom - map.Bounds.Top;

			if (lastRow < 0 || firstRow > map.Bounds.Height)
				return;

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, verticesPerRow * firstRow, verticesPerRow * (lastRow - firstRow),
				PrimitiveType.QuadList, wr.Theater.Sheet);

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}
	}
}
