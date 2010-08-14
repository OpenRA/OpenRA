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
				x => SheetBuilder.SharedInstance.Add(world.TileSet.GetBytes(x), tileSize));

			Vertex[] vertices = new Vertex[4 * map.Height * map.Width];
			ushort[] indices = new ushort[6 * map.Height * map.Width];

			terrainSheet = tileMapping[map.MapTiles[map.TopLeft.X, map.TopLeft.Y]].sheet;

			int nv = 0;
			int ni = 0;
			for( int j = map.TopLeft.Y ; j < map.BottomRight.Y; j++ )
				for( int i = map.TopLeft.X ; i < map.BottomRight.X; i++ )
				{
					Sprite tile = tileMapping[map.MapTiles[i, j]];
					// TODO: The zero below should explicitly refer to the terrain palette, but this code is called
					// before the palettes are created
					Util.FastCreateQuad(vertices, indices, Game.CellSize * new float2(i, j), tile, 0, nv, ni, tile.size);
					nv += 4;
					ni += 6;
					
					if (tileMapping[map.MapTiles[i, j]].sheet != terrainSheet)
						throw new InvalidOperationException("Terrain sprites span multiple sheets");
				}

			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer( vertices.Length );
			vertexBuffer.SetData( vertices );

			indexBuffer = Game.Renderer.Device.CreateIndexBuffer( indices.Length );
			indexBuffer.SetData( indices );
		}

		public void Draw( Viewport viewport )
		{
			int indicesPerRow = map.Width * 6;
			int verticesPerRow = map.Width * 4;

			int visibleRows = (int)(viewport.Height / 24.0f + 2);

			int firstRow = (int)((viewport.Location.Y) / 24.0f - map.YOffset);
			int lastRow = firstRow + visibleRows;

			if (lastRow < 0 || firstRow > map.Height)
				return;

			if (firstRow < 0) firstRow = 0;
			if (lastRow > map.Height) lastRow = map.Height;

			if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.Disabled && world.LocalPlayer.Shroud.Bounds.HasValue)
			{
				var r = world.LocalPlayer.Shroud.Bounds.Value;
				if (firstRow < r.Top - map.YOffset)
					firstRow = r.Top - map.YOffset;

				if (firstRow > r.Bottom - map.YOffset)
					firstRow = r.Bottom - map.YOffset;
			}

			if( lastRow < firstRow ) lastRow = firstRow;

			Game.Renderer.SpriteShader.SetValue( "DiffuseTexture", terrainSheet.Texture );
			Game.Renderer.SpriteShader.Render(() =>
				Game.Renderer.DrawBatch(vertexBuffer, indexBuffer,
					new Range<int>(verticesPerRow * firstRow, verticesPerRow * lastRow),
					new Range<int>(indicesPerRow * firstRow, indicesPerRow * lastRow),
					PrimitiveType.TriangleList, Game.Renderer.SpriteShader));

			foreach (var r in world.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render();
		}
	}
}
