using System.Drawing;
using System.Windows.Forms;
using Ijw.DirectX;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class TerrainRenderer
	{
		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		Sheet terrainSheet;
		public TileSet tileSet;

		Renderer renderer;
		Map map;
		OverlayRenderer overlayRenderer;

		public TerrainRenderer(Renderer renderer, Map map)
		{
			this.renderer = renderer;
			this.map = map;
			overlayRenderer = new OverlayRenderer( renderer, map );

			tileSet = new TileSet( map.TileSuffix );

			Size tileSize = new Size( Game.CellSize, Game.CellSize );

			SheetBuilder.ForceNewSheet();

			var tileMapping = new Cache<TileReference, Sprite>(
				x => SheetBuilder.Add(tileSet.GetBytes(x), tileSize));

			Vertex[] vertices = new Vertex[4 * map.Height * map.Width];
			ushort[] indices = new ushort[6 * map.Height * map.Width];

			int nv = 0;
			int ni = 0;
			for( int j = map.YOffset ; j < map.YOffset + map.Height ; j++ )
				for( int i = map.XOffset ; i < map.XOffset + map.Width; i++ )
				{
					Sprite tile = tileMapping[map.MapTiles[i, j]];
					Util.FastCreateQuad(vertices, indices, Game.CellSize * new float2(i, j), tile, 0, nv, ni);
					nv += 4;
					ni += 6;
				}

			terrainSheet = tileMapping[map.MapTiles[map.XOffset, map.YOffset]].sheet;

			vertexBuffer = new FvfVertexBuffer<Vertex>( renderer.Device, vertices.Length, Vertex.Format );
			vertexBuffer.SetData( vertices );

			indexBuffer = new IndexBuffer( renderer.Device, indices.Length );
			indexBuffer.SetData( indices );
		}

		public void Draw( Viewport viewport )
		{
			int indicesPerRow = map.Width * 6;
			int verticesPerRow = map.Width * 4;

			int visibleRows = (int)(viewport.Width / 24.0f + 2);

			int firstRow = (int)((viewport.Location.Y) / 24.0f - map.YOffset);
			int lastRow = firstRow + visibleRows;

			if (lastRow < 0 || firstRow > map.Height)
				return;

			if (firstRow < 0) firstRow = 0;
			if (lastRow > map.Height) lastRow = map.Height;

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
