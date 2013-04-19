#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	class TerrainRenderer
	{
		IVertexBuffer<Vertex> vertexBuffer;
		Sheet terrainSheet;

		World world;
		Map map;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			this.world = world;
			this.map = world.Map;

			var tileSize = new Size( Game.CellSize, Game.CellSize );
			var tileMapping = new Cache<TileReference<ushort,byte>, Sprite>(
				x => Game.modData.SheetBuilder.Add(world.TileSet.GetBytes(x), tileSize));

			var vertices = new Vertex[4 * map.Bounds.Height * map.Bounds.Width];

			terrainSheet = tileMapping[map.MapTiles.Value[map.Bounds.Left, map.Bounds.Top]].sheet;

			int nv = 0;

			var terrainPalette = wr.Palette("terrain").Index;

			for( int j = map.Bounds.Top; j < map.Bounds.Bottom; j++ )
				for( int i = map.Bounds.Left; i < map.Bounds.Right; i++ )
				{
					var tile = tileMapping[map.MapTiles.Value[i, j]];
					// TODO: move GetPaletteIndex out of the inner loop.
					Util.FastCreateQuad(vertices, Game.CellSize * new float2(i, j), tile, terrainPalette, nv, tile.size);
					nv += 4;

					if (tileMapping[map.MapTiles.Value[i, j]].sheet != terrainSheet)
						throw new InvalidOperationException("Terrain sprites span multiple sheets. Try increasing Game.Settings.Graphics.SheetSize.");
				}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer( vertices.Length );
			vertexBuffer.SetData( vertices, nv );
		}

		public void Draw( WorldRenderer wr, Viewport viewport )
		{
			int verticesPerRow = map.Bounds.Width * 4;

			int visibleRows = (int)(viewport.Height * 1f / Game.CellSize / viewport.Zoom + 2);

			int firstRow = (int)(viewport.Location.Y * 1f / Game.CellSize - map.Bounds.Top);
			int lastRow = firstRow + visibleRows;

			if (lastRow < 0 || firstRow > map.Bounds.Height)
				return;

			if (world.VisibleBounds.HasValue)
			{
				var r = world.VisibleBounds.Value;
				if (firstRow < r.Top - map.Bounds.Top)
					firstRow = r.Top - map.Bounds.Top;

				if (firstRow > r.Bottom - map.Bounds.Top)
					firstRow = r.Bottom - map.Bounds.Top;
			}

			if (firstRow < 0) firstRow = 0;
			if (lastRow > map.Bounds.Height) lastRow = map.Bounds.Height;

			if( lastRow < firstRow ) lastRow = firstRow;

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, verticesPerRow * firstRow, verticesPerRow * (lastRow - firstRow),
				PrimitiveType.QuadList, terrainSheet);

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render( wr );
		}
	}
}
