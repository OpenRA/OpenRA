using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IjwFramework.Collections;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;

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
		readonly Animation ready;

		readonly List<Pair<Rectangle, Action<bool>>> buttons = new List<Pair<Rectangle, Action<bool>>>();
		readonly Cache<string, Animation> clockAnimations;
		readonly List<Sprite> digitSprites;
		readonly Dictionary<string, Sprite[]> tabSprites;
		readonly Sprite[] shimSprites;
		readonly Sprite blank;

		public Chrome(Renderer r)
		{
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 32, 192), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(512 - 320, 0, 320, 32), TextureChannel.Alpha);

			blank = SheetBuilder.Add(new Size(64, 48), 16);

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
							new Rectangle(512 - (n + 1) * 27, 64 + i * 40, 27, 40),
							TextureChannel.Alpha))))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			clockAnimations = new Cache<string, Animation>(
				s =>
				{
					var anim = new Animation("clock");
					anim.PlayFetchIndex("idle", ClockAnimFrame(s));
					return anim;
				});

			digitSprites = Util.MakeArray(10, a => a)
				.Select(n => new Sprite(specialBin, new Rectangle(32 + 13 * n, 0, 13, 17), TextureChannel.Alpha)).ToList();

			shimSprites = new[] 
			{
				new Sprite( specialBin, new Rectangle( 0, 192, 192 +9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 202, 192 +9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 216, 9, 48 ), TextureChannel.Alpha ),
			};

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
		}

		public void Draw()
		{
			buttons.Clear();

			renderer.Device.DisableScissor();
			renderer.DrawText("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\nPower {4}/{5}\nReady: {6} (F8 to toggle)".F(
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue,
				Game.LocalPlayer.powerDrained,
				Game.LocalPlayer.powerProvided,
				Game.LocalPlayer.IsReady ? "Yes" : "No"
				), new int2(140, 5), Color.White);

			PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			chromeRenderer.DrawSprite(specialBinSprite, float2.Zero, 0);
			chromeRenderer.DrawSprite(moneyBinSprite, new float2(Game.viewport.Width - 320, 0), 0);

			DrawMoney();

			var x = Game.viewport.Width - 36 - 3 * 64;
			var y = 40;

			if (currentTab == null || !Rules.TechTree.BuildableItems(Game.LocalPlayer, currentTab).Any())
				ChooseAvailableTab();

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			foreach (var q in tabSprites)
			{
				var groupName = q.Key;
				if (!Rules.TechTree.BuildableItems(Game.LocalPlayer, groupName).Any())
				{
					CheckDeadTab(groupName);
					continue;
				}

				var producing = queue.Producing(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				chromeRenderer.DrawSprite(q.Value[index], new float2(x, y), 0);

				buttons.Add(Pair.New(new Rectangle(x, y, 27, 40), 
					(Action<bool>)(isLmb => currentTab = groupName)));
				y += 40;
			}

			chromeRenderer.Flush();
			DrawBuildPalette(currentTab);

			DrawChat();
		}

		void CheckDeadTab( string groupName )
		{
			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var item = queue.Producing( groupName );
			if (item != null)
				Game.controller.AddOrder(Order.CancelProduction(Game.LocalPlayer, item.Item));
		}

		void ChooseAvailableTab()
		{
			currentTab = tabSprites.Select(q => q.Key).FirstOrDefault(
				t => Rules.TechTree.BuildableItems(Game.LocalPlayer, t).Any());
		}

		void DrawMoney()
		{
			var moneyDigits = Game.LocalPlayer.DisplayCash.ToString();
			var x = Game.viewport.Width - 155;
			foreach (var d in moneyDigits.Reverse())
			{
				chromeRenderer.DrawSprite(digitSprites[d - '0'], new float2(x, 6), 0);
				x -= 14;
			}
		}

		void DrawChat()
		{
			var chatpos = new int2(400, Game.viewport.Height - 20);

			if (Game.chat.isChatting)
				RenderChatLine(Tuple.New(Color.White, "Chat:", Game.chat.typing), chatpos);

			foreach (var line in Game.chat.recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				RenderChatLine(line, chatpos);
			}
		}

		void RenderChatLine(Tuple<Color, string, string> line, int2 p)
		{
			var size = renderer.MeasureText(line.b);
			renderer.DrawText(line.b, p, line.a);
			renderer.DrawText(line.c, p + new int2(size.X + 10, 0), Color.White);
		}

		string currentTab = "Building";
		static string[] groups = new string[] { "Building", "Defense", "Infantry", "Vehicle", "Plane", "Ship" };
		Dictionary<string, Sprite> sprites;

		const int NumClockFrames = 54;
		Func<int> ClockAnimFrame(string group)
		{
			return () =>
			{
				var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
				var producing = queue.Producing( group );
				if (producing == null) return 0;
				return (producing.TotalTime - producing.RemainingTime) * NumClockFrames / producing.TotalTime;
			};
		}

		void DrawBuildPalette(string queueName)
		{
			if (queueName == null) return;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();

			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where(a => Rules.UnitInfo[a].TechLevel != -1)
				.OrderBy(a => Rules.UnitInfo[a].TechLevel);

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var currentItem = queue.Producing( queueName );

			var overlayBits = new List<Pair<Sprite, float2>>();

			foreach (var item in allItems)
			{
				var rect = new Rectangle(Game.viewport.Width - (3 - x) * 64, 40 + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				var isBuildingThis = currentItem != null && currentItem.Item == item;
				var isBuildingSomethingElse = currentItem != null && currentItem.Item != item;

				buildPaletteRenderer.DrawSprite(sprites[item], drawPos, 0);

				if (!buildableItems.Contains(item) || isBuildingSomethingElse)
					overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				if (isBuildingThis)
				{
					clockAnimations[queueName].Tick();
					buildPaletteRenderer.DrawSprite(clockAnimations[queueName].Image,
						drawPos, 0);

					var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

					if (currentItem.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (currentItem.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
				}

				var closureItem = item;
				buttons.Add(Pair.New(rect,
					(Action<bool>)(isLmb => HandleBuildPalette(closureItem, isLmb))));
				if (++x == 3) { x = 0; y++; }
			}

			while (x != 0)
			{
				var rect = new Rectangle(Game.viewport.Width - (3 - x) * 64, 40 + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				buildPaletteRenderer.DrawSprite(blank, drawPos, 0);
				buttons.Add(Pair.New(rect, (Action<bool>)(_ => { })));
				if (++x == 3) { x = 0; y++; }
			}

			foreach (var ob in overlayBits)
				buildPaletteRenderer.DrawSprite(ob.First, ob.Second, 0);

			buildPaletteRenderer.Flush();

			for (var j = 0; j < y; j++)
				chromeRenderer.DrawSprite(shimSprites[2], new float2(Game.viewport.Width - 192 - 9, 40 + 48 * j), 0);
			chromeRenderer.DrawSprite(shimSprites[0], new float2(Game.viewport.Width - 192 - 9, 40 - 9), 0);
			chromeRenderer.DrawSprite(shimSprites[1], new float2(Game.viewport.Width - 192 - 9, 40 - 1 + 48 * y), 0);
			chromeRenderer.Flush();
		}

		void HandleBuildPalette(string item, bool isLmb)
		{
			var player = Game.LocalPlayer;
			var group = Rules.UnitCategory[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var producing = queue.Producing( group );

			Sound.Play("ramenu1.aud");

			if (isLmb)
			{
				if (producing == null)
				{
					Game.controller.AddOrder(Order.StartProduction(player, item));
					Sound.Play("abldgin1.aud");
				}
				else if (producing.Item == item)
				{
					if (producing.Done)
					{
						if (group == "Building" || group == "Defense")
							Game.controller.orderGenerator = new PlaceBuilding(player.PlayerActor, item);
					}
					else
						Game.controller.AddOrder(Order.PauseProduction(player, item, false));
				}
				else
				{
					Sound.Play("progres1.aud");
				}
			}
			else
			{
				if (producing == null) return;
				if (item != producing.Item) return;

				if (producing.Paused || producing.Done)
				{
					Sound.Play("cancld1.aud");
					Game.controller.AddOrder(Order.CancelProduction(player, item));
				}
				else
				{
					Sound.Play("onhold1.aud");
					Game.controller.AddOrder(Order.PauseProduction(player, item, true));
				}
			}
		}

		public bool HandleInput(MouseInput mi)
		{
			var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();

			if (action == null)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				action(mi.Button == MouseButtons.Left);

			return true;
		}

		public bool HitTest(int2 mousePos)
		{
			return buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}
	}
}
