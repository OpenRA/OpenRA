#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Network;
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
		public float2 Pos;
		public List<ProductionItem> Queued;
	}

	public class ProductionPaletteWidget : Widget
	{
		public readonly int Columns = 3;
		public readonly string TabClick = null;
		public readonly string DisabledTabClick = null;
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";

		[Translate] public readonly string ReadyText = "";
		[Translate] public readonly string HoldText = "";

		public string TooltipActor { get; private set; }
		public readonly World World;
		readonly OrderManager orderManager;

		Lazy<TooltipContainerWidget> tooltipContainer;
		ProductionQueue currentQueue;

		public ProductionQueue CurrentQueue
		{
			get { return currentQueue; }
			set { currentQueue = value; RefreshIcons(); }
		}

		public override Rectangle EventBounds { get { return eventBounds; } }
		Dictionary<Rectangle, ProductionIcon> icons = new Dictionary<Rectangle, ProductionIcon>();
		Animation cantBuild, clock;
		Rectangle eventBounds = Rectangle.Empty;
		readonly WorldRenderer worldRenderer;
		SpriteFont overlayFont;
		float2 holdOffset, readyOffset, timeOffset, queuedOffset;

		[ObjectCreator.UseCtor]
		public ProductionPaletteWidget(OrderManager orderManager, World world, WorldRenderer worldRenderer)
		{
			this.orderManager = orderManager;
			this.World = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			cantBuild = new Animation(world, "clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			clock = new Animation(world, "clock");
		}

		public override void Tick()
		{
			if (CurrentQueue != null && !CurrentQueue.self.IsInWorld)
				CurrentQueue = null;

			if (CurrentQueue != null)
				RefreshIcons();
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate,
					new WidgetArgs() { { "palette", this } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var icon = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (mi.Event == MouseInputEvent.Move)
				TooltipActor = icon != null ? icon.Name : null;

			if (icon == null)
				return false;

			// Eat mouse-up events
			if (mi.Event != MouseInputEvent.Down)
				return true;

			var actor = World.Map.Rules.Actors[icon.Name];
			var first = icon.Queued.FirstOrDefault();

			if (mi.Button == MouseButton.Left)
			{
				// Pick up a completed building
				if (first != null && first.Done && actor.Traits.Contains<BuildingInfo>())
				{
					Sound.Play(TabClick);
					World.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue.self, icon.Name);
				}
				else if (first != null && first.Paused)
				{
					// Resume a paused item
					Sound.Play(TabClick);
					World.IssueOrder(Order.PauseProduction(CurrentQueue.self, icon.Name, false));
				}
				else if (CurrentQueue.BuildableItems().Any(a => a.Name == icon.Name))
				{
					// Queue a new item
					Sound.Play(TabClick);
					Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.QueuedAudio, World.LocalPlayer.Country.Race);
					World.IssueOrder(Order.StartProduction(CurrentQueue.self, icon.Name,
						Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
				}
				else
					Sound.Play(DisabledTabClick);
			}
			else if (mi.Button == MouseButton.Right)
			{
				// Hold/Cancel an existing item
				if (first != null)
				{
					Sound.Play(TabClick);

					// instant cancel of things we havent started yet and things that are finished
					if (first.Paused || first.Done || first.TotalCost == first.RemainingCost)
					{
						Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Country.Race);
						World.IssueOrder(Order.CancelProduction(CurrentQueue.self, icon.Name,
							Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));
					}
					else
					{
						Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.OnHoldAudio, World.LocalPlayer.Country.Race);
						World.IssueOrder(Order.PauseProduction(CurrentQueue.self, icon.Name, true));
					}
				}
				else
					Sound.Play(DisabledTabClick);
			}

			return true;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, ProductionIcon>();
			if (CurrentQueue == null)
				return;

			var allBuildables = CurrentQueue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			var i = 0;
			var rb = RenderBounds;
			foreach (var item in allBuildables)
			{
				var x = i % Columns;
				var y = i / Columns;
				var rect = new Rectangle(rb.X + x * 64 + 1, rb.Y + y * 48 + 1, 64, 48);
				var icon = new Animation(World, item.Traits.Get<TooltipInfo>().Icon);
				icon.Play(item.Traits.Get<BuildableInfo>().Icon);
				var pi = new ProductionIcon()
				{
					Name = item.Name,
					Sprite = icon.Image,
					Pos = new float2(rect.Location),
					Queued = CurrentQueue.AllQueued().Where(a => a.Item == item.Name).ToList(),
				};
				icons.Add(rect, pi);
				i++;
			}

			eventBounds = icons.Keys.Aggregate(Rectangle.Union);
		}

		public override void Draw()
		{
			var iconSize = new float2(64, 48);
			var iconOffset = 0.5f * iconSize;

			overlayFont = Game.Renderer.Fonts["TinyBold"];
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0)) / 2;
			queuedOffset = new float2(4, 2);
			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

			if (CurrentQueue == null)
				return;

			var buildableItems = CurrentQueue.BuildableItems();

			// Background
			foreach (var rect in icons.Keys)
				WidgetUtils.DrawPanel("panel-black", rect.InflateBy(1, 1, 1, 1));

			// Icons
			foreach (var icon in icons.Values)
			{
				WidgetUtils.DrawSHPCentered(icon.Sprite, icon.Pos + iconOffset, worldRenderer);

				// Build progress
				if (icon.Queued.Count > 0)
				{
					var first = icon.Queued[0];
					clock.PlayFetchIndex("idle",
						() => (first.TotalTime - first.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / first.TotalTime);
					clock.Tick();
					WidgetUtils.DrawSHPCentered(clock.Image, icon.Pos + iconOffset, worldRenderer);
				}
				else if (!buildableItems.Any(a => a.Name == icon.Name))
					WidgetUtils.DrawSHPCentered(cantBuild.Image, icon.Pos + iconOffset, worldRenderer);
			}

			// Overlays
			foreach (var icon in icons.Values)
			{
				var total = icon.Queued.Count;
				if (total > 0)
				{
					var first = icon.Queued[0];
					var waiting = first != CurrentQueue.CurrentItem() && !first.Done;
					if (first.Done)
					{
						// Blink the ready text
						if (orderManager.LocalFrameNumber / 9 % 2 == 0)
							overlayFont.DrawTextWithContrast(ReadyText,
															 icon.Pos + readyOffset,
															 Color.White, Color.Black, 1);
					}
					else if (first.Paused)
						overlayFont.DrawTextWithContrast(HoldText,
														 icon.Pos + holdOffset,
														 Color.White, Color.Black, 1);
					else if (!waiting)
						overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(first.RemainingTimeActual),
														 icon.Pos + timeOffset,
														 Color.White, Color.Black, 1);

					if (total > 1 || waiting)
						overlayFont.DrawTextWithContrast(total.ToString(),
														 icon.Pos + queuedOffset,
														 Color.White, Color.Black, 1);
				}
			}
		}

		public override string GetCursor(int2 pos)
		{
			var icon = icons.Where(i => i.Key.Contains(pos))
				.Select(i => i.Value).FirstOrDefault();

			return icon != null ? base.GetCursor(pos) : null;
		}
	}
}
