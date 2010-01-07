using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IjwFramework.Types;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Orders;
using OpenRa.Game.Support;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class Chrome : IHandleInput
	{
		readonly Renderer renderer;
		readonly LineRenderer lineRenderer;
		readonly Sheet specialBin;
		readonly SpriteRenderer chromeRenderer;
		readonly Sprite specialBinSprite;
		readonly Sprite moneyBinSprite;
		readonly Sprite tooltipSprite;
		readonly Sprite powerIndicatorSprite;
		readonly Sprite powerLevelTopSprite;
		readonly Sprite powerLevelBottomSprite;
		
		readonly Animation repairButton;
		readonly Animation sellButton;
		readonly Animation pwrdownButton;
		readonly Animation optionsButton;
				
		readonly SpriteRenderer buildPaletteRenderer;
		readonly Animation cantBuild;
		readonly Animation ready;
		readonly Animation clock;

		readonly List<Pair<Rectangle, Action<bool>>> buttons = new List<Pair<Rectangle, Action<bool>>>();
		readonly List<Sprite> digitSprites;
		readonly Dictionary<string, Sprite[]> tabSprites;
		readonly Sprite[] shimSprites;
		readonly Sprite blank;
	
		readonly int paletteColumns;
		readonly int2 paletteOrigin;
		bool hadRadar = false;
		bool optionsPressed = false;
		const int MinRows = 4;
		
		public Chrome(Renderer r)
		{
			// Positioning of chrome elements
			// Build palette
			paletteColumns = 4;
			paletteOrigin = new int2(Game.viewport.Width - paletteColumns * 64 - 9 - 20, 240 - 9);
			
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			lineRenderer = new LineRenderer(renderer);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 32, 192), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(512 - 320, 0, 320, 32), TextureChannel.Alpha);
			tooltipSprite = new Sprite(specialBin, new Rectangle(0, 288, 272, 136), TextureChannel.Alpha);
			
			var powerIndicator = new Animation("power");
			powerIndicator.PlayRepeating("power-level-indicator");
			powerIndicatorSprite = powerIndicator.Image;
			
			var powerTop = new Animation("powerbar");
			powerTop.PlayRepeating("powerbar-top");
			powerLevelTopSprite = powerTop.Image;

			var powerBottom = new Animation("powerbar");
			powerBottom.PlayRepeating("powerbar-bottom");
			powerLevelBottomSprite = powerBottom.Image;
			
			repairButton = new Animation("repair");
			repairButton.PlayRepeating("normal");

			sellButton = new Animation("sell");
			sellButton.PlayRepeating("normal");

			pwrdownButton = new Animation("repair");
			pwrdownButton.PlayRepeating("normal");
			
			optionsButton = new Animation("tabs");
			optionsButton.PlayRepeating("left-normal");
			
			blank = SheetBuilder.Add(new Size(64, 48), 16);

			sprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u,
					u => SpriteSheetBuilder.LoadAllSprites(Rules.UnitInfo[u].Icon ?? (u + "icon"))[0]);

			tabSprites = groups.Select(
				(g, i) => Pair.New(g,
					OpenRa.Game.Graphics.Util.MakeArray(3,
						n => new Sprite(specialBin,
							new Rectangle(512 - (n + 1) * 27, 64 + i * 40, 27, 40),
							TextureChannel.Alpha))))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			digitSprites = Graphics.Util.MakeArray(10, a => a)
				.Select(n => new Sprite(specialBin, new Rectangle(32 + 13 * n, 0, 13, 17), TextureChannel.Alpha)).ToList();

			shimSprites = new[] 
			{
				new Sprite( specialBin, new Rectangle( 0, 192, 9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 202, 9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 216, 9, 48 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 11, 192, 64, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 11, 202, 64, 10 ), TextureChannel.Alpha ),
			};

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
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
				Game.LocalPlayer.PowerDrained,
				Game.LocalPlayer.PowerProvided,
				Game.LocalPlayer.IsReady ? "Yes" : "No"
				), new int2(140, 15), Color.White);

			PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			DrawMinimap();

			chromeRenderer.DrawSprite(specialBinSprite, float2.Zero, PaletteType.Chrome);
			chromeRenderer.DrawSprite(moneyBinSprite, new float2(Game.viewport.Width - 320, 0), PaletteType.Chrome);

			DrawMoney();
			DrawPower();
			chromeRenderer.Flush();
			DrawButtons();
			
			int paletteHeight = DrawBuildPalette(currentTab);
			DrawBuildTabs(paletteHeight);
			DrawChat();
		}

		void DrawMinimap()
		{
			var hasRadar = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer 
				&& a.traits.Contains<ProvidesRadar>() 
				&& a.traits.Get<ProvidesRadar>().IsActive());
			
			if (hasRadar != hadRadar)
				Sound.Play((hasRadar) ? "radaron2.aud" : "radardn1.aud");
			hadRadar = hasRadar;
			
			if (hasRadar)
				Game.minimap.Draw(new float2(Game.viewport.Width - 256, 8));
		}
		
		void AddButton(Rectangle r, Action<bool> b) { buttons.Add(Pair.New(r, b)); }
		
		void DrawBuildTabs(int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;

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

				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				
				
				// Don't let tabs overlap the bevel
				if (y > paletteOrigin.Y + paletteHeight - tabHeight - 9 && y < paletteOrigin.Y + paletteHeight)
				{
					y += tabHeight;	
				}
				
				// Stick tabs to the edge of the screen
				if (y > paletteOrigin.Y + paletteHeight)
				{
					x = Game.viewport.Width - tabWidth;
				}

				chromeRenderer.DrawSprite(q.Value[index], new float2(x, y), PaletteType.Chrome);

				buttons.Add(Pair.New(new Rectangle(x, y, tabWidth, tabHeight), 
					(Action<bool>)(isLmb => currentTab = groupName)));
				y += tabHeight;
			}

			chromeRenderer.Flush();
		}
		
		void CheckDeadTab( string groupName )
		{
			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			foreach( var item in queue.AllItems( groupName ) )
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
				chromeRenderer.DrawSprite(digitSprites[d - '0'], new float2(x, 6), PaletteType.Chrome);
				x -= 14;
			}
		}

		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		
		void DrawPower()
		{
			//draw background
			float2 powerOrigin = Game.viewport.Location+new float2(Game.viewport.Width - 20, 240 - 9);

			buildPaletteRenderer.DrawSprite(powerLevelTopSprite, powerOrigin, PaletteType.Chrome);
			buildPaletteRenderer.DrawSprite(powerLevelBottomSprite, powerOrigin + new float2(0, powerLevelTopSprite.size.Y), PaletteType.Chrome);
			buildPaletteRenderer.Flush();
			float2 top = powerOrigin + new float2(0, 15);
			float2 bottom = powerOrigin + new float2(0, powerLevelTopSprite.size.Y + powerLevelBottomSprite.size.Y) - new float2(0, 50);
			
			var scale = 100;
			while(Math.Max(Game.LocalPlayer.PowerProvided, Game.LocalPlayer.PowerDrained) >= scale) scale *= 2;
			//draw bar

			var powerTopY = bottom.Y + (top.Y - bottom.Y) * (Game.LocalPlayer.PowerProvided / (float)scale) - Game.viewport.Location.Y;
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerTopY), powerTopY, .3f);
			float2 powerTop = new float2(bottom.X, lastPowerProvidedPos.Value + Game.viewport.Location.Y);
			
			var color = Color.LimeGreen;
			if (Game.LocalPlayer.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (Game.LocalPlayer.GetPowerState() == PowerState.Critical)
				color = Color.Red;

			var color2 = Graphics.Util.Lerp(0.25f, color, Color.Black);
			
			for(int i = 11; i < 13; i++)
				lineRenderer.DrawLine(bottom + new float2(i, 0), powerTop + new float2(i, 0), color, color);
			for (int i = 13; i < 15; i++)
				lineRenderer.DrawLine(bottom + new float2(i, 0), powerTop + new float2(i, 0), color2, color2);
			
			lineRenderer.Flush();

			var drainedPositionY = bottom.Y + (top.Y - bottom.Y)*(Game.LocalPlayer.PowerDrained/(float) scale) - powerIndicatorSprite.size.Y /2 - Game.viewport.Location.Y;
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(drainedPositionY), drainedPositionY, .3f);
			//draw indicator
			float2 drainedPosition = new float2(bottom.X + 2, lastPowerDrainedPos.Value + Game.viewport.Location.Y);

			buildPaletteRenderer.DrawSprite(powerIndicatorSprite, drainedPosition, PaletteType.Chrome);
			buildPaletteRenderer.Flush();
		}

		void DrawButtons()
		{
			// Chronoshift
			Rectangle chronoshiftRect = new Rectangle(6, 14, repairButton.Image.bounds.Width, repairButton.Image.bounds.Height);
			var chronoshiftDrawPos = Game.viewport.Location + new float2(chronoshiftRect.Location);

			var hasChronosphere = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<Chronosphere>());

			if (!hasChronosphere)
				repairButton.ReplaceAnim("disabled");
			else
			{
				//repairButton.ReplaceAnim(Game.controller.orderGenerator is RepairOrderGenerator ? "pressed" : "normal");
				AddButton(chronoshiftRect, isLmb => HandleChronosphereButton());
			}
			buildPaletteRenderer.DrawSprite(repairButton.Image, chronoshiftDrawPos, PaletteType.Chrome);

			// Iron Curtain
			Rectangle curtainRect = new Rectangle(6, 14+50, repairButton.Image.bounds.Width, repairButton.Image.bounds.Height);
			var curtainDrawPos = Game.viewport.Location + new float2(curtainRect.Location);

			var hasCurtain = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<IronCurtain>());

			if (!hasCurtain)
				repairButton.ReplaceAnim("disabled");
			else
			{
				//repairButton.ReplaceAnim(Game.controller.orderGenerator is RepairOrderGenerator ? "pressed" : "normal");
				AddButton(curtainRect, isLmb => Game.controller.ToggleInputMode<IronCurtainOrderGenerator>());
			}
			buildPaletteRenderer.DrawSprite(repairButton.Image, curtainDrawPos, PaletteType.Chrome);
			
			
			// Repair
			Rectangle repairRect = new Rectangle(Game.viewport.Width - 120, 5, repairButton.Image.bounds.Width, repairButton.Image.bounds.Height);
			var repairDrawPos = Game.viewport.Location + new float2(repairRect.Location);

			var hasFact = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<ConstructionYard>());

			if (Game.Settings.RepairRequiresConyard && !hasFact)
				repairButton.ReplaceAnim("disabled");
			else
			{
				repairButton.ReplaceAnim(Game.controller.orderGenerator is RepairOrderGenerator ? "pressed" : "normal");
				AddButton(repairRect, isLmb => Game.controller.ToggleInputMode<RepairOrderGenerator>());
			}
			buildPaletteRenderer.DrawSprite(repairButton.Image, repairDrawPos, PaletteType.Chrome);
			
			// Sell
			Rectangle sellRect = new Rectangle(Game.viewport.Width - 80, 5, 
				sellButton.Image.bounds.Width, sellButton.Image.bounds.Height);

			var sellDrawPos = Game.viewport.Location + new float2(sellRect.Location);

			sellButton.ReplaceAnim(Game.controller.orderGenerator is SellOrderGenerator ? "pressed" : "normal");
			
			AddButton(sellRect, isLmb => Game.controller.ToggleInputMode<SellOrderGenerator>());
			buildPaletteRenderer.DrawSprite(sellButton.Image, sellDrawPos, PaletteType.Chrome);
			buildPaletteRenderer.Flush();

			if (Game.Settings.PowerDownBuildings)
			{
				// Power Down
				Rectangle pwrdownRect = new Rectangle(Game.viewport.Width - 40, 5,
					pwrdownButton.Image.bounds.Width, pwrdownButton.Image.bounds.Height);

				var pwrdownDrawPos = Game.viewport.Location + new float2(pwrdownRect.Location);

				pwrdownButton.ReplaceAnim(Game.controller.orderGenerator is PowerDownOrderGenerator ? "pressed" : "normal");

				AddButton(pwrdownRect, isLmb => Game.controller.ToggleInputMode<PowerDownOrderGenerator>());
				buildPaletteRenderer.DrawSprite(pwrdownButton.Image, pwrdownDrawPos, PaletteType.Chrome);
			}
			buildPaletteRenderer.Flush();
			
			//Options
			Rectangle optionsRect = new Rectangle(0 + 40,0, optionsButton.Image.bounds.Width, 
				optionsButton.Image.bounds.Height);
			
			var optionsDrawPos = Game.viewport.Location + new float2(optionsRect.Location);
			
			optionsButton.ReplaceAnim(optionsPressed ? "left-pressed" : "left-normal");
			
			AddButton(optionsRect, isLmb => DrawOptionsMenu());
			buildPaletteRenderer.DrawSprite(optionsButton.Image, optionsDrawPos, PaletteType.Chrome);
			buildPaletteRenderer.Flush();
			
			renderer.DrawText("Exit", new int2(80, -2) , Color.White);
			
		}
		
		void DrawOptionsMenu()
		{
			Environment.Exit(0);
		}
		
		void HandleChronosphereButton()
		{
			if (Game.controller.ToggleInputMode<ChronosphereSelectOrderGenerator>())
				Sound.Play("slcttgt1.aud");
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
				var producing = queue.CurrentItem( group );
				if (producing == null) return 0;
				return (producing.TotalTime - producing.RemainingTime) * NumClockFrames / producing.TotalTime;
			};
		}
		
		// Return an int telling us the y coordinate at the bottom of the palette
		int DrawBuildPalette(string queueName)
		{
			// Hack
			int columns = paletteColumns;
			int2 origin = new int2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
			
			if (queueName == null) return 0;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();

			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where(a => Rules.UnitInfo[a].TechLevel != -1)
				.OrderBy(a => Rules.UnitInfo[a].TechLevel);

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();

			string tooltipItem = null;

			foreach (var item in allItems)
			{
				var rect = new Rectangle(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;

				buildPaletteRenderer.DrawSprite(sprites[item], drawPos, PaletteType.Chrome);

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item);

				if (rect.Contains(lastMousePos.ToPoint()))
					tooltipItem = item;

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex( "idle", 
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime) 
							* NumClockFrames / firstOfThis.TotalTime);
					clock.Tick();

					buildPaletteRenderer.DrawSprite(clock.Image, drawPos, PaletteType.Chrome);

					if (firstOfThis.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (firstOfThis.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}

					var repeats = queue.AllItems(queueName).Count(a => a.Item == item);
					if (repeats > 1 || queue.CurrentItem(queueName) != firstOfThis)
					{
						var offset = -22;
						var digits = repeats.ToString();
						foreach (var d in digits)
						{
							ready.PlayFetchIndex("groups", () => d - '0');
							ready.Tick();
							overlayBits.Add(Pair.New(ready.Image, overlayPos + new float2(offset, 0)));
							offset += 6;
						}
					}
				}
				else
					if (!buildableItems.Contains(item) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureItem = item;
				AddButton(rect, isLmb => HandleBuildPalette(closureItem, isLmb));
				if (++x == columns) { x = 0; y++; }
			}

			while (x != 0 || y < MinRows)
			{
				var rect = new Rectangle(origin.X +  x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				buildPaletteRenderer.DrawSprite(blank, drawPos, PaletteType.Chrome);
				AddButton(rect, _ => { });
				if (++x == columns) { x = 0; y++; }
			}

			foreach (var ob in overlayBits)
				buildPaletteRenderer.DrawSprite(ob.First, ob.Second, PaletteType.Chrome);

			buildPaletteRenderer.Flush();

			for (var j = 0; j < y; j++)
				chromeRenderer.DrawSprite(shimSprites[2], new float2(origin.X - 9, origin.Y + 48 * j), PaletteType.Chrome);

			chromeRenderer.DrawSprite(shimSprites[0], new float2(origin.X - 9, origin.Y - 9), PaletteType.Chrome);
			chromeRenderer.DrawSprite(shimSprites[1], new float2(origin.X - 9, origin.Y - 1 + 48 * y), PaletteType.Chrome);

			for (var i = 0; i < columns; i++)
			{
				chromeRenderer.DrawSprite(shimSprites[3], new float2(origin.X + 64 * i, origin.Y - 9), PaletteType.Chrome);
				chromeRenderer.DrawSprite(shimSprites[4], new float2(origin.X + 64 * i, origin.Y - 1 + 48 * y), PaletteType.Chrome);
			}
			chromeRenderer.Flush();

			if (tooltipItem != null)
				DrawProductionTooltip(tooltipItem, new int2(Game.viewport.Width, origin.Y + y * 48 + 9)/*tooltipPos*/);
				
			return y*48+9;
		}

		void StartProduction( string item )
		{
			var group = Rules.UnitCategory[item];
			Sound.Play((group == "Building" || group == "Defense") ? "abldgin1.aud" : "train1.aud");
			Game.controller.AddOrder(Order.StartProduction(Game.LocalPlayer, item));
		}

		void HandleBuildPalette(string item, bool isLmb)
		{
			var player = Game.LocalPlayer;
			var group = Rules.UnitCategory[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var producing = queue.AllItems(group).FirstOrDefault( a => a.Item == item );

			Sound.Play("ramenu1.aud");

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(group))
				{
					if (producing.Done)
					{
						if (group == "Building" || group == "Defense")
							Game.controller.orderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
						return;
					}

					if (producing.Paused)
					{
						Game.controller.AddOrder(Order.PauseProduction(player, item, false));
						return;
					}
				}

				StartProduction(item);
			}
			else
			{
				if (producing != null)
				{
					// instant cancel of things we havent really started yet, and things that are finished
					if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
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
		}

		int2 lastMousePos;
		public bool HandleInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				lastMousePos = mi.Location;

			var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();

			if (action == null)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				action(mi.Button == MouseButton.Left);

			return true;
		}

		public bool HitTest(int2 mousePos)
		{
			return buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			renderer.DrawText2(text, pos - new int2(renderer.MeasureText2(text).X, 0), c);
		}

		void DrawProductionTooltip(string unit, int2 pos)
		{
			var p = pos.ToFloat2() - new float2(tooltipSprite.size.X, 0);
			chromeRenderer.DrawSprite(tooltipSprite, p, PaletteType.Chrome);
			chromeRenderer.Flush();

			var info = Rules.UnitInfo[unit];

			renderer.DrawText2(info.Description, p.ToInt2() + new int2(5,5), Color.White);

			DrawRightAligned( "${0}".F(info.Cost), pos + new int2(-5,5), 
				Game.LocalPlayer.Cash + Game.LocalPlayer.Ore >= info.Cost ? Color.White : Color.Red);

			var bi = info as BuildingInfo;
			if (bi != null)
				DrawRightAligned("ϟ{0}".F(bi.Power), pos + new int2(-5, 20),
					Game.LocalPlayer.PowerProvided - Game.LocalPlayer.PowerDrained + bi.Power >= 0
					? Color.White : Color.Red);

			var buildings = Rules.TechTree.GatherBuildings( Game.LocalPlayer );
			p += new int2(5, 5);
			p += new int2(0, 15);
			if (!Rules.TechTree.CanBuild(info, Game.LocalPlayer, buildings))
			{
				var prereqs = info.Prerequisite.Select(a => Rules.UnitInfo[a.ToLowerInvariant()].Description);
				renderer.DrawText("Requires {0}".F( string.Join( ", ", prereqs.ToArray() ) ), p.ToInt2(),
					Color.White);
			}

			if (info.LongDesc != null)
			{
				p += new int2(0, 15);
				renderer.DrawText(info.LongDesc.Replace( "\\n", "\n" ), p.ToInt2(), Color.White);
			}
		}
	}
}
