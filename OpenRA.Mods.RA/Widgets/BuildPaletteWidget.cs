#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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

namespace OpenRA.Mods.RA.Widgets
{
	class BuildPaletteWidget : Widget
	{
		public int Columns = 3;
		public int Rows = 5;
		
		string currentTab = null;
		bool paletteOpen = false;
		Dictionary<string, string[]> tabImageNames;
		Dictionary<string, Sprite> tabSprites;
		static float2 paletteOpenOrigin = new float2(Game.viewport.Width - 215, 280);
		static float2 paletteClosedOrigin = new float2(Game.viewport.Width - 16, 280);
		static float2 paletteOrigin = paletteClosedOrigin;
		const int paletteAnimationLength = 7;
		int paletteAnimationFrame = 0;
		bool paletteAnimating = false;
		List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();
		List<Pair<Rectangle, Action<MouseInput>>> tabs = new List<Pair<Rectangle, Action<MouseInput>>>();
		Animation cantBuild;
		Animation ready;
		Animation clock;
		public readonly string BuildPaletteOpen = "bleep13.aud";
		public readonly string BuildPaletteClose = "bleep13.aud";
		public readonly string TabClick = "ramenu1.aud";
		List<string> visibleTabs = new List<string>();
		
		public BuildPaletteWidget() : base() { }
		
