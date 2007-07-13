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

		SpriteRenderer spriteRenderer;
		Package package;

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();

		void LoadSprite(string name)
		{
			sprites.Add(name, SpriteSheetBuilder.LoadSprite(package, name + "icon.shp"));
		}

		public Sidebar(Race race, Renderer renderer)
		{
			this.renderer = renderer;
			this.spriteRenderer = new SpriteRenderer(renderer);

			package = new Package("../../../hires.mix");
			LoadSprite("E7");
			LoadSprite("E6");
			LoadSprite("POWR");
			techTree.CurrentRace = race;
		}

		public void Paint(PointF scrollOffset)
		{
			int x = 0, y = 0;
			foreach (Item i in techTree.BuildableBuildings)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;
				PointF location = new PointF(x + scrollOffset.X, y + scrollOffset.Y);
				spriteRenderer.DrawSprite(sprite, location);
				y += 48;
			}

			spriteRenderer.Flush();
		}
	}
}
