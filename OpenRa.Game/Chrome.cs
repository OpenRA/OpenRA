using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using System.Drawing;
using IjwFramework.Types;
using IjwFramework.Collections;

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
		readonly Cache<string, Animation> clockAnimations;
		readonly List<Sprite> digitSprites;

		public Chrome(Renderer r)
		{
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 32, 256), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(512-320, 0, 320, 64), TextureChannel.Alpha);

			sprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u, 
					u => SpriteSheetBuilder.LoadSprite(u + "icon", ".shp"));

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			clockAnimations = new Cache<string, Animation>(
				s => { 
					var anim = new Animation("clock"); 
					anim.PlayFetchIndex("idle", ClockAnimFrame(s)); 
					return anim; 
				});

			digitSprites = Util.MakeArray(10, a => a)
				.Select(n => new Sprite(specialBin, new Rectangle(32 + 14 * n, 0, 14, 17), TextureChannel.Alpha)).ToList();
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
			chromeRenderer.DrawSprite(moneyBinSprite, new float2( Game.viewport.Width - 320, 0 ), 0);

			var moneyDigits = Game.LocalPlayer.Cash.ToString();
			var x = Game.viewport.Width - 155;
			foreach( var d in moneyDigits.Reverse() )
			{
				chromeRenderer.DrawSprite(digitSprites[d - '0'], new float2(x,6), 0);
				x -= 14;
			}
			chromeRenderer.Flush();

			DrawBuildPalette("Building");
		}

		static string[] groups = new string[] { "Building", "Defense", "Vehicle", "Ship", "Infantry", "Plane" };
		Dictionary<string, Sprite> sprites;

		const int NumClockFrames = 54;
		Func<int> ClockAnimFrame(string group)
		{
			return () =>
			{
				var producing = Game.LocalPlayer.Producing(group);
				if (producing == null) return 0;
				return (producing.TotalTime - producing.RemainingTime) * NumClockFrames / producing.TotalTime;
			};
		}

		void DrawBuildPalette(string queueName)
		{
			var buildItem = Game.LocalPlayer.Producing(queueName);
			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();
			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where( a => Rules.UnitInfo[a].TechLevel != -1 )
				.OrderBy( a => Rules.UnitInfo[a].TechLevel );

			var currentItem = Game.LocalPlayer.Producing(queueName);

			foreach (var item in allItems)
			{
				var rect = new Rectangle(Game.viewport.Width - (3 - x) * 64 - 10, 40 + 48 * y, 64, 48);
				buildPaletteRenderer.DrawSprite(sprites[item], Game.viewport.Location + new float2(rect.Location), 0);

				if (!buildableItems.Contains(item) || (currentItem != null && currentItem.Item != item))
					buildPaletteRenderer.DrawSprite(cantBuild.Image, Game.viewport.Location + new float2(rect.Location), 0);

				if (currentItem != null && currentItem.Item == item)
					buildPaletteRenderer.DrawSprite(clockAnimations[queueName].Image, 
						Game.viewport.Location + new float2(rect.Location), 0);

				buildItems.Add(Pair.New(rect, item));
				if (++x == 3) { x = 0; y++; }
			}

			buildPaletteRenderer.Flush();
		}
	}
}
