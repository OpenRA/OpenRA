using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using System.Drawing;
using IjwFramework.Types;
using IjwFramework.Collections;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Chrome : IHandleInput
	{
		readonly Renderer renderer;
		readonly Sheet specialBin;
		readonly SpriteRenderer chromeRenderer;
		readonly Sprite specialBinSprite;
		readonly Sprite moneyBinSprite;
		readonly SpriteRenderer buildPaletteRenderer;
		readonly Animation cantBuild;

		readonly List<Pair<Rectangle, Action<bool>>> buildItems = new List<Pair<Rectangle, Action<bool>>>();
		readonly Cache<string, Animation> clockAnimations;
		readonly List<Sprite> digitSprites;
		readonly Dictionary<string, Sprite[]> tabSprites;
		readonly Sprite[] shimSprites;

		public Chrome(Renderer r)
		{
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 32, 192), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(512-320, 0, 320, 64), TextureChannel.Alpha);

			sprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u, 
					u => SpriteSheetBuilder.LoadSprite(u + "icon", ".shp"));

			tabSprites = groups.Select(
				(g, i) => Pair.New(g, 
					Util.MakeArray(3, 
						n => new Sprite(specialBin, 
							new Rectangle(512 - (n+1) * 27, 64 + i * 40, 27, 40), 
							TextureChannel.Alpha))))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			clockAnimations = new Cache<string, Animation>(
				s => { 
					var anim = new Animation("clock"); 
					anim.PlayFetchIndex("idle", ClockAnimFrame(s)); 
					return anim; 
				});

			digitSprites = Util.MakeArray(10, a => a)
				.Select(n => new Sprite(specialBin, new Rectangle(32 + 13 * n, 0, 13, 17), TextureChannel.Alpha)).ToList();

			shimSprites = new [] 
			{
				new Sprite( specialBin, new Rectangle( 0, 192, 192 +9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 202, 192 +9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 216, 9, 48 ), TextureChannel.Alpha ),
			};
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

			x = Game.viewport.Width - 36 - 3 * 64;
			var y = 40;
			
			foreach (var q in tabSprites)
			{
				if (!Rules.TechTree.BuildableItems(Game.LocalPlayer, q.Key).Any()) continue;
				var index = q.Key == "Building" ? 2 : 0;
				chromeRenderer.DrawSprite(q.Value[index], new float2(x, y), 0);
				y += 40;
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

			if (!buildableItems.Any())
				return;

			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where( a => Rules.UnitInfo[a].TechLevel != -1 )
				.OrderBy( a => Rules.UnitInfo[a].TechLevel );

			var currentItem = Game.LocalPlayer.Producing(queueName);

			foreach (var item in allItems)
			{
				var rect = new Rectangle(Game.viewport.Width - (3 - x) * 64, 40 + 48 * y, 64, 48);
				buildPaletteRenderer.DrawSprite(sprites[item], Game.viewport.Location + new float2(rect.Location), 0);

				if (!buildableItems.Contains(item) || (currentItem != null && currentItem.Item != item))
					buildPaletteRenderer.DrawSprite(cantBuild.Image, Game.viewport.Location + new float2(rect.Location), 0);

				if (currentItem != null && currentItem.Item == item)
				{
					clockAnimations[queueName].Tick();
					buildPaletteRenderer.DrawSprite(clockAnimations[queueName].Image,
						Game.viewport.Location + new float2(rect.Location), 0);
				}

				var closureItem = item;
				buildItems.Add(Pair.New(rect,
					(Action<bool>)(isLmb => HandleBuildPalette(closureItem, isLmb))));
				if (++x == 3) { x = 0; y++; }
			}

			buildPaletteRenderer.Flush();

			for (var j = 0; j <= y; j++)
				chromeRenderer.DrawSprite(shimSprites[2], new float2(Game.viewport.Width - 192 - 9, 40 + 48 * j), 0);
			chromeRenderer.DrawSprite(shimSprites[0], new float2(Game.viewport.Width - 192 - 9, 40 - 9), 0);
			chromeRenderer.DrawSprite(shimSprites[1], new float2(Game.viewport.Width - 192 - 9, 40 - 1 + 48 + 48 * y), 0);
			chromeRenderer.Flush();
		}

		void HandleBuildPalette(string item, bool isLmb)
		{
			var player = Game.LocalPlayer;
			var group = Rules.UnitCategory[item];
			var producing = player.Producing(group);

			if (isLmb)
			{
				if (producing == null)
				{
					Game.controller.AddOrder(Order.StartProduction(player, item));
					Game.PlaySound("abldgin1.aud", false);
				}
				else if (producing.Item == item)
				{
					if (producing.Done)
					{
						if (group == "Building" || group == "Defense")
							Game.controller.orderGenerator = new PlaceBuilding(player, item);
					}
					else
						Game.controller.AddOrder(Order.PauseProduction(player, item, false));
				}
				else
				{
					Game.PlaySound("progres1.aud", false);
				}
			}
			else
			{
				if (producing == null) return;
				if (item != producing.Item) return;

				if (producing.Paused || producing.Done)
				{
					Game.PlaySound("cancld1.aud", false);
					Game.controller.AddOrder(Order.CancelProduction(player, item));
				}
				else
				{
					Game.PlaySound("onhold1.aud", false);
					Game.controller.AddOrder(Order.PauseProduction(player, item, true));
				}
			}
		}

		public bool HandleInput(MouseInput mi)
		{
			var action = buildItems.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select( a => a.Second ).FirstOrDefault();

			if (action == null)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				action(mi.Button == MouseButtons.Left);

			return true;
		}

		public bool HitTest(int2 mousePos)
		{
			return buildItems.Any(a => a.First.Contains(mousePos.ToPoint()));
		}
	}
}
