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
		
		ProductionQueue CurrentQueue = null;
		List<ProductionQueue> VisibleQueues = new List<ProductionQueue>();

		bool paletteOpen = false;
		Dictionary<string, Sprite> iconSprites;
		
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

		public BuildPaletteWidget() : base() { }
		
		public override void Initialize()
		{
			base.Initialize();
			
			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");

			iconSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^' )
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);
			
			IsVisible = () => { return CurrentQueue != null || (CurrentQueue == null && !paletteOpen);  };
		}
		
		public override Rectangle EventBounds
		{
			get { return new Rectangle((int)(paletteOrigin.X) - 24, (int)(paletteOrigin.Y), 215, 48 * numActualRows); }
		}
		
		public override void Tick(World world)
		{
			VisibleQueues.Clear();
			
			var queues = world.Queries.WithTraitMultiple<ProductionQueue>()
				.Where(p => p.Actor.Owner == world.LocalPlayer)
				.Select(p => p.Trait);
			
			if (CurrentQueue != null && CurrentQueue.self.Destroyed)
				CurrentQueue = null;
			
			foreach (var queue in queues)
			{
				if (queue.AllItems().Count() > 0)
					VisibleQueues.Add(queue);
				else if (CurrentQueue == queue)
					CurrentQueue = null;
			}
			if (CurrentQueue == null)
				CurrentQueue = VisibleQueues.FirstOrDefault();
			
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
		
		public void SetCurrentTab(ProductionQueue queue)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			CurrentQueue = queue;
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
			// todo: fix
			paletteHeight = DrawPalette(world, CurrentQueue);
			DrawBuildTabs(world, paletteHeight);
		}

		int DrawPalette(World world, ProductionQueue queue)
		{
			buttons.Clear();
			if (queue == null) return 0;
			
			string paletteCollection = "palette-" + world.LocalPlayer.Country.Race;
			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
						
			// Collect info
			var x = 0;
			var y = 0;
			var buildableItems = queue.BuildableItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			var allBuildables = queue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);

			var overlayBits = new List<Pair<Sprite, float2>>();
			numActualRows = Math.Max((allBuildables.Count() + Columns - 1) / Columns, Rows);

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
			var isBuildingSomething = queue.CurrentItem() != null;
			foreach (var item in allBuildables)
			{
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = new float2(rect.Location);
				WidgetUtils.DrawSHP(iconSprites[item.Name], drawPos);
				
				var firstOfThis = queue.AllQueued().FirstOrDefault(a => a.Item == item.Name);

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

					var repeats = queue.AllQueued().Count(a => a.Item == item.Name);
					if (repeats > 1 || queue.CurrentItem() != firstOfThis)
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
					if (!buildableItems.Any(a => a.Name == item.Name) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureName = buildableItems.Any(a => a.Name == item.Name) ? item.Name : null;
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
		
		Action<MouseInput> HandleTabClick(ProductionQueue queue, World world)
		{
			return mi => {
				if (mi.Button != MouseButton.Left)
					return;
				
				Sound.Play(TabClick);
				var wasOpen = paletteOpen;
				paletteOpen = (CurrentQueue == queue && wasOpen) ? false : true;
				CurrentQueue = queue;
				if (wasOpen != paletteOpen)
					paletteAnimating = true;
			};
		}
		
		static string Description( string a )
		{
			if( a[ 0 ] == '@' )
				return "any " + a.Substring( 1 );
			else
				return Rules.Info[ a.ToLowerInvariant() ].Traits.Get<TooltipInfo>().Name;
		}
		
		void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var unit = Rules.Info[item];
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var producing = CurrentQueue.AllQueued().FirstOrDefault( a => a.Item == item );

			if (isLmb)
			{
				if (producing != null && producing == CurrentQueue.CurrentItem())
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							world.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue.self, item);
						return;
					}

					if (producing.Paused)
					{
						Game.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, false));
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
						Game.IssueOrder(Order.CancelProduction(CurrentQueue.self, item));
					}
					else
					{
						Sound.Play(eva.OnHoldAudio);
						Game.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, true));
					}
				}
			}
		}
		
		void StartProduction( World world, string item )
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);
						
			Game.IssueOrder(Order.StartProduction(CurrentQueue.self, item, 
				Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
		}
		
		void DrawBuildTabs( World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;
			
			tabs.Clear();

			foreach (var queue in VisibleQueues)
			{											
				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem();
				var index = queue == CurrentQueue ? 2 : (producing != null && producing.Done) ? 1 : 0;
				
				var race = world.LocalPlayer.Country.Race;
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("tabs-"+tabKeys[index], race+"-"+queue.Info.Type), new float2(x, y));
				
				var rect = new Rectangle((int)x,(int)y,(int)tabWidth,(int)tabHeight);
				tabs.Add(Pair.New(rect, HandleTabClick(queue, world)));

				if (rect.Contains(Viewport.LastMousePos.ToPoint()))
				{
					var text = queue.Info.Type;
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
			var tooltip = info.Traits.Get<TooltipInfo>();
			var buildable = info.Traits.Get<BuildableInfo>();
			var cost = info.Traits.Get<ValuedInfo>().Cost;
			var canBuildThis = CurrentQueue.CanBuild(info);
			
			var longDescSize = Game.Renderer.RegularFont.Measure(tooltip.Description.Replace("\\n", "\n")).Y;
			if (!canBuildThis) longDescSize += 8;

			WidgetUtils.DrawPanel("dialog4", new Rectangle(Game.viewport.Width - 300, pos.Y, 300, longDescSize + 65));
			
			Game.Renderer.BoldFont.DrawText(
				tooltip.Name + ((buildable.Hotkey != null)? " ({0})".F(buildable.Hotkey.ToUpper()) : ""),
			                                       p.ToInt2() + new int2(5, 5), Color.White);

			var resources = pl.PlayerActor.Trait<PlayerResources>();
			var power = pl.PlayerActor.Trait<PowerManager>();

			DrawRightAligned("${0}".F(cost), pos + new int2(-5, 5),
				(resources.DisplayCash + resources.DisplayOre >= cost ? Color.White : Color.Red ));
			
			var lowpower = power.PowerState != PowerState.Normal;
			var time = CurrentQueue.GetBuildTime(info.Name) 
				* ((lowpower)? CurrentQueue.Info.LowPowerSlowdown : 1);
			DrawRightAligned(WorldUtils.FormatTime(time), pos + new int2(-5, 35), lowpower ? Color.Red: Color.White);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("{1}{0}".F(bi.Power, bi.Power > 0 ? "+" : ""), pos + new int2(-5, 20),
				                 ((power.PowerProvided - power.PowerDrained) >= -bi.Power || bi.Power > 0)? Color.White: Color.Red);
		
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
			Game.Renderer.RegularFont.DrawText(tooltip.Description.Replace("\\n", "\n"), 
				p.ToInt2(), Color.White);

			Game.Renderer.RgbaSpriteRenderer.Flush();
		}

        bool DoBuildingHotkey(char c, World world)
        {
			if (!paletteOpen) return false;
            var toBuild = CurrentQueue.BuildableItems().FirstOrDefault(b => b.Traits.Get<BuildableInfo>().Hotkey == c.ToString());

            if ( toBuild != null )
			{
				Sound.Play(TabClick);
		    	HandleBuildPalette(world, toBuild.Name, true);
				return true;
			}

			return false;
        }
         
		void TabChange(bool shift)
        {
            int size = VisibleQueues.Count();
            if (size > 0)
            {
                int current = VisibleQueues.IndexOf(CurrentQueue);
                if (!shift)
                {
                    if (current + 1 >= size)
                        SetCurrentTab(VisibleQueues.FirstOrDefault());
                    else
                        SetCurrentTab(VisibleQueues[current + 1]);
                }
                else
                {
                    if (current - 1 < 0)
                        SetCurrentTab(VisibleQueues.LastOrDefault());
                    else
                        SetCurrentTab(VisibleQueues[current - 1]);
                }
            }
        }
	}
}