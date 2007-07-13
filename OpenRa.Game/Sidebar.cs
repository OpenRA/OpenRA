using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.TechTree;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	using Sprite = SheetRectangle<Sheet>;
	class Sidebar
	{
		TechTree.TechTree techTree = new TechTree.TechTree();
		Renderer renderer;

		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		const int spritesPerBatch = 1024;
		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();


		public Sidebar(Race race, Renderer renderer)
		{
			Package package = new Package("../../../hires.mix");
			sprites.Add("e7", SpriteSheetBuilder.LoadSprite(package, "e7icon.shp"));
			sprites.Add("e6", SpriteSheetBuilder.LoadSprite(package, "e6icon.shp"));
			techTree.CurrentRace = race;
			this.renderer = renderer;
			vertexBuffer = new FvfVertexBuffer<Vertex>(renderer.Device, 4 * spritesPerBatch, Vertex.Format);
			indexBuffer = new IndexBuffer(renderer.Device, 6 * spritesPerBatch);
		}

		public void Paint(PointF scrollOffset)
		{
			List<Vertex> vertices = new List<Vertex>();
			List<ushort> indicies = new List<ushort>();

			int x = 0, y = 0;

			//foreach (SheetRectangle<Sheet> sprite in sprites.Values)
			foreach (Item i in techTree.BuildableBuildings)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;
				PointF location = new PointF(x + scrollOffset.X, y + scrollOffset.Y);
				Util.CreateQuad(vertices, indicies, location, sprite, 0);
				y += 48;
			}

			renderer.DrawWithShader(ShaderQuality.Low, delegate 
			{
				vertexBuffer.SetData(vertices.ToArray());
				indexBuffer.SetData(indicies.ToArray());
				foreach (Sprite sprite in sprites.Values)
					renderer.DrawBatch(vertexBuffer, indexBuffer, new Range<int>(0, vertices.Count), new Range<int>(0, indicies.Count), sprite.sheet.Texture);
			});
		}
	}
}
