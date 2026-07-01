#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionOverviewWidget : Widget
	{
		public static readonly string[] DefaultGroups = ["Building", "Defense", "Infantry", "Vehicle", "Aircraft", "Ship"];

		static readonly string[] GroupLabelKeys =
		[
			"button-production-types-building-tooltip",
			"button-production-types-defense-tooltip",
			"button-production-types-infantry-tooltip",
			"button-production-types-vehicle-tooltip",
			"button-production-types-aircraft-tooltip",
			"button-production-types-naval-tooltip"
		];

		static readonly Color BorderColor = Color.FromArgb(255, 120, 120, 120);

		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";
		public readonly string TooltipContainer;
		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		public string ProductionPaletteWidget = "PRODUCTION_PALETTE";
		public string ProductionTabsWidget = null;
		public string TitleButtonBackground = "sidebar-button";

		public Func<string, bool> IsGroupDisabled;
		public Func<string, Modifiers, bool> TrySelectGroup;

		public readonly int2 IconSize = new(62, 46);
		public readonly int2 IconMargin = new(1, 0);
		public readonly int2 IconSpriteOffset = new(-1, -1);
		public int SlotPadding = 4;

		public string SidebarCollection = "sidebar";
		public string IconBackgroundImage = "background-iconbg";
		public int TitleBarHeight = 14;
		public string TitleFont = "Tiny";

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";

		[FluentReference]
		public string ReadyText = "";

		[FluentReference]
		public string HoldText = "";

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly OrderManager orderManager;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		readonly Lazy<ProductionTabsWidget> tabsWidget;
		readonly Dictionary<ProductionQueue, Animation> clocks = [];
		readonly List<OverviewEntry> entries = [];
		Rectangle eventBounds = Rectangle.Empty;
		string resolvedSidebarCollection;
		string resolvedTitleButtonBackground;
		Sprite iconBackgroundSprite;
		string[] groupLabels;

		SpriteFont overlayFont;
		SpriteFont titleFont;
		float2 iconOffset;
		float2 holdOffset;
		float2 readyOffset;
		float2 timeOffset;
		int lastIconIdx;
		int currentTooltipToken;
		int? hoveredTitleIndex;
		bool titlePressed;

		int SlotWidth => IconSize.X + 2 * SlotPadding;
		int IconRowHeight => IconSize.Y + 2 * SlotPadding;
		int SlotStride => SlotWidth + IconMargin.X;

		Rectangle GetSlotRect(Rectangle rb, int index, int y, int height)
		{
			var x = rb.X + index * SlotStride;
			var width = index == DefaultGroups.Length - 1 ? rb.Right - x : SlotWidth;
			return new Rectangle(x, y, width, height);
		}

		Rectangle GetIconRect(Rectangle rb, int index, int iconRowY)
		{
			var slot = GetSlotRect(rb, index, iconRowY, IconRowHeight);
			return new Rectangle(slot.X + SlotPadding, slot.Y + SlotPadding, IconSize.X, IconSize.Y);
		}

		Sprite GetUniformGreyTile(Sprite source)
		{
			var cellWidth = source.Bounds.Width / 3;
			var cellBounds = new Rectangle(source.Bounds.X, source.Bounds.Y, cellWidth, source.Bounds.Height);
			return new Sprite(source.Sheet, cellBounds, source.Channel);
		}

		void DrawUniformCell(Sprite source, Rectangle rect)
		{
			WidgetUtils.DrawSprite(GetUniformGreyTile(source), rect.Location, new float2(rect.Width, rect.Height));
		}

		void DrawSlotBackgrounds(Rectangle rb, int iconRowY)
		{
			for (var i = 0; i < DefaultGroups.Length; i++)
				DrawUniformCell(iconBackgroundSprite, GetSlotRect(rb, i, iconRowY, IconRowHeight));
		}

		bool IsGroupAvailable(string group)
		{
			if (IsGroupDisabled != null)
				return !IsGroupDisabled(group);

			var tabs = tabsWidget.Value;
			if (tabs != null)
				return tabs.Groups.TryGetValue(group, out var tabGroup) &&
					tabGroup.Tabs.Any(t => t.Queue.BuildableItems().Any());

			var player = world.LocalPlayer;
			if (player == null)
				return false;

			return player.PlayerActor.TraitsImplementing<ProductionQueue>()
				.Any(q => (q.Info.Group ?? q.Info.Type) == group && q.Actor.IsInWorld && q.AnyItemsToBuild());
		}

		string GetActiveGroup()
		{
			var tabs = tabsWidget.Value;
			if (tabs != null)
				return tabs.QueueGroup;

			var palette = paletteWidget.Value;
			if (palette?.CurrentQueue == null)
				return null;

			return palette.CurrentQueue.Info.Group ?? palette.CurrentQueue.Info.Type;
		}

		bool SelectProductionGroup(string group, bool reverse)
		{
			var modifiers = reverse ? Modifiers.Shift : Modifiers.None;
			if (TrySelectGroup != null)
				return TrySelectGroup(group, modifiers);

			if (!IsGroupAvailable(group))
				return false;

			var tabs = tabsWidget.Value;
			if (tabs != null)
			{
				if (tabs.QueueGroup == group)
					tabs.SelectNextTab(reverse);
				else
					tabs.QueueGroup = group;

				tabs.PickUpCompletedBuilding();
				return true;
			}

			var palette = paletteWidget.Value;
			if (palette == null)
				return false;

			var queues = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
				.Where(q => (q.Info.Group ?? q.Info.Type) == group && q.Actor.IsInWorld)
				.ToArray();

			palette.CurrentQueue = queues.FirstOrDefault(q => q.Enabled);
			palette.ScrollToTop();
			palette.PickUpCompletedBuilding();
			return true;
		}

		int? GetTitleIndexAt(int2 location, Rectangle rb)
		{
			for (var i = 0; i < DefaultGroups.Length; i++)
			{
				if (GetSlotRect(rb, i, rb.Y, TitleBarHeight).Contains(location))
					return i;
			}

			return null;
		}

		void DrawTitleLabels(Rectangle rb)
		{
			var activeGroup = GetActiveGroup();

			for (var i = 0; i < groupLabels.Length; i++)
			{
				var group = DefaultGroups[i];
				var slot = GetSlotRect(rb, i, rb.Y, TitleBarHeight);
				var disabled = !IsGroupAvailable(group);
				var hover = hoveredTitleIndex == i && Ui.MouseOverWidget == this;
				var highlighted = group == activeGroup;
				var pressed = titlePressed && hover;

				ButtonWidget.DrawBackground(resolvedTitleButtonBackground, slot, disabled, pressed, hover, highlighted);

				var label = groupLabels[i];
				var textSize = titleFont.Measure(label);
				var textPos = new float2(
					slot.X + (slot.Width - textSize.X) / 2f,
					slot.Y + (TitleBarHeight - textSize.Y) / 2f);
				var textColor = disabled ? Color.Gray :
					highlighted ? Color.Gold : Color.White;
				titleFont.DrawTextWithContrast(label, textPos, textColor, Color.Black, 1);
			}
		}

		void DrawBorders(Rectangle rb)
		{
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, rb.Top),
				new int2(rb.Right, rb.Bottom),
				1,
				BorderColor);

			var dividerY = rb.Y + TitleBarHeight;
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, dividerY),
				new int2(rb.Right, dividerY),
				1,
				BorderColor);

			for (var i = 1; i < DefaultGroups.Length; i++)
			{
				var x = rb.X + i * SlotStride;
				Game.Renderer.RgbaColorRenderer.DrawRect(
					new int2(x, rb.Top),
					new int2(x, rb.Bottom),
					1,
					BorderColor);
			}
		}

		struct OverviewEntry
		{
			public Rectangle Bounds;
			public ProductionIcon Icon;
		}

		[ObjectCreator.UseCtor]
		public ProductionOverviewWidget(World world, WorldRenderer worldRenderer, OrderManager orderManager)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			this.orderManager = orderManager;
			GetTooltipIcon = () => TooltipIcon;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			paletteWidget = Exts.Lazy(() =>
				ProductionPaletteWidget != null ? Ui.Root.GetOrNull<ProductionPaletteWidget>(ProductionPaletteWidget) : null);
			tabsWidget = Exts.Lazy(() =>
				ProductionTabsWidget != null ? Ui.Root.GetOrNull<ProductionTabsWidget>(ProductionTabsWidget) : null);
		}

		protected ProductionOverviewWidget(ProductionOverviewWidget other)
			: base(other)
		{
			world = other.world;
			worldRenderer = other.worldRenderer;
			orderManager = other.orderManager;

			IconSize = other.IconSize;
			IconMargin = other.IconMargin;
			IconSpriteOffset = other.IconSpriteOffset;
			SlotPadding = other.SlotPadding;

			SidebarCollection = other.SidebarCollection;
			IconBackgroundImage = other.IconBackgroundImage;
			TitleBarHeight = other.TitleBarHeight;
			TitleFont = other.TitleFont;

			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;

			ReadyText = other.ReadyText;
			HoldText = other.HoldText;

			TooltipIcon = other.TooltipIcon;
			GetTooltipIcon = () => TooltipIcon;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;
			ClickSound = other.ClickSound;
			ClickDisabledSound = other.ClickDisabledSound;

			ProductionPaletteWidget = other.ProductionPaletteWidget;
			ProductionTabsWidget = other.ProductionTabsWidget;
			TitleButtonBackground = other.TitleButtonBackground;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			paletteWidget = Exts.Lazy(() =>
				ProductionPaletteWidget != null ? Ui.Root.GetOrNull<ProductionPaletteWidget>(ProductionPaletteWidget) : null);
			tabsWidget = Exts.Lazy(() =>
				ProductionTabsWidget != null ? Ui.Root.GetOrNull<ProductionTabsWidget>(ProductionTabsWidget) : null);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			overlayFont = Game.Renderer.Fonts["TinyBold"];
			titleFont = Game.Renderer.Fonts[TitleFont];
			ReadyText = FluentProvider.GetMessage(ReadyText);
			HoldText = FluentProvider.GetMessage(HoldText);
			groupLabels = GroupLabelKeys.Select(k => FluentProvider.GetMessage(k)).ToArray();

			iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;
			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

			resolvedSidebarCollection = ResolveSidebarCollection(SidebarCollection);
			iconBackgroundSprite = ChromeProvider.GetImage(resolvedSidebarCollection, IconBackgroundImage);
			resolvedTitleButtonBackground = ResolveTitleButtonBackground(TitleButtonBackground);
		}

		string ResolveTitleButtonBackground(string background)
		{
			var player = world.LocalPlayer;
			if (player == null || player.Spectating)
				return background;

			if (ChromeMetrics.TryGet("FactionSuffix-" + player.Faction.InternalName, out string faction))
				return background + "-" + faction;

			return background + "-" + player.Faction.InternalName;
		}

		void DrawTitleBar(Rectangle rb)
		{
			DrawTitleLabels(rb);
		}

		string ResolveSidebarCollection(string collection)
		{
			var player = world.LocalPlayer;
			if (player == null || player.Spectating)
				return collection;

			if (ChromeMetrics.TryGet("FactionSuffix-" + player.Faction.InternalName, out string faction))
				return collection + "-" + faction;

			return collection + "-" + player.Faction.InternalName;
		}

		public override ProductionOverviewWidget Clone() { return new ProductionOverviewWidget(this); }

		public override Rectangle EventBounds =>
			world.LocalPlayer != null && !world.LocalPlayer.Spectating ? RenderBounds : eventBounds;

		IEnumerable<ProductionQueue> QueuesForGroup(string group)
		{
			var player = world.LocalPlayer;
			if (player == null)
				return [];

			return player.PlayerActor.TraitsImplementing<ProductionQueue>()
				.Where(q => (q.Info.Group ?? q.Info.Type) == group && q.Actor.IsInWorld);
		}

		static (ProductionQueue Queue, ProductionItem Item)? SelectDisplayItem(IEnumerable<ProductionQueue> queues)
		{
			var active = queues
				.Select(q => new { Queue = q, Item = q.CurrentItem() })
				.Where(x => x.Item != null)
				.OrderBy(x => x.Item.Done ? 0 : x.Item.Paused ? 2 : 1)
				.ThenBy(x => x.Item.RemainingTimeActual)
				.FirstOrDefault();

			if (active == null)
				return null;

			return (active.Queue, active.Item);
		}

		bool PickUpCompletedBuilding(ProductionQueue queue, ProductionItem item)
		{
			if (item == null || !item.Done)
				return false;

			var actor = world.Map.Rules.Actors[item.Item];
			if (!actor.HasTraitInfo<BuildingInfo>())
				return false;

			world.OrderGenerator = new PlaceBuildingOrderGenerator(queue, item.Item, worldRenderer);
			return true;
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			titlePressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return false;

			var rb = RenderBounds;
			var titleIndex = GetTitleIndexAt(mi.Location, rb);

			if (mi.Event == MouseInputEvent.Move)
				hoveredTitleIndex = titleIndex;

			if (titleIndex != null)
			{
				if (mi.Button != MouseButton.Left)
					return true;

				var titleRect = GetSlotRect(rb, titleIndex.Value, rb.Y, TitleBarHeight);
				var disabled = !IsGroupAvailable(DefaultGroups[titleIndex.Value]);

				if (mi.Event == MouseInputEvent.Down)
				{
					titlePressed = titleRect.Contains(mi.Location) && !disabled;
					TakeMouseFocus(mi);
					if (disabled)
						Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);
					return true;
				}

				if (mi.Event == MouseInputEvent.Up)
				{
					if (titlePressed && titleRect.Contains(mi.Location))
					{
						var group = DefaultGroups[titleIndex.Value];
						var reverse = mi.Modifiers.HasModifier(Modifiers.Shift);
						var sound = SelectProductionGroup(group, reverse) ? ClickSound : ClickDisabledSound;
						Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", sound, null);
					}

					titlePressed = false;
					if (HasMouseFocus)
						YieldMouseFocus(mi);
					return true;
				}

				return true;
			}

			var entry = entries.FirstOrDefault(e => e.Bounds.Contains(mi.Location));
			if (entry.Icon == null)
				return false;

			if (mi.Event == MouseInputEvent.Move)
				TooltipIcon = entry.Icon;

			if (mi.Event != MouseInputEvent.Down)
				return true;

			var item = entry.Icon.Queued[0];
			if (PickUpCompletedBuilding(entry.Icon.ProductionQueue, item))
				Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickSound, null);

			return true;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null || world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs { { "player", world.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", world } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override void Tick()
		{
			if (TooltipContainer == null || world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			if (Ui.MouseOverWidget != this)
			{
				if (TooltipIcon != null)
				{
					tooltipContainer.Value.RemoveTooltip(currentTooltipToken);
					lastIconIdx = 0;
					TooltipIcon = null;
				}

				return;
			}

			if (TooltipIcon != null &&
				entries.Count > lastIconIdx &&
				entries[lastIconIdx].Icon?.Actor == TooltipIcon.Actor &&
				entries[lastIconIdx].Bounds.Contains(Viewport.LastMousePos))
				return;

			for (var i = 0; i < entries.Count; i++)
			{
				if (!entries[i].Bounds.Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipIcon = entries[i].Icon;
				currentTooltipToken = tooltipContainer.Value.SetTooltip(
					TooltipTemplate,
					new WidgetArgs { { "player", world.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", world } });
				return;
			}

			TooltipIcon = null;
		}

		public override void Draw()
		{
			entries.Clear();
			eventBounds = Rectangle.Empty;

			var player = world.LocalPlayer;
			if (player == null || player.Spectating)
				return;

			var rb = RenderBounds;
			var iconRowY = rb.Y + TitleBarHeight;
			var bounds = new List<Rectangle>();
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0, world.Timestep)) / 2;

			Game.Renderer.EnableAntialiasingFilter();

			DrawSlotBackgrounds(rb, iconRowY);
			DrawTitleBar(rb);
			DrawBorders(rb);

			for (var groupIndex = 0; groupIndex < DefaultGroups.Length; groupIndex++)
			{
				var group = DefaultGroups[groupIndex];
				var iconRect = GetIconRect(rb, groupIndex, iconRowY);
				var display = SelectDisplayItem(QueuesForGroup(group));
				if (display == null)
					continue;

				var queue = display.Value.Queue;
				var current = display.Value.Item;

				if (!clocks.ContainsKey(queue))
					clocks.Add(queue, new Animation(world, ClockAnimation));

				var actor = queue.AllItems().FirstOrDefault(a => a.Name == current.Item);
				if (actor == null)
					continue;

				var faction = player.Faction.InternalName;
				var rsi = actor.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(world, rsi.GetImage(actor, faction));
				var bi = actor.TraitInfo<BuildableInfo>();
				icon.Play(bi.Icon);

				var paletteName = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + player.InternalName : bi.IconPalette;
				var pos = new float2(iconRect.Location);
				var palette = worldRenderer.Palette(paletteName);
				var clockPalette = worldRenderer.Palette(ClockPalette);

				WidgetUtils.DrawSpriteCentered(icon.Image, palette, pos + iconOffset);

				var queued = queue.AllQueued().Where(a => a.Item == current.Item).ToList();
				var productionIcon = new ProductionIcon
				{
					Actor = actor,
					Name = actor.Name,
					Sprite = icon.Image,
					Palette = palette,
					IconClockPalette = clockPalette,
					Pos = pos,
					Queued = queued,
					ProductionQueue = queue
				};

				entries.Add(new OverviewEntry { Bounds = iconRect, Icon = productionIcon });
				bounds.Add(iconRect);

				var pios = player.PlayerActor.TraitsImplementing<IProductionIconOverlay>();
				foreach (var pio in pios.Where(p => p.IsOverlayActive(actor)))
					WidgetUtils.DrawSpriteCentered(pio.Sprite, worldRenderer.Palette(pio.Palette),
						pos + iconOffset + pio.Offset(IconSize));

				if (!current.Done)
				{
					var queueClock = clocks[queue];
					queueClock.PlayFetchIndex(ClockSequence, () => current.TotalTime == 0 ? 0 :
						(current.TotalTime - current.RemainingTime) * (queueClock.CurrentSequence.Length - 1) / current.TotalTime);
					queueClock.Tick();
					WidgetUtils.DrawSpriteCentered(queueClock.Image, clockPalette, pos + iconOffset);
				}
			}

			Game.Renderer.DisableAntialiasingFilter();

			foreach (var entry in entries)
			{
				var icon = entry.Icon;
				var first = icon.Queued[0];
				var queue = icon.ProductionQueue;
				var waiting = !queue.IsProducing(first) && !first.Done;
				var pos = icon.Pos;
				var total = icon.Queued.Count;

				if (total > 0)
				{
					if (first.Done)
					{
						if (orderManager.LocalFrameNumber * world.Timestep / 360 % 2 == 0)
							overlayFont.DrawTextWithContrast(ReadyText, pos + readyOffset, Color.White, Color.Black, 1);
					}
					else if (first.Paused)
						overlayFont.DrawTextWithContrast(HoldText, pos + holdOffset, Color.White, Color.Black, 1);
					else if (!waiting)
						overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(queue.RemainingTimeActual(first), world.Timestep),
							pos + timeOffset, Color.White, Color.Black, 1);

					if (total > 1 || waiting)
					{
						var text = total.ToString(NumberFormatInfo.CurrentInfo);
						overlayFont.DrawTextWithContrast(text, pos + new float2(4, 2), Color.White, Color.Black, 1);
					}
				}
			}

			eventBounds = bounds.Count > 0 ? bounds.Union() : Rectangle.Empty;
		}
	}
}
