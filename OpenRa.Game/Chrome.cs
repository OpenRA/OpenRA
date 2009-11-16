using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using System.Drawing;

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
		}

		public void Draw()
		{
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

		static string[] groups = new string[] { "Building", "Vehicle", "Ship", "Infantry", "Plane" };
		Dictionary<string, Sprite> sprites;

		void DrawBuildPalette(string queueName)
		{
			var buildItem = Game.LocalPlayer.Producing(queueName);
			var x = 0;
			var y = 0;

			foreach (var item in Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName))
			{
				buildPaletteRenderer.DrawSprite(sprites[item],
					new float2(
						Game.viewport.Width - (3 - x) * 64 - 20,
						32 + 48 * y), 0);

				if (++x == 3)
				{
					x = 0; y++;
				}
			}

			buildPaletteRenderer.Flush();
		}
	}
}
