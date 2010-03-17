#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	class Chrome : IHandleInput
	{
		public readonly Renderer renderer;
		public readonly SpriteRenderer rgbaRenderer;
		readonly LineRenderer lineRenderer;
		readonly SpriteRenderer shpRenderer;
		
		string chromeCollection;
		string radarCollection;
		string paletteCollection;
		string digitCollection;
		
		// Special power bin
		readonly Dictionary<string, Sprite> spsprites;
		
		// Options menu (to be refactored)
		bool optionsPressed = false;

		// Buttons
		readonly Animation optionsButton;

		// Build Palette tabs
		string currentTab = "Building";
		bool paletteOpen = false;
		readonly Dictionary<string, string[]> tabImageNames;
		readonly Dictionary<string, Sprite> tabSprites;
		
		// Build Palette
		const int paletteColumns = 3;
		const int paletteRows = 5;
		static float2 paletteOpenOrigin = new float2(Game.viewport.Width - 215, 280);
		static float2 paletteClosedOrigin = new float2(Game.viewport.Width - 16, 280);
		static float2 paletteOrigin = paletteClosedOrigin;
		const int paletteAnimationLength = 7;
		int paletteAnimationFrame = 0;
		bool paletteAnimating = false;
		readonly List<Pair<RectangleF, Action<bool>>> buttons = new List<Pair<RectangleF, Action<bool>>>();
		readonly Animation cantBuild;
		readonly Animation ready;
		readonly Animation clock;

		// Radar
		static float2 radarOpenOrigin = new float2(Game.viewport.Width - 215, 29);
		static float2 radarClosedOrigin = new float2(Game.viewport.Width - 215, -166);
		static float2 radarOrigin = radarClosedOrigin;
		float radarMinimapHeight;
		const int radarSlideAnimationLength = 15;
		const int radarActivateAnimationLength = 5;
		int radarAnimationFrame = 0;
		bool radarAnimating = false;
		bool hasRadar = false;
				
		// Power bar 
		static float2 powerOrigin = new float2(42, 205); // Relative to radarOrigin
		static Size powerSize = new Size(138,5);
		
		// mapchooser
		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		int mapOffset = 0;
						
		public Chrome(Renderer r, Manifest m)
		{
			this.renderer = r;
			rgbaRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			lineRenderer = new LineRenderer(renderer);
			shpRenderer = new SpriteRenderer(renderer, true, renderer.WorldSpriteShader);
			
			optionsButton = new Animation("tabs");
			optionsButton.PlayRepeating("left-normal");

			tabSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>())
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<BuildableInfo>().Icon ?? (u.Name + "icon"))[0]);

			spsprites = Rules.Info.Values.SelectMany( u => u.Traits.WithInterface<SupportPowerInfo>() )
				.ToDictionary(
					u => u.Image,
					u => SpriteSheetBuilder.LoadAllSprites(u.Image)[0]);

			var groups = Rules.Info.Values.Select( x => x.Category ).Distinct().Where( g => g != null ).ToList();
			
			tabImageNames = groups.Select(
				(g, i) => Pair.New(g,
					OpenRA.Graphics.Util.MakeArray(3,
						n => i.ToString())))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
			
			var widgetYaml = m.ChromeLayout.Select(a => MiniYaml.FromFile(a)).Aggregate(MiniYaml.Merge);

			rootWidget = WidgetLoader.LoadWidget( widgetYaml.FirstOrDefault() );
			rootWidget.Initialize();
		}

		public Widget rootWidget;
		List<string> visibleTabs = new List<string>();
		
		public void Tick(World world)
		{
			TickPaletteAnimation();
			TickRadarAnimation();

			visibleTabs.Clear();
			foreach (var q in tabImageNames)
				if (!Rules.TechTree.BuildableItems(world.LocalPlayer, q.Key).Any())
				{
					CheckDeadTab(world, q.Key);
					if (currentTab == q.Key)
						currentTab = null;
				}
				else
					visibleTabs.Add(q.Key);

			if (currentTab == null)
				currentTab = visibleTabs.FirstOrDefault();
		}
				
		public void Draw( World world )
		{
			DrawDownloadBar();

			chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;
			radarCollection = "radar-" + world.LocalPlayer.Country.Race;
			paletteCollection = "palette-" + world.LocalPlayer.Country.Race;
			digitCollection = "digits-" + world.LocalPlayer.Country.Race;

			buttons.Clear();

			renderer.Device.DisableScissor();
			renderer.RegularFont.DrawText( rgbaRenderer, "RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\n".F(
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue), 
				new int2(140, 15), Color.White);

			if (Game.Settings.PerfGraph)
				PerfHistory.Render(renderer, world.WorldRenderer.lineRenderer);

			DrawRadar( world );
			DrawPower( world );
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, chromeCollection, "moneybin"), new float2(Game.viewport.Width - 320, 0), "chrome");
			DrawMoney( world );
			rgbaRenderer.Flush();
			DrawButtons( world );
			
			int paletteHeight = DrawBuildPalette(world, currentTab);
			DrawSupportPowers( world );
			DrawBuildTabs(world, paletteHeight);
			DrawChat();
			DrawOptionsMenu();
		}

		public void DrawDownloadBar()
		{
			if (PackageDownloader.IsIdle())
				return;

			var r = new Rectangle((Game.viewport.Width - 400) / 2, Game.viewport.Height - 110, 400, 100);
			DrawDialogBackground(r, "dialog");

			DrawCentered("Downloading: {0} (+{1} more)".F(
				PackageDownloader.CurrentPackage.Split(':')[0],
				PackageDownloader.RemainingPackages),
				new int2( Game.viewport.Width  /2, Game.viewport.Height - 90),
				Color.White);

			DrawDialogBackground(new Rectangle(r.Left + 30, r.Top + 50, r.Width - 60, 20),
				"dialog2");

			var x1 = r.Left + 35;
			var x2 = r.Right - 35;
			var x = float2.Lerp(x1, x2, PackageDownloader.Fraction);

			for (var y = r.Top + 55; y < r.Top + 65; y++)
				lineRenderer.DrawLine(
					new float2(x1, y) + Game.viewport.Location, 
					new float2(x, y) + Game.viewport.Location,
					Color.White, Color.White);

			lineRenderer.Flush();
		}

		public void DrawDialog(string text)
		{
			DrawDialog(text, null, _ => { }, null, _ => { });
		}
		
		public void DrawDialog(string text, string button1String, Action<bool> button1Action, string button2String, Action<bool> button2Action)
		{
			var w = Math.Max(renderer.BoldFont.Measure(text).X + 120, 400);
			var h = (button1String == null) ? 100 : 140;
			var r = new Rectangle((Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h);
			DrawDialogBackground(r, "dialog");
			DrawCentered(text, new int2(Game.viewport.Width / 2, Game.viewport.Height / 2 - ((button1String == null) ? 8 : 28)), Color.White);

			// don't allow clicks through the dialog
			AddButton(r, _ => { });
			
			if (button1String != null)
			{
				AddUiButton(new int2(r.Right - 300, r.Bottom - 40),button1String, button1Action);
			}
			
			if (button2String != null)
			{
				AddUiButton(new int2(r.Right - 100, r.Bottom - 40),button2String, button2Action);
			}
				
		}

		class MapInfo
		{
			public readonly string Filename;
			public readonly Map Map;

			public MapInfo(string filename)
			{
				Filename = filename.ToLowerInvariant();
				Map = new Map(Filename);
			}
		};

		Lazy<List<MapInfo>> mapList = Lazy.New(
			() =>
			{
				var builtinMaps = new IniFile(FileSystem.Open("missions.pkt")).GetSection("Missions").Select(a => a.Key);
				var mapsFolderMaps = Directory.GetFiles("maps/");
				return builtinMaps.Concat(mapsFolderMaps).Select(a => new MapInfo(a)).ToList();
			});

		bool showMapChooser = false;
		MapInfo currentMap;
		bool mapPreviewDirty = true;

		void AddUiButton(int2 pos, string text, Action<bool> a)
		{
			var rect = new Rectangle(pos.X - 160 / 2, pos.Y - 4, 160, 24);
			DrawDialogBackground( rect, "dialog2");
			DrawCentered(text, new int2(pos.X, pos.Y), Color.White);
			AddButton(rect, a);
		}

		public void DrawMapChooser()
		{
			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );
			DrawDialogBackground(r, "dialog");
			DrawCentered("Choose Map", new int2(r.Left + w / 2, r.Top + 20), Color.White);

			DrawDialogBackground(new Rectangle(r.Right - 200 - 160 / 2,
					r.Bottom - 50 + 6, 160, 24), "dialog2");

			AddUiButton(new int2(r.Left + 200, r.Bottom - 40), "OK",
				_ =>
				{
					Game.IssueOrder(Order.Chat("/map " + currentMap.Filename));
					showMapChooser = false;
				});

			AddUiButton(new int2(r.Right - 200, r.Bottom - 40), "Cancel",
				_ =>
				{
					showMapChooser = false;
				});

			if (mapPreviewDirty)
			{
				if (mapChooserSheet == null || mapChooserSheet.Size.Width != currentMap.Map.MapSize)
					mapChooserSheet = new Sheet(renderer, new Size(currentMap.Map.MapSize, currentMap.Map.MapSize));
				
				var b = Minimap.RenderTerrainBitmapWithSpawnPoints(currentMap.Map, Game.world.TileSet);	// tileset -> hack
				mapChooserSheet.Texture.SetData(b);
				mapChooserSprite = new Sprite(mapChooserSheet, 
					Minimap.MakeMinimapBounds(currentMap.Map), TextureChannel.Alpha);
				mapPreviewDirty = false;
			}

			var mapRect = new Rectangle(r.Right - 280, r.Top + 30, 256, 256);
			DrawDialogBackground(mapRect, "dialog2");
			rgbaRenderer.DrawSprite(mapChooserSprite, 
				new float2(mapRect.Location) + new float2(4, 4), 
				"chrome", 
				new float2(mapRect.Size) - new float2(8, 8));
			rgbaRenderer.Flush();

			var y = r.Top + 50;
			
			int maxListItems = ((r.Bottom - 60 - y ) / 20);
			
			for(int i = mapOffset; i < Math.Min(maxListItems + mapOffset, mapList.Value.Count()); i++, y += 20){

				var map = mapList.Value.ElementAt(i);
				var itemRect = new Rectangle(r.Left + 50, y - 2, r.Width - 340, 20);
				if (map == currentMap)
					DrawDialogBackground(itemRect, "dialog2");

				renderer.RegularFont.DrawText(rgbaRenderer, map.Map.Title, new int2(r.Left + 60, y), Color.White);
				var closureMap = map;
				AddButton(itemRect, _ => { currentMap = closureMap; mapPreviewDirty = true; });
			}

			y = mapRect.Bottom + 20;
			DrawCentered("Title: {0}".F(currentMap.Map.Title, currentMap.Map.Height),
				new int2(mapRect.Left + mapRect.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Size: {0}x{1}".F(currentMap.Map.Width, currentMap.Map.Height),
				new int2(mapRect.Left + mapRect.Width / 2, y), Color.White);
			y += 20;
			
			var theaterInfo = Game.world.WorldActor.Info.Traits.WithInterface<TheaterInfo>().FirstOrDefault(t => t.Theater == currentMap.Map.Theater);
			DrawCentered("Theater: {0}".F(theaterInfo.Name, currentMap.Map.Height),
				new int2(mapRect.Left + mapRect.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Spawnpoints: {0}".F(currentMap.Map.SpawnPoints.Count()),
				new int2(mapRect.Left + mapRect.Width / 2, y), Color.White);

			y += 30;
			AddUiButton(new int2(mapRect.Left + mapRect.Width / 2, y), "^",
			_ =>
			{
				mapOffset = (mapOffset - 1 < 0) ? 0 : mapOffset - 1;
			});
			
			y += 30;
			AddUiButton(new int2(mapRect.Left + mapRect.Width / 2, y), "\\/",
			_ =>
			{
				mapOffset = (mapOffset + 1 > mapList.Value.Count() - maxListItems) ? mapOffset : mapOffset + 1;
			});

			AddButton(r, _ => { });
		}
		bool PaletteAvailable(int index) { return Game.LobbyInfo.Clients.All(c => c.PaletteIndex != index); }
		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		
		void CyclePalette(bool left)
		{
			var d = left ? +1 : Player.PlayerColors.Count() - 1;

			var newIndex = ((int)Game.world.LocalPlayer.PaletteIndex + d) % Player.PlayerColors.Count();
				
			while (!PaletteAvailable(newIndex) && newIndex != (int)Game.world.LocalPlayer.PaletteIndex)
				newIndex = (newIndex + d) % Player.PlayerColors.Count();
			
			Game.IssueOrder(
				Order.Chat("/pal " + newIndex));
		}

		void CycleRace(bool left)
		{
			var countries = Game.world.GetCountries();
			var nextCountry = countries.Concat(countries)
				.SkipWhile(c => c != Game.world.LocalPlayer.Country)
				.Skip(1)
				.First();

			Game.IssueOrder(Order.Chat("/race " + nextCountry.Name));
		}

		void CycleReady(bool left)
		{
			Game.IssueOrder(
				new Order("ToggleReady", Game.world.LocalPlayer.PlayerActor, "") { IsImmediate = true });
		}

		void CycleSpawnPoint(bool left)
		{
			var d = left ? +1 : Game.world.Map.SpawnPoints.Count();

			var newIndex = (Game.world.LocalPlayer.SpawnPointIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			while (!SpawnPointAvailable(newIndex) && newIndex != (int)Game.world.LocalPlayer.SpawnPointIndex)
				newIndex = (newIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			Game.IssueOrder(
				Order.Chat("/spawn " + newIndex));
			
		}

		public void DrawWidgets(World world) { rootWidget.Draw(); }
		
		public void DrawLobby( World world )
		{
			buttons.Clear();
			DrawDownloadBar();

			if (showMapChooser)
			{
				DrawMapChooser();
				return;
			}

			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );
			DrawDialogBackground(r, "dialog");
			DrawCentered("OpenRA Multiplayer Lobby", new int2(r.Left + w / 2, r.Top + 20), Color.White);

			DrawDialogBackground(new Rectangle(r.Right - 264, r.Top + 43, 244, 244),"dialog2");
			var minimapRect = new Rectangle(r.Right - 262, r.Top + 45, 240, 240);

			world.Minimap.Update();
			world.Minimap.Draw(minimapRect, true);
			world.Minimap.DrawSpawnPoints(minimapRect);

			if (Game.IsHost)
			{
				AddUiButton(new int2(r.Right - 100, r.Top + 300), "Change Map",
				_ =>
				{
					showMapChooser = true;
					currentMap = mapList.Value.Single(
						m => m.Filename == Game.LobbyInfo.GlobalSettings.Map.ToLowerInvariant());
					mapPreviewDirty = true;
				});
			}

			var f = renderer.BoldFont;
			f.DrawText(rgbaRenderer, "Name", new int2(r.Left + 40, r.Top + 50), Color.White);
			f.DrawText(rgbaRenderer, "Color", new int2(r.Left + 140, r.Top + 50), Color.White);
			f.DrawText(rgbaRenderer, "Faction", new int2(r.Left + 220, r.Top + 50), Color.White);
			f.DrawText(rgbaRenderer, "Status", new int2(r.Left + 290, r.Top + 50), Color.White);
			f.DrawText(rgbaRenderer, "Spawn", new int2(r.Left + 390, r.Top + 50), Color.White);
				
			var y = r.Top + 80;
			foreach (var client in Game.LobbyInfo.Clients)
			{
				var isLocalPlayer = client.Index == Game.orderManager.Connection.LocalClientId;
				var paletteRect = new Rectangle(r.Left + 130, y - 2, 65, 22);

				if (isLocalPlayer)
				{
					// todo: name editing
					var nameRect = new Rectangle(r.Left + 30, y - 2, 95, 22);
					DrawDialogBackground(nameRect, "dialog2");

					DrawDialogBackground(paletteRect, "dialog2");
					AddButton(paletteRect, CyclePalette);

					var raceRect = new Rectangle(r.Left + 210, y - 2, 65, 22);
					DrawDialogBackground(raceRect, "dialog2");
					AddButton(raceRect, CycleRace);

					var readyRect = new Rectangle(r.Left + 280, y - 2, 95, 22);
					DrawDialogBackground(readyRect, "dialog2");
					AddButton(readyRect, CycleReady);
					
					var spawnPointRect = new Rectangle(r.Left + 380, y - 2, 70, 22);
					DrawDialogBackground(spawnPointRect, "dialog2");
					AddButton(spawnPointRect, CycleSpawnPoint);
				}

				shpRenderer.Flush();

				f = renderer.RegularFont;
				f.DrawText(rgbaRenderer, client.Name, new int2(r.Left + 40, y), Color.White);
				lineRenderer.FillRect(RectangleF.FromLTRB(paletteRect.Left + Game.viewport.Location.X + 5,
															paletteRect.Top + Game.viewport.Location.Y + 5,
															paletteRect.Right + Game.viewport.Location.X - 5,
															paletteRect.Bottom+Game.viewport.Location.Y - 5),
													Player.PlayerColors[client.PaletteIndex].c);
				lineRenderer.Flush();
				f.DrawText(rgbaRenderer, client.Country, new int2(r.Left + 220, y), Color.White);
				f.DrawText(rgbaRenderer, client.State.ToString(), new int2(r.Left + 290, y), Color.White);
				f.DrawText(rgbaRenderer, (client.SpawnPoint == 0) ? "-" : client.SpawnPoint.ToString(), new int2(r.Left + 410, y), Color.White);
				y += 30;

				rgbaRenderer.Flush();
			}

			var typingBox = new Rectangle(r.Left + 20, r.Bottom - 47, r.Width - 40, 27);
			var chatBox = new Rectangle(r.Left + 20, r.Bottom - 269, r.Width - 40, 220);

			DrawDialogBackground(typingBox, "dialog2");
			DrawDialogBackground(chatBox, "dialog2");

			DrawChat(typingBox, chatBox);

			// block clicks `through` the dialog
			AddButton(r, _ => { });
		}

		public void TickRadarAnimation()
		{
			if (!radarAnimating)
				return;

			// Increment frame
			if (hasRadar)
				radarAnimationFrame++;
			else
				radarAnimationFrame--;

			// Calculate radar bin position
			if (radarAnimationFrame <= radarSlideAnimationLength)
				radarOrigin = float2.Lerp(radarClosedOrigin, radarOpenOrigin, radarAnimationFrame * 1.0f / radarSlideAnimationLength);

			var eva = Game.world.LocalPlayer.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
			
			// Play radar-on sound at the start of the activate anim (open)
			if (radarAnimationFrame == radarSlideAnimationLength && hasRadar)
				Sound.Play(eva.RadarUp);

			// Play radar-on sound at the start of the activate anim (close)
			if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength - 1 && !hasRadar)
				Sound.Play(eva.RadarDown);

			// Minimap height
			if (radarAnimationFrame >= radarSlideAnimationLength)
				radarMinimapHeight = float2.Lerp(0, 192, (radarAnimationFrame - radarSlideAnimationLength) * 1.0f / radarActivateAnimationLength);

			// Animation is complete
			if ((radarAnimationFrame == 0 && !hasRadar)
					|| (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength && hasRadar))
			{
				radarAnimating = false;
			}
		}
		
		void DrawRadar( World world )
		{
			var hasNewRadar = world.Queries.OwnedBy[world.LocalPlayer]
				.WithTrait<ProvidesRadar>()
				.Any(a => a.Trait.IsActive);
			
			if (hasNewRadar != hasRadar)
			{
				radarAnimating = true;
			}
			
			hasRadar = hasNewRadar;

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "left"), radarOrigin, "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "right"), radarOrigin + new float2(201, 0), "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bottom"), radarOrigin + new float2(0, 192), "chrome");	

			if (radarAnimating)
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bg"), radarOrigin + new float2(9, 0), "chrome");	
			
			rgbaRenderer.Flush();

			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				RectangleF mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y+(192-radarMinimapHeight)/2, 192, radarMinimapHeight);
				world.Minimap.Draw(mapRect, false);
			}
		}
		
		void AddButton(RectangleF r, Action<bool> b) { buttons.Add(Pair.New(r, b)); }
		
		void DrawBuildTabs( World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;

			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			foreach (var q in tabImageNames)
			{
				var groupName = q.Key;
				if (!visibleTabs.Contains(groupName))
					continue;

				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				var race = world.LocalPlayer.Country.Race;
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer,"tabs-"+tabKeys[index], race+"-"+q.Key), new float2(x, y), "chrome");

				buttons.Add(Pair.New(new RectangleF(x, y, tabWidth, tabHeight),
					(Action<bool>)(isLmb => HandleTabClick(groupName))));
				y += tabHeight;
			}

			rgbaRenderer.Flush();
		}
		
		void HandleTabClick(string button)
		{
			var eva = Game.world.LocalPlayer.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
			Sound.Play(eva.TabClick);
			var wasOpen = paletteOpen;
			paletteOpen = (currentTab == button && wasOpen) ? false : true;
			currentTab = button;
			if (wasOpen != paletteOpen)
				paletteAnimating = true;
		}
		
		void CheckDeadTab( World world, string groupName )
		{
			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			foreach( var item in queue.AllItems( groupName ) )
				Game.IssueOrder(Order.CancelProduction(world.LocalPlayer, item.Item));		
		}

		void DrawMoney( World world )
		{
			var moneyDigits = world.LocalPlayer.DisplayCash.ToString();
			var x = Game.viewport.Width - 65;
			foreach (var d in moneyDigits.Reverse())
			{
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, digitCollection, (d - '0').ToString()), new float2(x, 6), "chrome");
				x -= 14;
			}
		}

		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		
		void DrawPower( World world )
		{
			// Nothing to draw
			if (world.LocalPlayer.PowerProvided == 0 && world.LocalPlayer.PowerDrained == 0)
				return;
			
			// Draw bar horizontally
			var barStart = powerOrigin + radarOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(world.LocalPlayer.PowerProvided, world.LocalPlayer.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;
			
			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerProvided / powerScaleBy);
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, .3f);
			float2 powerLevel = new float2(lastPowerProvidedPos.Value, barStart.Y);

			var color = Color.LimeGreen;
			if (world.LocalPlayer.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (world.LocalPlayer.GetPowerState() == PowerState.Critical)
				color = Color.Red;
		
			var colorDark = Graphics.Util.Lerp(0.25f, color, Color.Black);
			for (int i = 0; i < powerSize.Height; i++)
			{
				color = (i-1 < powerSize.Height/2) ? color : colorDark;
				float2 leftOffset = new float2(0,i);
				float2 rightOffset = new float2(0,i);
				// Indent corners
				if ((i == 0 || i == powerSize.Height - 1) && powerLevel.X - barStart.X > 1)
				{
					leftOffset.X += 1;
					rightOffset.X -= 1;
				}
				lineRenderer.DrawLine(Game.viewport.Location + barStart + leftOffset, Game.viewport.Location + powerLevel + rightOffset, color, color);
			}
			lineRenderer.Flush();

			// Power usage indicator
			var indicator = ChromeProvider.GetImage(renderer, radarCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value-indicator.size.X/2, barStart.Y-1);
		
			rgbaRenderer.DrawSprite(indicator, powerDrainLevel, "chrome");
			rgbaRenderer.Flush();
		}

		const int chromeButtonGap = 2;

		void DrawButtons( World world )
		{
			var origin = new int2(Game.viewport.Width - 200, 2);
			
			foreach (var cb in world.WorldActor.traits.WithInterface<IChromeButton>())
			{
				var button = cb;
				var anim = new Animation(cb.Image);
				anim.Play(cb.Enabled ? cb.Pressed ? "pressed" : "normal" : "disabled");
				origin.X -= (int)anim.Image.size.X + chromeButtonGap;
				shpRenderer.DrawSprite(anim.Image, origin, "chrome");
				AddButton(new RectangleF(origin.X, origin.Y, anim.Image.size.X, anim.Image.size.Y),
					_ => { if (button.Enabled) button.OnClick(); });
			}
			
			//Options
			Rectangle optionsRect = new Rectangle(0,0, optionsButton.Image.bounds.Width, 
				optionsButton.Image.bounds.Height);
			
			var optionsDrawPos = new float2(optionsRect.Location);
			
			optionsButton.ReplaceAnim(optionsPressed ? "left-pressed" : "left-normal");
			
			AddButton(optionsRect, isLmb => optionsPressed = !optionsPressed);
			shpRenderer.DrawSprite(optionsButton.Image, optionsDrawPos, "chrome");
			shpRenderer.Flush();

			renderer.RegularFont.DrawText(rgbaRenderer, "Options", new int2((int)(optionsButton.Image.size.X - renderer.RegularFont.Measure("Options").X) / 2, -2), Color.White);
		}
		
		void DrawOptionsMenu()
		{
			if (optionsPressed){
				var width = 500;
				var height = 300;
				
				DrawDialogBackground(new Rectangle((Game.viewport.Width - width)/ 2, (Game.viewport.Height-height) / 2,
					width, height), "dialog");
			}
		}

		void DrawDialogBackground(Rectangle r, string collection)
		{
			renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);

			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = images.Select(x => ChromeProvider.GetImage(renderer, collection, x)).ToArray();
			
			for( var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				rgbaRenderer.DrawSprite(ss[2], new float2(r.Left, y), "chrome");
				rgbaRenderer.DrawSprite(ss[3], new float2(r.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				rgbaRenderer.DrawSprite(ss[0], new float2(x, r.Top), "chrome");
				rgbaRenderer.DrawSprite(ss[1], new float2(x, r.Bottom - ss[1].size.Y), "chrome");
			}

			rgbaRenderer.DrawSprite(ss[4], new float2(r.Left, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[5], new float2(r.Right - ss[5].size.X, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[6], new float2(r.Left, r.Bottom - ss[6].size.Y), "chrome");
			rgbaRenderer.DrawSprite(ss[7], new float2(r.Right - ss[7].size.X, r.Bottom - ss[7].size.Y), "chrome");
			rgbaRenderer.Flush();

			renderer.Device.DisableScissor();
		}

		void DrawChat()
		{
			var typingArea = new Rectangle(400, Game.viewport.Height - 30, Game.viewport.Width - 420, 30);
			var chatLogArea = new Rectangle(400, Game.viewport.Height - 500, Game.viewport.Width - 420, 500 - 40);

			DrawChat(typingArea, chatLogArea);
		}

		void DrawChat(Rectangle typingArea, Rectangle chatLogArea)
		{
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);

			renderer.Device.EnableScissor(typingArea.Left, typingArea.Top, typingArea.Width, typingArea.Height);
			if (Game.chat.isChatting)
				RenderChatLine(Tuple.New(Color.White, "Chat:", Game.chat.typing), 
					new int2(typingArea.X + 10, typingArea.Y + 6));

			rgbaRenderer.Flush();
			renderer.Device.DisableScissor();

			renderer.Device.EnableScissor(chatLogArea.Left, chatLogArea.Top, chatLogArea.Width, chatLogArea.Height);
			foreach (var line in Game.chat.recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				RenderChatLine(line, chatpos);
			}

			rgbaRenderer.Flush();
			renderer.Device.DisableScissor();
		}

		void RenderChatLine(Tuple<Color, string, string> line, int2 p)
		{
			var size = renderer.RegularFont.Measure(line.b);
			renderer.RegularFont.DrawText(rgbaRenderer, line.b, p, line.a);
			renderer.RegularFont.DrawText(rgbaRenderer, line.c, p + new int2(size.X + 10, 0), Color.White);
		}
		
		void TickPaletteAnimation()
		{		
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;

			Log.Write("{0}",paletteAnimationFrame);
			
			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin, paletteAnimationFrame * 1.0f / paletteAnimationLength);

			var eva = Game.world.LocalPlayer.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
			
			// Play palette-open sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play(eva.BuildPaletteOpen);

			// Play palette-close sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play(eva.BuildPaletteClose);

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
					|| (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}
		
		
		// Return an int telling us the y coordinate at the bottom of the palette
		int DrawBuildPalette( World world, string queueName )
		{
			// Hack
			int columns = paletteColumns;
			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
			
			if (queueName == null) return 0;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(world.LocalPlayer, queueName).ToArray();

			var allBuildables = Rules.TechTree.AllBuildables(world.LocalPlayer, queueName)
				.Where(a => a.Traits.Contains<BuildableInfo>())
				.Where(a => a.Traits.Get<BuildableInfo>().Owner.Contains(world.LocalPlayer.Country.Race))
				.OrderBy(a => a.Traits.Get<BuildableInfo>().TechLevel);

			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();

			string tooltipItem = null;

			// Draw the top border
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "top"), new float2(origin.X - 9, origin.Y - 9), "chrome");

			// Draw the icons
			int lasty = -1;
			foreach (var item in allBuildables)
			{
				// Draw the background for this row
				if (y != lasty)
				{
					rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "bg-" + (y % 4).ToString()), new float2(origin.X - 9, origin.Y + 48 * y), "chrome");
					rgbaRenderer.Flush();
					lasty = y;
				}
				
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;

				shpRenderer.DrawSprite(tabSprites[item.Name], drawPos, "chrome");

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item.Name);

				if (rect.Contains(lastMousePos.ToPoint()))
					tooltipItem = item.Name;

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex( "idle", 
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime) 
							* (clock.CurrentSequence.Length - 1)/ firstOfThis.TotalTime);
					clock.Tick();
					shpRenderer.DrawSprite(clock.Image, drawPos, "chrome");

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

					var repeats = queue.AllItems(queueName).Count(a => a.Item == item.Name);
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
					if (!buildableItems.Contains(item.Name) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureItemName = item.Name;
				
				var eva = world.LocalPlayer.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
				
				AddButton(rect, buildableItems.Contains(item.Name)
					? isLmb => HandleBuildPalette(world, closureItemName, isLmb)
					: (Action<bool>)(_ => Sound.Play(eva.TabClick)));
	
				if (++x == columns) { x = 0; y++; }
			}
			if (x != 0) y++;
			
			while (y < paletteRows)
			{
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "bg-" + (y % 4).ToString()), new float2(origin.X - 9, origin.Y + 48 * y), "chrome");
				y++;
			}

			foreach (var ob in overlayBits)
				shpRenderer.DrawSprite(ob.First, ob.Second, "chrome");

			shpRenderer.Flush();
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "bottom"), new float2(origin.X - 9, origin.Y - 1 + 48 * y), "chrome");

			// Draw dock
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-top"), new float2(Game.viewport.Width - 14, origin.Y - 23), "chrome");
			for (int i = 0; i < y; i++)
			{
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-" + (y % 4).ToString()), new float2(Game.viewport.Width - 14, origin.Y + 48 * i), "chrome");
			}
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-bottom"), new float2(Game.viewport.Width - 14, origin.Y - 1 + 48 * y), "chrome");
			rgbaRenderer.Flush();

			if (tooltipItem != null && paletteOpen)
				DrawProductionTooltip(world, tooltipItem, new float2(Game.viewport.Width, origin.Y + y * 48 + 9).ToInt2()/*tooltipPos*/);
				
			return y*48+9;
		}

		void StartProduction( World world, string item )
		{
			var eva = world.LocalPlayer.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
			var unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);
			Game.IssueOrder(Order.StartProduction(world.LocalPlayer, item));
		}

		void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var player = world.LocalPlayer;
			var unit = Rules.Info[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var eva = player.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
			var producing = queue.AllItems(unit.Category).FirstOrDefault( a => a.Item == item );

			Sound.Play(eva.TabClick);

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(unit.Category))
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							Game.controller.orderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
						return;
					}

					if (producing.Paused)
					{
						Game.IssueOrder(Order.PauseProduction(player, item, false));
						return;
					}
				}

				StartProduction(world, item);
			}
			else
			{
				if (producing != null)
				{
					// instant cancel of things we havent really started yet, and things that are finished
					if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
					{
						Sound.Play(eva.CancelledAudio);
						Game.IssueOrder(Order.CancelProduction(player, item));
					}
					else
					{
						Sound.Play(eva.OnHoldAudio);
						Game.IssueOrder(Order.PauseProduction(player, item, true));
					}
				}
			}
		}

		int2 lastMousePos;
		public bool HandleInput(World world, MouseInput mi)
		{
			if (rootWidget.HandleInput(mi))
				return true;
			
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
			return rootWidget.GetEventBounds().Contains(mousePos.X, mousePos.Y) 
				|| buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(rgbaRenderer, text, pos - new int2(renderer.BoldFont.Measure(text).X, 0), c);
		}

		void DrawCentered(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(rgbaRenderer, text, pos - new int2(renderer.BoldFont.Measure(text).X / 2, 0), c);
		}

		void DrawProductionTooltip(World world, string unit, int2 pos)
		{
			var tooltipSprite = ChromeProvider.GetImage(renderer, chromeCollection, "tooltip-bg");
			var p = pos.ToFloat2() - new float2(tooltipSprite.size.X, 0);
			rgbaRenderer.DrawSprite(tooltipSprite, p, "chrome");
			

			var info = Rules.Info[unit];
			var buildable = info.Traits.Get<BuildableInfo>();

			renderer.BoldFont.DrawText(rgbaRenderer, buildable.Description, p.ToInt2() + new int2(5, 5), Color.White);

			DrawRightAligned( "${0}".F(buildable.Cost), pos + new int2(-5,5), 
				world.LocalPlayer.Cash + world.LocalPlayer.Ore >= buildable.Cost ? Color.White : Color.Red);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("ϟ{0}".F(bi.Power), pos + new int2(-5, 20),
					world.LocalPlayer.PowerProvided - world.LocalPlayer.PowerDrained + bi.Power >= 0
					? Color.White : Color.Red);

			var buildings = Rules.TechTree.GatherBuildings( world.LocalPlayer );
			p += new int2(5, 5);
			p += new int2(0, 15);
			if (!Rules.TechTree.CanBuild(info, world.LocalPlayer, buildings))
			{
				var prereqs = buildable.Prerequisites
					.Select( a => Description( a ) );
				renderer.RegularFont.DrawText(rgbaRenderer, "Requires {0}".F(string.Join(", ", prereqs.ToArray())), p.ToInt2(),
					Color.White);
			}

			if (buildable.LongDesc != null)
			{
				p += new int2(0, 15);
				renderer.RegularFont.DrawText(rgbaRenderer, buildable.LongDesc.Replace( "\\n", "\n" ), p.ToInt2(), Color.White);
			}

			rgbaRenderer.Flush();
		}

		static string Description( string a )
		{
			if( a[ 0 ] == '@' )
				return "any " + a.Substring( 1 );
			else
				return Rules.Info[ a.ToLowerInvariant() ].Traits.Get<BuildableInfo>().Description;
		}

		void DrawSupportPowers( World world )
		{
			var powers = world.LocalPlayer.PlayerActor.traits.WithInterface<SupportPower>();
			var numPowers = powers.Count(p => p.IsAvailable);

			if (numPowers == 0) return;

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, chromeCollection, "specialbin-top"), new float2(0, 14), "chrome");
			for (var i = 1; i < numPowers; i++)
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, chromeCollection, "specialbin-middle"), new float2(0, 14 + i * 51), "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, chromeCollection, "specialbin-bottom"), new float2(0, 14 + numPowers * 51), "chrome");

			rgbaRenderer.Flush();

			var y = 24;

			SupportPower tooltipItem = null;
			int2 tooltipPos = int2.Zero;

			foreach (var sp in powers)
			{
				var image = spsprites[sp.Info.Image];
				if (sp.IsAvailable)
				{
					var drawPos = new float2(5, y);
					shpRenderer.DrawSprite(image, drawPos, "chrome");

					clock.PlayFetchIndex("idle",
						() => (sp.TotalTime - sp.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / sp.TotalTime);
					clock.Tick();

					shpRenderer.DrawSprite(clock.Image, drawPos, "chrome");

					var rect = new Rectangle(5, y, 64, 48);
					if (sp.IsReady)
					{
						ready.Play("ready");
						shpRenderer.DrawSprite(ready.Image, 
							drawPos + new float2((64 - ready.Image.size.X) / 2, 2), 
							"chrome");
					}

					AddButton(rect, HandleSupportPower(sp));

					if (rect.Contains(lastMousePos.ToPoint()))
					{
						tooltipItem = sp;
						tooltipPos = drawPos.ToInt2() + new int2(72, 0);
					}

					y += 51;
				}
			}

			shpRenderer.Flush();

			if (tooltipItem != null)
				DrawSupportPowerTooltip(world, tooltipItem, tooltipPos);
		}

		Action<bool> HandleSupportPower(SupportPower sp)
		{
			return b => { if (b) sp.Activate(); };
		}

		string FormatTime(int ticks)
		{
			var seconds = ticks / 25;
			var minutes = seconds / 60;

			return "{0:D2}:{1:D2}".F(minutes, seconds % 60);
		}

		void DrawSupportPowerTooltip(World world, SupportPower sp, int2 pos)
		{
			var tooltipSprite = ChromeProvider.GetImage(renderer, chromeCollection, "tooltip-bg");
			rgbaRenderer.DrawSprite(tooltipSprite, pos, "chrome");
			rgbaRenderer.Flush();

			pos += new int2(5, 5);

			renderer.BoldFont.DrawText(rgbaRenderer, sp.Info.Description, pos, Color.White);

			var timer = "Charge Time: {0}".F(FormatTime(sp.RemainingTime));
			DrawRightAligned(timer, pos + new int2((int)tooltipSprite.size.X - 10, 0), Color.White);

			if (sp.Info.LongDesc != null)
			{
				pos += new int2(0, 25);
				renderer.RegularFont.DrawText(rgbaRenderer, sp.Info.LongDesc.Replace("\\n", "\n"), pos, Color.White);
			}

			rgbaRenderer.Flush();
		}

		public void SetCurrentTab(string produces)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			currentTab = produces;
		}
	}
}
