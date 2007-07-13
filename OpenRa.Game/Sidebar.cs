using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.TechTree;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;
using System.IO;


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

		public Sidebar(Race race, Renderer renderer)
		{
			techTree.CurrentRace = race;
			techTree.Build("FACT");
			techTree.Build("POWR");
			techTree.Build("BARR");
			techTree.Build("PROC");
			techTree.Build("WEAP");
			techTree.Build("DOME");
			this.renderer = renderer;
			this.spriteRenderer = new SpriteRenderer(renderer);

			package = new Package("../../../hires.mix");
			LoadSprites("../../../buildings.txt");
			LoadSprites("../../../units.txt");

			sprites.Add("BLANK", CoreSheetBuilder.Add(new Size(64, 48), 16));
			techTree.CurrentRace = race;
		}

		void LoadSprites(string filename)
		{
			foreach (string line in File.ReadAllLines(filename))
			{
				string key = line.Substring(0, line.IndexOf(','));
				sprites.Add(key, SpriteSheetBuilder.LoadSprite(package, key + "icon.shp"));
			}
		}

		public void Paint(Size clientSize, PointF scrollOffset)
		{
			int y1 = 0, y2 = 0;
			foreach (Item i in techTree.BuildableBuildings)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;
				PointF location = new PointF(clientSize.Width - 128 + scrollOffset.X, y1 + scrollOffset.Y);
				spriteRenderer.DrawSprite(sprite, location);
				y1 += 48;
			}
			foreach (Item i in techTree.BuildableUnits)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;
				PointF location = new PointF(clientSize.Width - 64 + scrollOffset.X, y2 + scrollOffset.Y);
				spriteRenderer.DrawSprite(sprite, location);
				y2 += 48;
			}
			while (y2 < clientSize.Height)
			{
				Sprite sprite = sprites["BLANK"];
				PointF location = new PointF(clientSize.Width - 64 + scrollOffset.X, y2 + scrollOffset.Y);
				spriteRenderer.DrawSprite(sprite, location);
				y2 += 48;
			}

			spriteRenderer.Flush();
		}
	}
}
