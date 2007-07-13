using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.TechTree;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Sidebar
	{
		TechTree.TechTree techTree = new TechTree.TechTree();
		Renderer renderer;

		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		const int spritesPerBatch = 1024;
		SheetRectangle<Sheet> sprite;


		public Sidebar(Race race, Renderer renderer)
		{
			Package package = new Package("../../../hires.mix");
			sprite = BeedeeSheetBuilder.LoadSprite(package, "e7icon.shp");
			techTree.CurrentRace = race;
			this.renderer = renderer;
			vertexBuffer = new FvfVertexBuffer<Vertex>(renderer.Device, 4 * spritesPerBatch, Vertex.Format);
			indexBuffer = new IndexBuffer(renderer.Device, 6 * spritesPerBatch);
		}

		public void Paint(PointF scrollOffset)
		{
			List<Vertex> vertices = new List<Vertex>();
			List<ushort> indicies = new List<ushort>();

			PointF location = new PointF(0 + scrollOffset.X, 0 + scrollOffset.Y);

			Util.CreateQuad(vertices, indicies, location, sprite, 0);

			renderer.DrawWithShader(ShaderQuality.Low, delegate 
			{
				vertexBuffer.SetData(vertices.ToArray());
				indexBuffer.SetData(indicies.ToArray());
				renderer.DrawBatch(vertexBuffer, indexBuffer, new Range<int>(0, vertices.Count), new Range<int>(0, indicies.Count), sprite.sheet.Texture);
			});
		}
	}
}
