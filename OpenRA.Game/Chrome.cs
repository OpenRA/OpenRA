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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	class Chrome : IHandleInput
	{
		public readonly Renderer renderer;
		public readonly LineRenderer lineRenderer;

		SpriteRenderer rgbaRenderer { get { return renderer.RgbaSpriteRenderer; } }
		SpriteRenderer shpRenderer { get { return renderer.WorldSpriteRenderer; } }
		
		string chromeCollection;
		string radarCollection;

		readonly List<Pair<RectangleF, Action<bool>>> buttons = new List<Pair<RectangleF, Action<bool>>>();

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

		internal MapStub currentMap;

		public Chrome(Renderer r, Manifest m)
		{
			this.renderer = r;
			lineRenderer = new LineRenderer(renderer);
					
			var widgetYaml = m.ChromeLayout.Select(a => MiniYaml.FromFile(a)).Aggregate(MiniYaml.Merge);
			
			if (rootWidget == null)
			{
				rootWidget = WidgetLoader.LoadWidget( widgetYaml.FirstOrDefault() );
				rootWidget.Initialize();
				rootWidget.InitDelegates();
				Widget.WindowList.Push("MAINMENU_BG");
			}
		}

		public static Widget rootWidget = null;
		public static Widget selectedWidget;
				
		public void Tick(World world)
		{
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;

			if (worldTooltipTicks < worldTooltipDelay)
				++worldTooltipTicks;
			
			rootWidget.Tick(world);
			
			TickRadarAnimation();
		}
				
		public void Draw( World world )
		{
			radarCollection = "radar-" + world.LocalPlayer.Country.Race;

			buttons.Clear();

			renderer.Device.DisableScissor();
			
			DrawRadar( world );
			DrawPower( world );
			rgbaRenderer.Flush();

			var typingArea = new Rectangle(240, Game.viewport.Height - 30, Game.viewport.Width - 420, 30);
			var chatLogArea = new Rectangle(240, Game.viewport.Height - 500, Game.viewport.Width - 420, 500 - 40);
			DrawChat(typingArea, chatLogArea);

			DrawWorldTooltip(world);
		}

		void DrawWorldTooltip(World world)
		{
			if (worldTooltipTicks < worldTooltipDelay || world == null || world.LocalPlayer == null)
				return;

			var actor = world.FindUnitsAtMouse(lastMousePos).FirstOrDefault();
			if (actor == null) return;

			var text = actor.Info.Traits.Contains<ValuedInfo>() ? actor.Info.Traits.Get<ValuedInfo>().Description : actor.Info.Name;
			var text2 = (actor.Owner == world.LocalPlayer)
				? "" : (actor.Owner == world.NeutralPlayer ? "{0}" : "{0} ({1})").F(actor.Owner.PlayerName, world.LocalPlayer.Stances[actor.Owner]);

			var sz = renderer.BoldFont.Measure(text);
			var sz2 = renderer.RegularFont.Measure(text2);

			sz.X = Math.Max(sz.X, sz2.X);

			if (text2 != "") sz.Y += sz2.Y + 2;

			sz.X += 20;
			sz.Y += 24;

			WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
				lastMousePos.X + 20, lastMousePos.Y + 20, lastMousePos.X + sz.X + 20, lastMousePos.Y + sz.Y + 20));

			renderer.BoldFont.DrawText(text, new float2(lastMousePos.X + 30, lastMousePos.Y + 30), Color.White);
			renderer.RegularFont.DrawText(text2, new float2(lastMousePos.X + 30, lastMousePos.Y + 50), Color.White);
		}
		
		void AddUiButton(int2 pos, string text, Action<bool> a)
		{
			var rect = new Rectangle(pos.X - 160 / 2, pos.Y - 4, 160, 24);
			DrawDialogBackground( rect, "dialog2");
			DrawCentered(text, new int2(pos.X, pos.Y), Color.White);
			rgbaRenderer.Flush();
			AddButton(rect, a);
		}

		public void DrawMapChooser()
		{
			buttons.Clear();

			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );

			AddUiButton(new int2(r.Left + 200, r.Bottom - 40), "OK",
				_ =>
				{
					Game.IssueOrder(Order.Chat("/map " + currentMap.Uid));
					Chrome.rootWidget.CloseWindow();
				});

			AddUiButton(new int2(r.Right - 200, r.Bottom - 40), "Cancel",
				_ =>
				{
					Chrome.rootWidget.CloseWindow();
				});

			var mapBackground = new Rectangle(r.Right - 284, r.Top + 26, 264, 264);
			var mapContainer = new Rectangle(r.Right - 280, r.Top + 30, 256, 256);
			var mapRect = currentMap.PreviewBounds(new Rectangle(mapContainer.X,mapContainer.Y,mapContainer.Width,mapContainer.Height));

			var y = r.Top + 50;
					
			// Don't bother showing a subset of the data
			// This will be fixed properly when we move the map list to widgets
			foreach (var kv in Game.AvailableMaps)
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;

				var itemRect = new Rectangle(r.Left + 50, y - 2, r.Width - 340, 20);
				if (map == currentMap)
				{
					rgbaRenderer.Flush();
					DrawDialogBackground(itemRect, "dialog2");
				}

				renderer.RegularFont.DrawText(map.Title, new int2(r.Left + 60, y), Color.White);
				rgbaRenderer.Flush();
				var closureMap = map;
				AddButton(itemRect, _ => { currentMap = closureMap; });
				y += 20;
			}

			y = mapContainer.Bottom + 20;
			DrawCentered("Title: {0}".F(currentMap.Title),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Size: {0}x{1}".F(currentMap.Width, currentMap.Height),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;

			var theaterInfo = Rules.Info["world"].Traits.WithInterface<TheaterInfo>().FirstOrDefault(t => t.Theater == currentMap.Tileset);
			DrawCentered("Theater: {0}".F(theaterInfo.Name),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Spawnpoints: {0}".F(currentMap.PlayerCount),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);

			AddButton(r, _ => { });
		}
		bool PaletteAvailable(int index) { return Game.LobbyInfo.Clients.All(c => c.PaletteIndex != index); }
		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		
		void CyclePalette(bool left)
		{
			var d = left ? +1 : Player.PlayerColors(Game.world).Count() - 1;

			var newIndex = ((int)Game.LocalClient.PaletteIndex + d) % Player.PlayerColors(Game.world).Count();
				
			while (!PaletteAvailable(newIndex) && newIndex != (int)Game.LocalClient.PaletteIndex)
				newIndex = (newIndex + d) % Player.PlayerColors(Game.world).Count();
			
			Game.IssueOrder(
				Order.Chat("/pal " + newIndex));
		}

		void CycleRace(bool left)
		{
			var countries = new[] { "Random" }.Concat(Game.world.GetCountries().Select(c => c.Name));
			var nextCountry = countries
				.SkipWhile(c => c != Game.LocalClient.Country)
				.Skip(1)
				.FirstOrDefault();

			if (nextCountry == null)
				nextCountry = countries.First();

			Game.IssueOrder(Order.Chat("/race " + nextCountry));
		}

		void CycleReady(bool left)
		{
			Game.IssueOrder(Order.Chat("/ready"));
		}

		void CycleSpawnPoint(bool left)
		{
			var d = left ? +1 : Game.world.Map.SpawnPoints.Count();

			var newIndex = (Game.LocalClient.SpawnPoint + d) % (Game.world.Map.SpawnPoints.Count()+1);

			while (!SpawnPointAvailable(newIndex) && newIndex != (int)Game.LocalClient.SpawnPoint)
				newIndex = (newIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			Game.IssueOrder(
				Order.Chat("/spawn " + newIndex));
			
		}

		public void DrawWidgets(World world) { rootWidget.Draw(world); shpRenderer.Flush(); rgbaRenderer.Flush(); }
		
		public void DrawLobby()
		{
			buttons.Clear();

			if( Game.LobbyInfo.GlobalSettings.Map == null )
				currentMap = null;
			else
				currentMap = Game.AvailableMaps[ Game.LobbyInfo.GlobalSettings.Map ];
			
			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );

			if (Game.IsHost)
			{
				AddUiButton(new int2(r.Right - 100, r.Top + 300), "Change Map",
				_ =>
				{
					currentMap = Game.AvailableMaps[ Game.LobbyInfo.GlobalSettings.Map ];
					rootWidget.OpenWindow( "MAP_CHOOSER" );
				});
			}
			
			
			var f = renderer.BoldFont;
			f.DrawText("Name", new int2(r.Left + 40, r.Top + 50), Color.White);
			f.DrawText("Color", new int2(r.Left + 140, r.Top + 50), Color.White);
			f.DrawText("Faction", new int2(r.Left + 220, r.Top + 50), Color.White);
			f.DrawText("Status", new int2(r.Left + 290, r.Top + 50), Color.White);
			f.DrawText("Spawn", new int2(r.Left + 390, r.Top + 50), Color.White);

			rgbaRenderer.Flush();
				
			var y = r.Top + 80;
			foreach (var client in Game.LobbyInfo.Clients)
			{
				var isLocalPlayer = client.Index == Game.orderManager.Connection.LocalClientId;
				var paletteRect = new Rectangle(r.Left + 130, y - 2, 65, 22);

				if (isLocalPlayer)
				{
					// todo: name editing
					var nameRect = new Rectangle(r.Left + 30, y - 2, 95, 22);
					DrawDialogBackground(nameRect, "dialog3");

					DrawDialogBackground(paletteRect, "dialog3");
					AddButton(paletteRect, CyclePalette);

					var raceRect = new Rectangle(r.Left + 210, y - 2, 65, 22);
					DrawDialogBackground(raceRect, "dialog3");
					AddButton(raceRect, CycleRace);

					var readyRect = new Rectangle(r.Left + 280, y - 2, 95, 22);
					DrawDialogBackground(readyRect, "dialog3");
					AddButton(readyRect, CycleReady);
					
					var spawnPointRect = new Rectangle(r.Left + 380, y - 2, 70, 22);
					DrawDialogBackground(spawnPointRect, "dialog3");
					AddButton(spawnPointRect, CycleSpawnPoint);
				}

				shpRenderer.Flush();

				f = renderer.RegularFont;
				f.DrawText(client.Name, new int2(r.Left + 40, y), Color.White);
				lineRenderer.FillRect(RectangleF.FromLTRB(paletteRect.Left + Game.viewport.Location.X + 5,
															paletteRect.Top + Game.viewport.Location.Y + 5,
															paletteRect.Right + Game.viewport.Location.X - 5,
															paletteRect.Bottom+Game.viewport.Location.Y - 5),
													Player.PlayerColors(Game.world)[client.PaletteIndex % Player.PlayerColors(Game.world).Count()].c);
				lineRenderer.Flush();
				f.DrawText(client.Country, new int2(r.Left + 220, y), Color.White);
				f.DrawText(client.State.ToString(), new int2(r.Left + 290, y), Color.White);
				f.DrawText((client.SpawnPoint == 0) ? "-" : client.SpawnPoint.ToString(), new int2(r.Left + 410, y), Color.White);
				y += 30;

				rgbaRenderer.Flush();
			}

			var typingBox = new Rectangle(r.Left + 20, r.Bottom - 47, r.Width - 40, 27);
			var chatBox = new Rectangle(r.Left + 20, r.Bottom - 269, r.Width - 40, 220);

			DrawDialogBackground(typingBox, "dialog2");
			DrawDialogBackground(chatBox, "dialog3");

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

			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			
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

		void DrawDialogBackground(Rectangle r, string collection)
		{
			WidgetUtils.DrawPanel(collection, r);
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
			renderer.RegularFont.DrawText(line.b, p, line.a);
			renderer.RegularFont.DrawText(line.c, p + new int2(size.X + 10, 0), Color.White);
		}

		const int worldTooltipDelay = 10;		/* ticks */

		public int worldTooltipTicks = 0;
		public int2 lastMousePos;
		public bool HandleInput(World world, MouseInput mi)
		{
			if (selectedWidget != null)
				return selectedWidget.HandleInput(mi);
				
			if (rootWidget.HandleInput(mi))
				return true;

			if (mi.Event == MouseInputEvent.Move)
			{
				lastMousePos = mi.Location;
				worldTooltipTicks = 0;
			}

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
			if (selectedWidget != null)
				return true;
			
			return rootWidget.HitTest(mousePos)
				|| buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}

		void DrawCentered(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(text, pos - new int2(renderer.BoldFont.Measure(text).X / 2, 0), c);
		}
	}
}
