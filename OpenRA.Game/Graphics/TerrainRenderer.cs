#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		IIndexBuffer indexBuffer;
		Sheet terrainSheet;

		World world;
		Map map;

		public TerrainRenderer(World world, WorldRenderer wr)
		{
			this.world = world;
			this.map = world.Map;

			Size tileSize = new Size( Game.CellSize, Game.CellSize );
			var tileMapping = new Cache<TileReference<ushort,byte>, Sprite>(
				x => Game.modData.SheetBuilder.Add(world.TileSet.GetBytes(x), tileSize));

			Vertex[] vertices = new Vertex[4 * map.Bounds.Height * map.Bounds.Width];
			ushort[] indices = new ushort[6 * map.Bounds.Height * map.Bounds.Width];

			terrainSheet = tileMapping[map.MapTiles[map.Bounds.Left, map.Bounds.Top]].sheet;

			int nv = 0;
			int ni = 0;
			
			for( int j = map.Bounds.Top; j < map.Bounds.Bottom; j++ )
				for( int i = map.Bounds.Left; i < map.Bounds.Right; i++ )
				{
					Sprite tile = tileMapping[map.MapTiles[i, j]];
					// TODO: The zero below should explicitly refer to the terrain palette, but this code is called
					// before the palettes are created. Therefore assumes that "terrain" is the first palette to be defined
					Util.FastCreateQuad(vertices, indices, Game.CellSize * new float2(i, j), tile, 0, nv, ni, tile.size);
					nv += 4;
					ni += 6;
					
					if (tileMapping[map.MapTiles[i, j]].sheet != terrainSheet)
						throw new InvalidOperationException("Terrain sprites span multiple sheets");
				}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer( vertices.Length );
			vertexBuffer.SetData( vertices, nv );

			indexBuffer = Game.Renderer.Device.CreateIndexBuffer( indices.Length );
			indexBuffer.SetData( indices, ni );
		}

		public void Draw( WorldRenderer wr, Viewport viewport )
		{
			int indicesPerRow = map.Bounds.Width * 6;
			int verticesPerRow = map.Bounds.Width * 4;

			int visibleRows = (int)(viewport.Height * 1f / Game.CellSize + 2);

			int firstRow = (int)(viewport.Location.Y * 1f / Game.CellSize - map.Bounds.Top);
			int lastRow = firstRow + visibleRows;

			if (lastRow < 0 || firstRow > map.Bounds.Height)
				return;

			if (firstRow < 0) firstRow = 0;
			if (lastRow > map.Bounds.Height) lastRow = map.Bounds.Height;

			if (world.LocalPlayer != null && !world.LocalShroud.Disabled && world.LocalShroud.Bounds.HasValue)
			{
				var r = world.LocalShroud.Bounds.Value;
				if (firstRow < r.Top - map.Bounds.Top)
					firstRow = r.Top - map.Bounds.Top;

				if (firstRow > r.Bottom - map.Bounds.Top)
					firstRow = r.Bottom - map.Bounds.Top;
			}

			if( lastRow < firstRow ) lastRow = firstRow;

			Game.Renderer.SpriteShader.SetValue( "DiffuseTexture", terrainSheet.Texture );
			Game.Renderer.SpriteShader.Render(() =>
				Game.Renderer.DrawBatch(vertexBuffer, indexBuffer,
					new Range<int>(verticesPerRow * firstRow, verticesPerRow * lastRow),
					new Range<int>(indicesPerRow * firstRow, indicesPerRow * lastRow),
					PrimitiveType.TriangleList, Game.Renderer.SpriteShader));

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render( wr );
		}
	}
}