		public override void Initialize()
		{
			base.Initialize();
			
			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
			
			tabSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>())
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<BuildableInfo>().Icon ?? (u.Name + "icon"))[0]);

			var groups = Rules.Categories();
			
			tabImageNames = groups.Select(
				(g, i) => Pair.New(g,
					OpenRA.Graphics.Util.MakeArray(3,
						n => i.ToString())))
				.ToDictionary(a => a.First, a => a.Second);

			IsVisible = () => { return currentTab != null || (currentTab == null && !paletteOpen);  };
		}
		
		public override Rectangle EventBounds
		{
			get { return new Rectangle((int)(paletteOrigin.X) - 24, (int)(paletteOrigin.Y), 215, 48 * numActualRows);
			}
		}
		
		public override void Tick(World world)
		{
			visibleTabs.Clear();
			foreach (var q in tabImageNames)
				if (!Rules.TechTree.BuildableItems(world.LocalPlayer, q.Key).Any())
				{
					if (currentTab == q.Key)
						currentTab = null;
				}
				else
					visibleTabs.Add(q.Key);

			if (currentTab == null)
				currentTab = visibleTabs.FirstOrDefault();
			
			TickPaletteAnimation(world);
			
			base.Tick(world);
		}
		
		void TickPaletteAnimation(World world)
		{		
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;
			
			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin, paletteAnimationFrame * 1.0f / paletteAnimationLength);
			
			// Play palette-open sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play(BuildPaletteOpen);

			// Play palette-close sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play(BuildPaletteClose);

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
					|| (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}
		
		public void SetCurrentTab(string produces)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			currentTab = produces;
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;

			if (e.KeyChar == '\t')
			{
				TabChange(e.Modifiers.HasModifier(Modifiers.Shift));
				return true;
			}

			return DoBuildingHotkey(Char.ToLowerInvariant(e.KeyChar), Game.world);
		}
		
		public override bool HandleInputInner(MouseInput mi)
		{			
			if (mi.Event != MouseInputEvent.Down)
				return false;
			
			var action = tabs.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();
			if (action == null && paletteOpen)
				action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
					.Select(a => a.Second).FirstOrDefault();
			
			if (action == null)
				return false;
	
			action(mi);
			return true;
		}
		
		int paletteHeight = 0;
		int numActualRows = 0;
		public override void DrawInner(World world)
		{	
			if (!IsVisible()) return;
			paletteHeight = DrawPalette(world, currentTab);
			DrawBuildTabs(world, paletteHeight);
		}

		int DrawPalette(World world, string queueName)
		{
			string paletteCollection = "palette-" + world.LocalPlayer.Country.Race;

			buttons.Clear();


			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);

			if (queueName == null) return 0;

			// Collect info

			var x = 0;
			var y = 0;
			var buildableItems = Rules.TechTree.BuildableItems(world.LocalPlayer, queueName).ToArray();
			var allBuildables = Rules.TechTree.AllBuildables(queueName)
				.Where(a => a.Traits.Get<BuildableInfo>().Owner.Contains(world.LocalPlayer.Country.Race))
				.OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder)
				.ToArray();

			var queue = world.LocalPlayer.PlayerActor.Trait<ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();
			numActualRows = Math.Max((allBuildables.Length + Columns - 1) / Columns, Rows);

			// Palette Background
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "top"), new float2(origin.X - 9, origin.Y - 9));
			for (var w = 0; w < numActualRows; w++)
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage(paletteCollection, "bg-" + (w % 4).ToString()),
					new float2(origin.X - 9, origin.Y + 48 * w));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "bottom"),
				new float2(origin.X - 9, origin.Y - 1 + 48 * numActualRows));
			Game.Renderer.RgbaSpriteRenderer.Flush();


			// Icons
			string tooltipItem = null;
			foreach (var item in allBuildables)
			{
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;
				WidgetUtils.DrawSHP(tabSprites[item.Name], drawPos);

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item.Name);

				if (rect.Contains(Viewport.LastMousePos.ToPoint()))
					tooltipItem = item.Name;

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex("idle",
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / firstOfThis.TotalTime);
					clock.Tick();
					WidgetUtils.DrawSHP(clock.Image, drawPos);

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

				var closureName = buildableItems.Contains(item.Name) ? item.Name : null;
				buttons.Add(Pair.New(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height), HandleClick(closureName, world)));

				if (++x == Columns) { x = 0; y++; }
			}
			if (x != 0) y++;

			foreach (var ob in overlayBits)
				WidgetUtils.DrawSHP(ob.First, ob.Second);

			Game.Renderer.WorldSpriteRenderer.Flush();

			// Tooltip
			if (tooltipItem != null && !paletteAnimating && paletteOpen)
				DrawProductionTooltip(world, tooltipItem, 
					new float2(Game.viewport.Width, origin.Y + numActualRows * 48 + 9).ToInt2());

			// Palette Dock
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-top"),
				new float2(Game.viewport.Width - 14, origin.Y - 23));

			for (int i = 0; i < numActualRows; i++)
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-" + (i % 4).ToString()),
					new float2(Game.viewport.Width - 14, origin.Y + 48 * i));

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-bottom"),
				new float2(Game.viewport.Width - 14, origin.Y - 1 + 48 * numActualRows));
			Game.Renderer.RgbaSpriteRenderer.Flush();

			return 48 * y + 9;
		}
		
		Action<MouseInput> HandleClick(string name, World world)
		{
			return mi => {
				Sound.Play(TabClick);
				
				if (name != null)
					HandleBuildPalette(world, name, (mi.Button == MouseButton.Left));
			};
		}
		
		Action<MouseInput> HandleTabClick(string button, World world)
		{
			return mi => {
				if (mi.Button != MouseButton.Left)
					return;
				
				Sound.Play(TabClick);
				var wasOpen = paletteOpen;
				paletteOpen = (currentTab == button && wasOpen) ? false : true;
				currentTab = button;
				if (wasOpen != paletteOpen)
					paletteAnimating = true;
			};
		}
		
		static string Description( string a )
		{
			if( a[ 0 ] == '@' )
				return "any " + a.Substring( 1 );
			else
				return Rules.Info[ a.ToLowerInvariant() ].Traits.Get<ValuedInfo>().Description;
		}
		
		void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var player = world.LocalPlayer;
			var unit = Rules.Info[item];
			var queue = player.PlayerActor.Trait<Traits.ProductionQueue>();
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var producing = queue.AllItems(unit.Category).FirstOrDefault( a => a.Item == item );

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(unit.Category))
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							world.OrderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
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
		
		void StartProduction( World world, string item )
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);
			Game.IssueOrder(Order.StartProduction(world.LocalPlayer, item, 
				Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
		}

		static Dictionary<string, string> CategoryNameRemaps = new Dictionary<string, string>
		{
			{ "Building", "Structures" },
			{ "Defense", "Defenses" },
			{ "Plane", "Aircraft" },
			{ "Ship", "Ships" },
			{ "Vehicle", "Vehicles" },
		};
		
		void DrawBuildTabs( World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;
			
			tabs.Clear();
			var queue = world.LocalPlayer.PlayerActor.Trait<Traits.ProductionQueue>();
			
			foreach (var q in tabImageNames)
			{
				var groupName = q.Key;
				if (!visibleTabs.Contains(groupName))
					continue;
				
				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				var race = world.LocalPlayer.Country.Race;
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("tabs-"+tabKeys[index], race+"-"+q.Key), new float2(x, y));
				
				var rect = new Rectangle((int)x,(int)y,(int)tabWidth,(int)tabHeight);
				tabs.Add(Pair.New(rect, HandleTabClick(groupName, world)));

				if (rect.Contains(Viewport.LastMousePos.ToPoint()))
				{
					var text = CategoryNameRemaps.ContainsKey(groupName) ? CategoryNameRemaps[groupName] : groupName;
					var sz = Game.Renderer.BoldFont.Measure(text);
					WidgetUtils.DrawPanelPartial("dialog4",
						Rectangle.FromLTRB((int)rect.Left - sz.X - 30, (int)rect.Top, (int)rect.Left - 5, (int)rect.Bottom),
						PanelSides.All);

					Game.Renderer.BoldFont.DrawText(text, 
						new float2(rect.Left - sz.X - 20, rect.Top + 12), Color.White);
				}

				y += tabHeight;
			}
			
			Game.Renderer.RgbaSpriteRenderer.Flush();
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			Game.Renderer.BoldFont.DrawText(text, 
				pos - new int2(Game.Renderer.BoldFont.Measure(text).X, 0), c);
		}

		void DrawProductionTooltip(World world, string unit, int2 pos)
		{
			pos.Y += 15;

			var pl = world.LocalPlayer;
			var p = pos.ToFloat2() - new float2(297, -3);

			var info = Rules.Info[unit];
			var buildable = info.Traits.Get<BuildableInfo>();

			var buildings = Rules.TechTree.GatherBuildings( pl );
			var canBuildThis = Rules.TechTree.CanBuild(info, pl, buildings);

			var longDescSize = Game.Renderer.RegularFont.Measure(buildable.LongDesc.Replace("\\n", "\n")).Y;
			if (!canBuildThis) longDescSize += 8;

			WidgetUtils.DrawPanel("dialog4", new Rectangle(Game.viewport.Width - 300, pos.Y, 300, longDescSize + 65));
			
			Game.Renderer.BoldFont.DrawText(
				buildable.Description + ((buildable.Hotkey != null)? " ({0})".F(buildable.Hotkey.ToUpper()) : ""),
			                                       p.ToInt2() + new int2(5, 5), Color.White);

			var resources = pl.PlayerActor.Trait<PlayerResources>();

			DrawRightAligned("${0}".F(buildable.Cost), pos + new int2(-5, 5),
				(resources.DisplayCash + resources.DisplayOre >= buildable.Cost ? Color.White : Color.Red ));
			
			var lowpower = resources.GetPowerState() != PowerState.Normal;
			var time = ProductionQueue.GetBuildTime(pl.PlayerActor, info.Name) 
				* ((lowpower)? pl.PlayerActor.Info.Traits.Get<ProductionQueueInfo>().LowPowerSlowdown : 1);
			DrawRightAligned(WorldUtils.FormatTime(time), pos + new int2(-5, 35), lowpower ? Color.Red: Color.White);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("{1}{0}".F(bi.Power, bi.Power > 0 ? "+" : ""), pos + new int2(-5, 20),
				                 ((resources.PowerProvided - resources.PowerDrained) >= -bi.Power || bi.Power > 0)? Color.White: Color.Red);
		
			p += new int2(5, 35);
			if (!canBuildThis)
			{
				var prereqs = buildable.Prerequisites
					.Select( a => Description( a ) );
				Game.Renderer.RegularFont.DrawText(
					"Requires {0}".F(string.Join(", ", prereqs.ToArray())), 
					p.ToInt2(),
					Color.White);

				p += new int2(0, 8);
			}

			p += new int2(0, 15);
			Game.Renderer.RegularFont.DrawText(buildable.LongDesc.Replace("\\n", "\n"), 
				p.ToInt2(), Color.White);

			Game.Renderer.RgbaSpriteRenderer.Flush();
		}

        bool DoBuildingHotkey(char c, World world)
        {
			if (!paletteOpen) return false;
			
            var buildable = Rules.TechTree.BuildableItems(world.LocalPlayer, currentTab);

            var toBuild = buildable.FirstOrDefault(b => Rules.Info[b.ToLowerInvariant()].Traits.Get<BuildableInfo>().Hotkey == c.ToString());

            if ( toBuild != null )
			{
				Sound.Play(TabClick);
		    	HandleBuildPalette(world, toBuild, true);
				return true;
			}

			return false;
        }
         
		void TabChange(bool shift)
        {
            int size = visibleTabs.Count();
            if (size > 0)
            {
                int current = visibleTabs.IndexOf(currentTab);
                if (!shift)
                {
                    if (current + 1 >= size)
                        SetCurrentTab(visibleTabs.FirstOrDefault());
                    else
                        SetCurrentTab(visibleTabs[current + 1]);
                }
                else
                {
                    if (current - 1 < 0)
                        SetCurrentTab(visibleTabs.LastOrDefault());
                    else
                        SetCurrentTab(visibleTabs[current - 1]);
                }
            }
        }
	}
}