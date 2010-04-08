#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

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

		Renderer renderer;
		Map map;

		public TerrainRenderer(World world, Renderer renderer, WorldRenderer wr)
		{
			this.renderer = renderer;
			this.map = world.Map;

			Size tileSize = new Size( Game.CellSize, Game.CellSize );

			var tileMapping = new Cache<TileReference<ushort,byte>, Sprite>(
				x => SheetBuilder.SharedInstance.Add(world.TileSet.GetBytes(x), tileSize));

			Vertex[] vertices = new Vertex[4 * map.Height * map.Width];
			ushort[] indices = new ushort[6 * map.Height * map.Width];

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
				}

			terrainSheet = tileMapping[map.MapTiles[map.TopLeft.X, map.TopLeft.Y]].sheet;

			vertexBuffer = renderer.Device.CreateVertexBuffer( vertices.Length );
			vertexBuffer.SetData( vertices );

			indexBuffer = renderer.Device.CreateIndexBuffer( indices.Length );
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

			if (!Game.world.LocalPlayer.Shroud.HasGPS && Game.world.LocalPlayer.Shroud.bounds.HasValue)
			{
				var r = Game.world.LocalPlayer.Shroud.bounds.Value;
				if (firstRow < r.Top - map.YOffset)
					firstRow = r.Top - map.YOffset;

				if (firstRow > r.Bottom - map.YOffset)
					firstRow = r.Bottom - map.YOffset;
			}

			if( lastRow < firstRow ) lastRow = firstRow;

			renderer.SpriteShader.SetValue( "DiffuseTexture", terrainSheet.Texture );
			renderer.SpriteShader.Render(() =>
				renderer.DrawBatch(vertexBuffer, indexBuffer,
					new Range<int>(verticesPerRow * firstRow, verticesPerRow * lastRow),
					new Range<int>(indicesPerRow * firstRow, indicesPerRow * lastRow),
					terrainSheet.Texture, PrimitiveType.TriangleList, renderer.SpriteShader));

			foreach (var r in Game.world.WorldActor.traits.WithInterface<IRenderOverlay>())
				r.Render();
		}
	}
}
