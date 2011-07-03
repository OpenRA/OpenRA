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
	public class ProductionIcon
	{
		public string Name;
		public Sprite Sprite;
		public List<ProductionItem> Queued;
	}

	public class ProductionPaletteWidget : Widget
	{
		public readonly int Columns = 3;
		public readonly string TabClick = "button.aud";

		ProductionQueue currentQueue;
		public ProductionQueue CurrentQueue
		{
			get
			{
				return currentQueue;
			}
			set
			{
				currentQueue = value;
				RefreshIcons();
			}
		}

		public override Rectangle EventBounds { get { return eventBounds; } }
		Dictionary<Rectangle, ProductionIcon> Icons = new Dictionary<Rectangle, ProductionIcon>();
		Dictionary<string, Sprite> iconSprites;
		Animation cantBuild, clock;
		Rectangle eventBounds = Rectangle.Empty;
		readonly WorldRenderer worldRenderer;
		readonly World world;

		[ObjectCreator.UseCtor]
		public ProductionPaletteWidget([ObjectCreator.Param] World world,
		                               [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
						
			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
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
			if (CurrentQueue != null && !CurrentQueue.self.IsInWorld)
				CurrentQueue = null;

			if (CurrentQueue != null)
				RefreshIcons();
		}
		
		public override bool HandleMouseInput(MouseInput mi)
		{			
			if (mi.Event != MouseInputEvent.Down)
				return false;
			
			var clicked = Icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();
			
			if (clicked == null)
				return true;
			
			var actor = Rules.Info[clicked.Name];
			var first = clicked.Queued.FirstOrDefault();
			
			if (mi.Button == MouseButton.Left)
			{
				Sound.Play(TabClick);

				// Pick up a completed building
				if (first != null && first.Done && actor.Traits.Contains<BuildingInfo>())
					world.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue.self, clicked.Name);

				// Resume a paused item
				else if (first != null && first.Paused)
					world.IssueOrder(Order.PauseProduction(CurrentQueue.self, clicked.Name, false));

				// Queue a new item
				else StartProduction(world, clicked.Name);
			}

			// Hold/Cancel an existing item
			else if (mi.Button == MouseButton.Right && first != null)
			{
				Sound.Play(TabClick);

				// instant cancel of things we havent started yet and things that are finished
				if (first.Paused || first.Done || first.TotalCost == first.RemainingCost)
				{
					Sound.Play(CurrentQueue.Info.CancelledAudio);
					world.IssueOrder(Order.CancelProduction(CurrentQueue.self, clicked.Name,
						Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
				}
				else
				{
					Sound.Play(CurrentQueue.Info.OnHoldAudio);
					world.IssueOrder(Order.PauseProduction(CurrentQueue.self, clicked.Name, true));
				}
			}
			return true;
		}
		
		void StartProduction( World world, string item )
		{
			Sound.Play(CurrentQueue.Info.QueuedAudio);
			world.IssueOrder(Order.StartProduction(CurrentQueue.self, item, 
				Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
		}
		
		public void RefreshIcons()
		{
			Icons = new Dictionary<Rectangle, ProductionIcon>();
			if (CurrentQueue == null)
				return;

			var allBuildables = CurrentQueue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			var i = 0;
			var rb = RenderBounds;
			foreach (var item in allBuildables)
			{
				var x = i % Columns;
				var y = i / Columns;
				var rect =  new Rectangle(rb.X + x * 64 + 1, rb.Y + y * 48 + 1, 64, 48);
				var pi = new ProductionIcon()
				{
					Sprite = iconSprites[item.Name],
					Queued = CurrentQueue.AllQueued().Where(a => a.Item == item.Name).ToList(),
					Name = item.Name
				};
				Icons.Add(rect, pi);
				i++;
			}

			eventBounds = Icons.Keys.Aggregate(Rectangle.Union);
		}

		public override void Draw()
		{
			if (CurrentQueue == null)
				return;

			var isBuildingSomething = CurrentQueue.CurrentItem() != null;
			var buildableItems = CurrentQueue.BuildableItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			
			var overlayFont = Game.Renderer.Fonts["TinyBold"];
			var holdOffset = new float2(32,24) - overlayFont.Measure("On Hold") / 2;
			var readyOffset = new float2(32,24) - overlayFont.Measure("Ready") / 2;
			var queuedOffset = new float2(4,2);
			
			foreach (var kv in Icons)
				WidgetUtils.DrawPanel("panel-black", kv.Key.InflateBy(1,1,1,1));

			foreach (var kv in Icons)
			{
				var rect = kv.Key;
				var icon = kv.Value;
				var drawPos = new float2(rect.Location);
				WidgetUtils.DrawSHP(icon.Sprite, drawPos, worldRenderer);

				// Build progress
				var total = icon.Queued.Count;
				if (total > 0)
				{
					var first = icon.Queued[0];
					clock.PlayFetchIndex("idle",
						() => (first.TotalTime - first.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / first.TotalTime);
					clock.Tick();
					WidgetUtils.DrawSHP(clock.Image, drawPos, worldRenderer);

					if (first.Done)
						overlayFont.DrawTextWithContrast("Ready",
						                                 drawPos + readyOffset,
						                                 Color.White, Color.Black, 1);
					else if (first.Paused)
						overlayFont.DrawTextWithContrast("On Hold",
						                                 drawPos + holdOffset,
						                                 Color.White, Color.Black, 1);

					if (total > 1 || (first != CurrentQueue.CurrentItem() && !first.Done))
						overlayFont.DrawTextWithContrast(total.ToString(),
						                                 drawPos + queuedOffset,
						                                 Color.White, Color.Black, 1);
				}
				else if (isBuildingSomething || !buildableItems.Any(a => a.Name == icon.Name))
					WidgetUtils.DrawSHP(cantBuild.Image, drawPos, worldRenderer);
			}
		}
	}
}
