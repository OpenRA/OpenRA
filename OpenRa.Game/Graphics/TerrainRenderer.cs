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
		Region region;

		Renderer renderer;
		Map map;
        Viewport viewport;
		OverlayRenderer overlayRenderer;

		public TerrainRenderer(Renderer renderer, Map map, Viewport viewport)
		{
			this.renderer = renderer;
            this.viewport = viewport;
			region = Region.Create(viewport, DockStyle.Left, viewport.Width - 128, Draw, null );
			viewport.AddRegion(region);
			this.map = map;
			overlayRenderer = new OverlayRenderer( renderer, map );

			tileSet = new TileSet( map.TileSuffix );

            Size tileSize = new Size( 24, 24 );

            var tileMapping = new Cache<TileReference, Sprite>(
                x => SheetBuilder.Add(tileSet.GetBytes(x), tileSize));

            Vertex[] vertices = new Vertex[4 * map.Height * map.Width];
            ushort[] indices = new ushort[6 * map.Height * map.Width];

            int nv = 0;
            int ni = 0;
			for( int j = 0 ; j < map.Height ; j++ )
                for (int i = 0; i < map.Width; i++)
                {
                    Sprite tile = tileMapping[map.MapTiles[i + map.XOffset, j + map.YOffset]];
                    Util.FastCreateQuad(vertices, indices, 24 * new float2(i, j), tile, 0, nv, ni);
                    nv += 4;
                    ni += 6;
                }

            terrainSheet = tileMapping[map.MapTiles[map.XOffset, map.YOffset]].sheet;

			vertexBuffer = new FvfVertexBuffer<Vertex>( renderer.Device, vertices.Length, Vertex.Format );
			vertexBuffer.SetData( vertices );

			indexBuffer = new IndexBuffer( renderer.Device, indices.Length );
			indexBuffer.SetData( indices );
		}

		void Draw()
		{
			int indicesPerRow = map.Width * 6;
			int verticesPerRow = map.Width * 4;

			int visibleRows = (int)(region.Size.Y / 24.0f + 2);

			int firstRow = (int)((region.Position.Y + viewport.Location.Y) / 24.0f);
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
                    terrainSheet.Texture, PrimitiveType.TriangleList));

			overlayRenderer.Draw();
		}
	}
}
