using System.Drawing;
using OpenRa.GlRenderer;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	class TerrainRenderer
	{
		VertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		Sheet terrainSheet;

		Renderer renderer;
		Map map;
		OverlayRenderer overlayRenderer;

		public TerrainRenderer(World world, Renderer renderer, WorldRenderer wr)
		{
			this.renderer = renderer;
			this.map = world.Map;

			Size tileSize = new Size( Game.CellSize, Game.CellSize );

			var tileMapping = new Cache<TileReference, Sprite>(
				x => SheetBuilder.Add(world.TileSet.GetBytes(x), tileSize));

			Vertex[] vertices = new Vertex[4 * map.Height * map.Width];
			ushort[] indices = new ushort[6 * map.Height * map.Width];

			int nv = 0;
			int ni = 0;
			for( int j = map.YOffset ; j < map.YOffset + map.Height ; j++ )
				for( int i = map.XOffset ; i < map.XOffset + map.Width; i++ )
				{
					Sprite tile = tileMapping[map.MapTiles[i, j]];
					// TODO: The zero below should explicitly refer to the terrain palette, but this code is called
					// before the palettes are created
					Util.FastCreateQuad(vertices, indices, Game.CellSize * new float2(i, j), tile, 0, nv, ni, tile.size);
					nv += 4;
					ni += 6;
				}

			terrainSheet = tileMapping[map.MapTiles[map.XOffset, map.YOffset]].sheet;

			vertexBuffer = new VertexBuffer<Vertex>( renderer.Device, vertices.Length, Vertex.Format );
			vertexBuffer.SetData( vertices );

			indexBuffer = new IndexBuffer( renderer.Device, indices.Length );
			indexBuffer.SetData( indices );

			overlayRenderer = new OverlayRenderer( renderer, map );
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

			renderer.SpriteShader.Quality = ShaderQuality.Low;
			renderer.SpriteShader.Render(() =>
				renderer.DrawBatch(vertexBuffer, indexBuffer,
					new Range<int>(verticesPerRow * firstRow, verticesPerRow * lastRow),
					new Range<int>(indicesPerRow * firstRow, indicesPerRow * lastRow),
					terrainSheet.Texture, PrimitiveType.TriangleList, renderer.SpriteShader));

			overlayRenderer.Draw();
		}
	}
}
