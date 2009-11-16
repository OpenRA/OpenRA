using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using System.Drawing;
using IjwFramework.Types;

namespace OpenRa.Game
{
	class Chrome
	{
		readonly Renderer renderer;
		readonly Sheet specialBin;
		readonly SpriteRenderer chromeRenderer;
		readonly Sprite specialBinSprite;
		readonly Sprite moneyBinSprite;
		readonly SpriteRenderer buildPaletteRenderer;
		readonly Animation cantBuild;

		readonly List<Pair<Rectangle, string>> buildItems = new List<Pair<Rectangle, string>>();

		public Chrome(Renderer r)
		{
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 64, 256), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(128, 0, 384, 64), TextureChannel.Alpha);

			sprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u, 
					u => SpriteSheetBuilder.LoadSprite(u + "icon", ".shp"));

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
		}

		public void Draw()
		{
			buildItems.Clear();

			renderer.Device.DisableScissor();
			renderer.DrawText(string.Format("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\n$ {4}\nPower {5}",
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue,
				Game.LocalPlayer.Cash,
				Game.LocalPlayer.GetTotalPower()
				), new int2(140, 5), Color.White);

			PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			chromeRenderer.DrawSprite(specialBinSprite, float2.Zero, 0);
			chromeRenderer.DrawSprite(moneyBinSprite, new float2( Game.viewport.Width - 384, 0 ), 0);
			chromeRenderer.Flush();

			DrawBuildPalette("Building");
		}

		static string[] groups = new string[] { "Building", "Defense", "Vehicle", "Ship", "Infantry", "Plane" };
		Dictionary<string, Sprite> sprites;

		void DrawBuildPalette(string queueName)
		{
			var buildItem = Game.LocalPlayer.Producing(queueName);
			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();
			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.OrderBy( a => Rules.UnitInfo[a].TechLevel );
			foreach (var item in allItems)
			{
				if (Rules.UnitInfo[item].TechLevel == -1) continue;
				var rect = new Rectangle(Game.viewport.Width - (3 - x) * 64 - 20, 32 + 48 * y, 64, 48);
				buildPaletteRenderer.DrawSprite(sprites[item], Game.viewport.Location + new float2(rect.Location), 0);

				if (!buildableItems.Contains(item))
				{
					/* don't have the necessary prereqs! */
					buildPaletteRenderer.DrawSprite(cantBuild.Image, Game.viewport.Location + new float2(rect.Location), 0);
				}

				buildItems.Add(Pair.New(rect, item));
				if (++x == 3) { x = 0; y++; }
			}

			buildPaletteRenderer.Flush();
		}
	}
}
