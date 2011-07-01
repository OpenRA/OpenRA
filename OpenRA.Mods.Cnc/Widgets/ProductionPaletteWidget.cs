#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionPaletteWidget : Widget
	{
		public readonly int Columns = 3;
		public readonly string BuildPaletteOpen = "appear1.aud";
		public readonly string BuildPaletteClose = "appear1.aud";
		public readonly string TabClick = "button.aud";
		public ProductionQueue CurrentQueue = null;

		Dictionary<string, Sprite> iconSprites;
		Animation cantBuild;
		Animation ready;
		Animation clock;
		List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();
		readonly WorldRenderer worldRenderer;
		readonly World world;
		[ObjectCreator.UseCtor]
		public ProductionPaletteWidget( [ObjectCreator.Param] World world, [ObjectCreator.Param] WorldRenderer worldRenderer )
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
						
			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
			
			iconSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^')
				.ToDictionary(
					u => u.Name,
					u => Game.modData.SpriteLoader.LoadAllSprites(
                        u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);
		}

		public override void Tick()
		{
			if (CurrentQueue != null && CurrentQueue.self.Destroyed)
				CurrentQueue = null;
			
			base.Tick();
		}
		
		public override Rectangle EventBounds
		{
			get { return (buttons.Count == 0) ? Rectangle.Empty : buttons.Select(kv => kv.First).Aggregate(Rectangle.Union); }
		}
		
		
		// TODO: BuildPaletteWidget doesn't support delegate methods for mouse input
		public override bool HandleMouseInput(MouseInput mi)
		{			
			if (mi.Event != MouseInputEvent.Down)
				return false;
			
			var action = buttons.Where(a => a.First.Contains(mi.Location))
					.Select(a => a.Second).FirstOrDefault();
			
			action(mi);
			return true;
		}
		
		public override void DrawInner()
		{	
			if (!IsVisible()) return;
			buttons.Clear();
			
			var x = 0;
			var y = 0;
			
			if (CurrentQueue != null)
			{
				var buildableItems = CurrentQueue.BuildableItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
				var allBuildables = CurrentQueue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
				var overlayBits = new List<Pair<Sprite, float2>>();
	
				// Icons
				string tooltipItem = null;
				var isBuildingSomething = CurrentQueue.CurrentItem() != null;
				foreach (var item in allBuildables)
				{
					var rect = new RectangleF(RenderOrigin.X + x * 64, RenderOrigin.Y + 48 * y, 64, 48);
					var drawPos = new float2(rect.Location);
					WidgetUtils.DrawSHP(iconSprites[item.Name], drawPos, worldRenderer);
					
	
					if (rect.Contains(Viewport.LastMousePos))
						tooltipItem = item.Name;
	
					var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);
					
					// Build progress
					var firstOfThis = CurrentQueue.AllQueued().FirstOrDefault(a => a.Item == item.Name);
					if (firstOfThis != null)
					{
						clock.PlayFetchIndex("idle",
							() => (firstOfThis.TotalTime - firstOfThis.RemainingTime)
								* (clock.CurrentSequence.Length - 1) / firstOfThis.TotalTime);
						clock.Tick();
						WidgetUtils.DrawSHP(clock.Image, drawPos, worldRenderer);
	
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
	
						var repeats = CurrentQueue.AllQueued().Count(a => a.Item == item.Name);
						if (repeats > 1 || CurrentQueue.CurrentItem() != firstOfThis)
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
					WidgetUtils.DrawSHP(ob.First, ob.Second, worldRenderer);
	
				// Tooltip
				if (tooltipItem != null)
					DrawProductionTooltip(world, tooltipItem, 
						new float2(Game.viewport.Width, Game.viewport.Height - 100).ToInt2());
			}
		}
		
		Action<MouseInput> HandleClick(string name, World world)
		{
			return mi => {
				Sound.Play(TabClick);
				
				if (name != null)
					HandleBuildPalette(world, name, (mi.Button == MouseButton.Left));
			};
		}
		
		static string Description( string a )
		{
			// hack hack hack - going to die soon anyway
			if (a == "barracks")
				return "Infantry production";
			if (a == "vehicleproduction")
				return "Vehicle production";
			if (a == "techcenter")
				return "Tech Center";
			if (a == "anypower")
				return "Power Plant";
			
			ActorInfo ai;
			Rules.Info.TryGetValue(a.ToLowerInvariant(), out ai);
			if (ai != null && ai.Traits.Contains<TooltipInfo>())
				return ai.Traits.Get<TooltipInfo>().Name;
			
			return a;
		}
		
		void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var unit = Rules.Info[item];
			var producing = CurrentQueue.AllQueued().FirstOrDefault( a => a.Item == item );

			if (isLmb)
			{
				if (producing != null && producing == CurrentQueue.CurrentItem())
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							world.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue.self, item);
						else
							StartProduction( world, item );
						return;
					}

					if (producing.Paused)
					{
						world.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, false));
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
						Sound.Play(CurrentQueue.Info.CancelledAudio);
						int numberToCancel = Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1;
						if (Game.GetModifierKeys().HasModifier(Modifiers.Shift) &&
							Game.GetModifierKeys().HasModifier(Modifiers.Ctrl))
						{
							numberToCancel = -1; //cancel all
						}
						world.IssueOrder(Order.CancelProduction(CurrentQueue.self, item, numberToCancel));
					}
					else
					{
						Sound.Play(CurrentQueue.Info.OnHoldAudio);
						world.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, true));
					}
				}
			}
		}
		
		void StartProduction( World world, string item )
		{
			Sound.Play(CurrentQueue.Info.QueuedAudio);
			world.IssueOrder(Order.StartProduction(CurrentQueue.self, item, 
				Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
		}
		
		void DrawRightAligned(string text, int2 pos, Color c)
		{
			Game.Renderer.Fonts["Bold"].DrawText(text, 
				pos - new int2(Game.Renderer.Fonts["Bold"].Measure(text).X, 0), c);
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
			
			var longDescSize = Game.Renderer.Fonts["Regular"].Measure(tooltip.Description.Replace("\\n", "\n")).Y;
			if (!canBuildThis) longDescSize += 8;

			WidgetUtils.DrawPanel("dialog4", new Rectangle(Game.viewport.Width - 300, pos.Y, 300, longDescSize + 65));
			
			Game.Renderer.Fonts["Bold"].DrawText(
				tooltip.Name + ((buildable.Hotkey != null)? " ({0})".F(buildable.Hotkey.ToUpper()) : ""),
			                                       p.ToInt2() + new int2(5, 5), Color.White);

			var resources = pl.PlayerActor.Trait<PlayerResources>();
			var power = pl.PlayerActor.Trait<PowerManager>();

			DrawRightAligned("${0}".F(cost), pos + new int2(-5, 5),
				(resources.DisplayCash + resources.DisplayOre >= cost ? Color.White : Color.Red ));
			
			var lowpower = power.PowerState != PowerState.Normal;
			var time = CurrentQueue.GetBuildTime(info.Name) 
				* ((lowpower)? CurrentQueue.Info.LowPowerSlowdown : 1);
			DrawRightAligned(WidgetUtils.FormatTime(time), pos + new int2(-5, 35), lowpower ? Color.Red: Color.White);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("{1}{0}".F(bi.Power, bi.Power > 0 ? "+" : ""), pos + new int2(-5, 20),
				                 ((power.PowerProvided - power.PowerDrained) >= -bi.Power || bi.Power > 0)? Color.White: Color.Red);
		
			p += new int2(5, 35);
			if (!canBuildThis)
			{
				var prereqs = buildable.Prerequisites
					.Select( a => Description( a ) );
				Game.Renderer.Fonts["Regular"].DrawText(
					"Requires {0}".F(string.Join(", ", prereqs.ToArray())), 
					p.ToInt2(),
					Color.White);

				p += new int2(0, 8);
			}

			p += new int2(0, 15);
			Game.Renderer.Fonts["Regular"].DrawText(tooltip.Description.Replace("\\n", "\n"), 
				p.ToInt2(), Color.White);
		}
	}
}
