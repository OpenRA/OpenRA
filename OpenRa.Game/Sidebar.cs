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
	class Sidebar
	{
		TechTree.TechTree techTree = new TechTree.TechTree();

		SpriteRenderer spriteRenderer;
		Package package;

		Sprite blank;

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();

		public Sidebar(Race race, Renderer renderer)
		{
			techTree.CurrentRace = race;
			techTree.Build("FACT", true);
			spriteRenderer = new SpriteRenderer(renderer, false);

			package = new Package("../../../hires.mix");
			LoadSprites("../../../buildings.txt");
			LoadSprites("../../../units.txt");

			blank = SheetBuilder.Add(new Size(64, 48), 16);
		}

		void LoadSprites(string filename)
		{
			foreach (string line in File.ReadAllLines(filename))
			{
				string key = line.Substring(0, line.IndexOf(','));
				sprites.Add(key, SpriteSheetBuilder.LoadSprite(package, key + "icon.shp"));
			}
		}

		void DrawSprite(Sprite s, ref PointF p)
		{
			spriteRenderer.DrawSprite(s, p, 0);
			p.Y += 48;
		}

		void Fill(Size clientSize, PointF p)
		{
			while (p.Y < clientSize.Height)
				DrawSprite(blank, ref p);
		}

		public void Paint(Size clientSize, PointF scrollOffset)
		{
			PointF buildPos = new PointF(clientSize.Width - 128 + scrollOffset.X, scrollOffset.Y);
			PointF unitPos = new PointF(clientSize.Width - 64 + scrollOffset.X, scrollOffset.Y);
			
			foreach (Item i in techTree.BuildableItems)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;

				if (i.IsStructure)
					DrawSprite( sprite, ref buildPos );
				else
					DrawSprite( sprite, ref unitPos );
			}

			Fill(clientSize, buildPos);
			Fill(clientSize, unitPos);

			spriteRenderer.Flush();
		}
	}
}
