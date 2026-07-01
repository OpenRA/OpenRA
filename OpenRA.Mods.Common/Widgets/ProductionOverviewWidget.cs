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
	public class ProductionQueueOverviewWidget : Widget
	{
		public static readonly string[] DefaultGroups = ["Building", "Defense", "Infantry", "Vehicle", "Aircraft", "Ship"];

		static readonly string[] GroupIconNames =
		[
			"building",
			"defense",
			"infantry",
			"vehicle",
			"aircraft",
			"ship"
		];

		static readonly Color BorderColor = Color.FromArgb(255, 120, 120, 120);
		static readonly Color CountColor = Color.White;
		static readonly Color PriorityFill = Color.FromArgb(255, 45, 45, 55);
		static readonly Color PriorityFillHover = Color.FromArgb(255, 60, 60, 75);
		static readonly Color PriorityFillPressed = Color.FromArgb(255, 110, 90, 25);
		static readonly Color PriorityFillDisabled = Color.FromArgb(255, 50, 50, 70);
		const float QueueIconScale = 1.15f;

		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";
		public readonly string TooltipContainer;
		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		public string ProductionPaletteWidget = "PRODUCTION_PALETTE";
		public string ProductionTabsWidget = null;
		public string TitleButtonBackground = "sidebar-button";

		public Func<string, bool> IsGroupDisabled;
		public Func<string, Modifiers, bool> TrySelectGroup;

		public readonly int2 CategoryIconSize = new(28, 28);
		public readonly int2 QueueIconSize = new(28, 28);
		public int CategoryColumnX = 7;
		public int QueueStartX = 42;
		public int RowHeight = 40;
		public int TitleHeight = 16;
		public int PriorityButtonSize = 12;
		public int IconPadding = 1;

		public string SidebarCollection = "sidebar";
		public string IconBackgroundImage = "background-iconbg";
		public string TitleFont = "Tiny";
		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";

		[FluentReference(optional: true)]
		public string TitleText = "Building que";

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly OrderManager orderManager;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		readonly Lazy<ProductionTabsWidget> tabsWidget;
		readonly Dictionary<ProductionQueue, Animation> clocks = [];
		readonly List<QueueEntry> entries = [];
		Rectangle eventBounds = Rectangle.Empty;
		string resolvedSidebarCollection;
		string resolvedTitleButtonBackground;
		Sprite iconBackgroundSprite;
		Sprite[] categoryIconSprites;
		string titleText;

		SpriteFont overlayFont;
		SpriteFont titleFont;
		int lastIconIdx;
		int currentTooltipToken;
		int? hoveredMoveUpIndex;
		int? hoveredMoveDownIndex;
		bool moveUpPressed;
		bool moveDownPressed;

		int QueueIconStride => QueueIconSize.X + IconPadding;
		int MoveButtonHeight => Math.Max(1, Math.Min(PriorityButtonSize, Math.Max(1, RowHeight - QueueIconSize.Y)));

		struct QueueBlock
		{
			public string ItemName;
			public ActorInfo Actor;
			public int Count;
			public int StartIndex;
			public ProductionQueue Queue;
			public bool CanMoveUp;
			public bool CanMoveDown;
		}

		struct QueueEntry
		{
			public Rectangle Bounds;
			public Rectangle MoveUpBounds;
			public Rectangle MoveDownBounds;
			public ProductionIcon Icon;
			public QueueBlock Block;
		}

		[ObjectCreator.UseCtor]
		public ProductionQueueOverviewWidget(World world, WorldRenderer worldRenderer, OrderManager orderManager)
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

		protected ProductionQueueOverviewWidget(ProductionQueueOverviewWidget other)
			: base(other)
		{
			world = other.world;
			worldRenderer = other.worldRenderer;
			orderManager = other.orderManager;

			CategoryIconSize = other.CategoryIconSize;
			QueueIconSize = other.QueueIconSize;
			CategoryColumnX = other.CategoryColumnX;
			QueueStartX = other.QueueStartX;
			RowHeight = other.RowHeight;
			TitleHeight = other.TitleHeight;
			PriorityButtonSize = other.PriorityButtonSize;
			IconPadding = other.IconPadding;

			SidebarCollection = other.SidebarCollection;
			IconBackgroundImage = other.IconBackgroundImage;
			TitleFont = other.TitleFont;
			TitleText = other.TitleText;
			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;

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
			titleText = string.IsNullOrEmpty(TitleText) ? "Building que" : FluentProvider.GetMessage(TitleText);

			resolvedSidebarCollection = ResolveSidebarCollection(SidebarCollection);
			iconBackgroundSprite = ChromeProvider.GetImage(resolvedSidebarCollection, IconBackgroundImage);
			resolvedTitleButtonBackground = ResolveTitleButtonBackground(TitleButtonBackground);

			categoryIconSprites = GroupIconNames
				.Select(name => ChromeProvider.TryGetImage("production-icons", name))
				.ToArray();
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

		string ResolveSidebarCollection(string collection)
		{
			var player = world.LocalPlayer;
			if (player == null || player.Spectating)
				return collection;

			if (ChromeMetrics.TryGet("FactionSuffix-" + player.Faction.InternalName, out string faction))
				return collection + "-" + faction;

			return collection + "-" + player.Faction.InternalName;
		}

		public override ProductionQueueOverviewWidget Clone() { return new ProductionQueueOverviewWidget(this); }

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

		ProductionQueue SelectQueueForGroup(string group)
		{
			var queues = QueuesForGroup(group).Where(q => q.Enabled).ToArray();
			if (queues.Length == 0)
				return null;

			var palette = paletteWidget.Value;
			if (palette?.CurrentQueue != null)
			{
				var currentGroup = palette.CurrentQueue.Info.Group ?? palette.CurrentQueue.Info.Type;
				if (currentGroup == group && queues.Contains(palette.CurrentQueue))
					return palette.CurrentQueue;
			}

			return queues.FirstOrDefault(q => q.AllQueued().Any()) ?? queues.First();
		}

		static List<QueueBlock> GetQueueBlocks(ProductionQueue queue)
		{
			var blocks = new List<QueueBlock>();
			if (queue == null)
				return blocks;

			var items = queue.AllQueued().ToList();
			if (items.Count == 0)
				return blocks;

			var index = 0;
			while (index < items.Count)
			{
				var itemName = items[index].Item;
				var startIndex = index;
				var count = 0;
				while (index < items.Count && items[index].Item == itemName)
				{
					count++;
					index++;
				}

				var actor = queue.AllItems().FirstOrDefault(a => a.Name == itemName);
				if (actor == null)
					continue;

				var canMoveUp = startIndex > 1;
				var canMoveDown = startIndex > 0 && startIndex + count < items.Count;

				blocks.Add(new QueueBlock
				{
					ItemName = itemName,
					Actor = actor,
					Count = count,
					StartIndex = startIndex,
					Queue = queue,
					CanMoveUp = canMoveUp,
					CanMoveDown = canMoveDown
				});
			}

			return blocks;
		}

		bool IsGroupAvailable(string group)
		{
			if (IsGroupDisabled != null)
				return !IsGroupDisabled(group);

			return QueuesForGroup(group).Any(q => q.Enabled && q.AllQueued().Any());
		}

		bool SelectProductionGroup(string group, bool reverse)
		{
			if (TrySelectGroup != null)
				return TrySelectGroup(group, reverse ? Modifiers.Shift : Modifiers.None);

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

			var queues = QueuesForGroup(group).Where(q => q.Enabled).ToArray();
			palette.CurrentQueue = queues.FirstOrDefault();
			palette.ScrollToTop();
			palette.PickUpCompletedBuilding();
			return true;
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

		bool HandleMoveBlockUp(QueueBlock block)
		{
			if (!block.CanMoveUp || block.Queue == null)
				return false;

			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickSound, null);
			world.IssueOrder(Order.MoveProductionBlockUp(block.Queue.Actor, block.StartIndex));
			return true;
		}

		bool HandleMoveBlockDown(QueueBlock block)
		{
			if (!block.CanMoveDown || block.Queue == null)
				return false;

			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickSound, null);
			world.IssueOrder(Order.MoveProductionBlockDown(block.Queue.Actor, block.StartIndex));
			return true;
		}

		Rectangle GetCategoryRect(Rectangle rb, int rowIndex)
		{
			return new Rectangle(
				rb.X + CategoryColumnX,
				rb.Y + TitleHeight + rowIndex * RowHeight + (RowHeight - CategoryIconSize.Y) / 2,
				CategoryIconSize.X,
				CategoryIconSize.Y);
		}

		Rectangle GetQueueIconRect(Rectangle rb, int rowIndex, int columnIndex)
		{
			var rowTop = rb.Y + TitleHeight + rowIndex * RowHeight;
			var iconAreaHeight = Math.Max(1, RowHeight - MoveButtonHeight);
			var iconTop = rowTop + Math.Max(0, (iconAreaHeight - QueueIconSize.Y) / 2);

			return new Rectangle(
				rb.X + QueueStartX + columnIndex * QueueIconStride,
				iconTop,
				QueueIconSize.X,
				QueueIconSize.Y);
		}

		Rectangle GetMoveUpButtonRect(Rectangle iconRect)
		{
			var buttonWidth = Math.Max(1, iconRect.Width / 2);
			return new Rectangle(
				iconRect.X,
				iconRect.Bottom,
				buttonWidth,
				MoveButtonHeight);
		}

		Rectangle GetMoveDownButtonRect(Rectangle iconRect)
		{
			var buttonWidth = Math.Max(1, iconRect.Width / 2);
			return new Rectangle(
				iconRect.X + buttonWidth,
				iconRect.Bottom,
				iconRect.Width - buttonWidth,
				MoveButtonHeight);
		}

		void DrawCategoryIcon(Rectangle rect, string group, int rowIndex)
		{
			var disabled = !IsGroupAvailable(group);
			var hover = Ui.MouseOverWidget == this && GetCategoryRect(RenderBounds, rowIndex).Contains(Viewport.LastMousePos);
			var highlighted = GetActiveGroup() == group;
			var pressed = false;

			ButtonWidget.DrawBackground(resolvedTitleButtonBackground, rect, disabled, pressed, hover, highlighted);

			var sprite = categoryIconSprites[rowIndex];
			if (sprite == null)
				return;

			var iconName = GroupIconNames[rowIndex];
			if (disabled)
			{
				var disabledSprite = ChromeProvider.TryGetImage("production-icons", iconName + "-disabled");
				if (disabledSprite != null)
					sprite = disabledSprite;
			}

			const int inset = 6;
			var scale = Math.Min(
				(rect.Width - 2 * inset) / sprite.Size.X,
				(rect.Height - 2 * inset) / sprite.Size.Y);
			var size = new float2(scale * sprite.Size.X, scale * sprite.Size.Y);
			var pos = new float2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
			WidgetUtils.DrawSprite(sprite, pos, size);
		}

		void DrawScaledActorIcon(Sprite sprite, PaletteReference palette, Rectangle rect, int inset = 4, float scaleMultiplier = 1f)
		{
			var scale = Math.Min(
				(rect.Width - 2 * inset) / sprite.Size.X,
				(rect.Height - 2 * inset) / sprite.Size.Y) * scaleMultiplier;
			var center = new float2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
			WidgetUtils.DrawSpriteCentered(sprite, palette, center, scale);
		}

		void DrawQueueIcon(QueueBlock block, Rectangle iconRect, int rowIndex, int columnIndex)
		{
			DrawUniformCell(iconBackgroundSprite, iconRect);

			var player = world.LocalPlayer;
			var faction = player.Faction.InternalName;
			var rsi = block.Actor.TraitInfo<RenderSpritesInfo>();
			var icon = new Animation(world, rsi.GetImage(block.Actor, faction));
			var bi = block.Actor.TraitInfo<BuildableInfo>();
			icon.Play(bi.Icon);

			var paletteName = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + player.InternalName : bi.IconPalette;
			var palette = worldRenderer.Palette(paletteName);
			DrawScaledActorIcon(icon.Image, palette, iconRect, inset: 2, scaleMultiplier: QueueIconScale);

			var queueNumberText = (block.StartIndex + 1).ToString(NumberFormatInfo.CurrentInfo);
			var queueNumberSize = overlayFont.Measure(queueNumberText);
			var queueNumberPos = new float2(
				iconRect.Right - queueNumberSize.X - 1,
				iconRect.Bottom - queueNumberSize.Y - 1);
			overlayFont.DrawTextWithContrast(queueNumberText, queueNumberPos, CountColor, Color.Black, 1);

			var current = block.Queue.CurrentItem();
			var isBuildingNow = block.StartIndex == 0 &&
				current != null &&
				!current.Done &&
				current.Item == block.ItemName;
			if (isBuildingNow)
			{
				if (!clocks.ContainsKey(block.Queue))
					clocks.Add(block.Queue, new Animation(world, ClockAnimation));

				var queueClock = clocks[block.Queue];
				queueClock.PlayFetchIndex(ClockSequence, () => current.TotalTime == 0 ? 0 :
					(current.TotalTime - current.RemainingTime) * (queueClock.CurrentSequence.Length - 1) / current.TotalTime);
				queueClock.Tick();

				var clockPalette = worldRenderer.Palette(ClockPalette);
				var clockCenter = new float2(iconRect.X + iconRect.Width / 2f, iconRect.Y + iconRect.Height / 2f);
				var clockScale = Math.Min(
					(iconRect.Width - 4f) / queueClock.Image.Size.X,
					(iconRect.Height - 4f) / queueClock.Image.Size.Y) * QueueIconScale;
				WidgetUtils.DrawSpriteCentered(queueClock.Image, clockPalette, clockCenter, clockScale);

				var timerText = WidgetUtils.FormatTime(block.Queue.RemainingTimeActual(current), world.Timestep);
				var timerSize = overlayFont.Measure(timerText);
				var timerPos = new float2(
					iconRect.X + (iconRect.Width - timerSize.X) / 2f,
					iconRect.Y + 1);
				overlayFont.DrawTextWithContrast(timerText, timerPos, Color.White, Color.Black, 1);
			}

			var moveUpRect = GetMoveUpButtonRect(iconRect);
			var moveDownRect = GetMoveDownButtonRect(iconRect);
			var moveIndex = entries.Count;

			var moveUpDisabled = !block.CanMoveUp;
			var moveUpHover = hoveredMoveUpIndex == moveIndex && Ui.MouseOverWidget == this;
			var moveUpDepressed = moveUpPressed && moveUpHover;
			var moveUpFill = moveUpDisabled ? PriorityFillDisabled :
				moveUpDepressed ? PriorityFillPressed :
				moveUpHover ? PriorityFillHover : PriorityFill;
			WidgetUtils.FillRectWithColor(moveUpRect, moveUpFill);
			ProductionBarButtonGraphics.TryDrawLeftTriangle(moveUpRect, moveUpRect.Width, int2.Zero, moveUpDisabled);

			var moveDownDisabled = !block.CanMoveDown;
			var moveDownHover = hoveredMoveDownIndex == moveIndex && Ui.MouseOverWidget == this;
			var moveDownDepressed = moveDownPressed && moveDownHover;
			var moveDownFill = moveDownDisabled ? PriorityFillDisabled :
				moveDownDepressed ? PriorityFillPressed :
				moveDownHover ? PriorityFillHover : PriorityFill;
			WidgetUtils.FillRectWithColor(moveDownRect, moveDownFill);
			ProductionBarButtonGraphics.TryDrawRightTriangle(moveDownRect, moveDownRect.Width, int2.Zero, moveDownDisabled);

			var queued = block.Queue.AllQueued().Where(a => a.Item == block.ItemName).ToList();
			var productionIcon = new ProductionIcon
			{
				Actor = block.Actor,
				Name = block.Actor.Name,
				Sprite = icon.Image,
				Palette = palette,
				Pos = new float2(iconRect.X, iconRect.Y),
				Queued = queued,
				ProductionQueue = block.Queue
			};

			entries.Add(new QueueEntry
			{
				Bounds = iconRect,
				MoveUpBounds = moveUpRect,
				MoveDownBounds = moveDownRect,
				Icon = productionIcon,
				Block = block
			});
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

		void DrawBorders(Rectangle rb)
		{
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, rb.Top),
				new int2(rb.Right, rb.Bottom),
				1,
				BorderColor);

			var dividerY = rb.Y + TitleHeight;
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, dividerY),
				new int2(rb.Right, dividerY),
				1,
				BorderColor);
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			moveUpPressed = false;
			moveDownPressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return false;

			var rb = RenderBounds;

			for (var rowIndex = 0; rowIndex < DefaultGroups.Length; rowIndex++)
			{
				var categoryRect = GetCategoryRect(rb, rowIndex);
				if (!categoryRect.Contains(mi.Location))
					continue;

				if (mi.Button != MouseButton.Left)
					return true;

				if (mi.Event == MouseInputEvent.Down)
				{
					TakeMouseFocus(mi);
					if (!IsGroupAvailable(DefaultGroups[rowIndex]))
						Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);
					return true;
				}

				if (mi.Event == MouseInputEvent.Up)
				{
					if (categoryRect.Contains(mi.Location))
					{
						var group = DefaultGroups[rowIndex];
						var reverse = mi.Modifiers.HasModifier(Modifiers.Shift);
						var sound = SelectProductionGroup(group, reverse) ? ClickSound : ClickDisabledSound;
						Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", sound, null);
					}

					if (HasMouseFocus)
						YieldMouseFocus(mi);
					return true;
				}

				return true;
			}

			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				var inMoveUp = entry.MoveUpBounds.Contains(mi.Location);
				var inMoveDown = entry.MoveDownBounds.Contains(mi.Location);
				if (!inMoveUp && !inMoveDown)
					continue;

				if (mi.Event == MouseInputEvent.Move)
				{
					hoveredMoveUpIndex = inMoveUp ? i : null;
					hoveredMoveDownIndex = inMoveDown ? i : null;
				}

				if (mi.Button != MouseButton.Left)
					return true;

				if (mi.Event == MouseInputEvent.Down)
				{
					moveUpPressed = inMoveUp && entry.Block.CanMoveUp;
					moveDownPressed = inMoveDown && entry.Block.CanMoveDown;
					TakeMouseFocus(mi);

					var clickedDisabled = (inMoveUp && !entry.Block.CanMoveUp) || (inMoveDown && !entry.Block.CanMoveDown);
					if (clickedDisabled)
						Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);
					return true;
				}

				if (mi.Event == MouseInputEvent.Up)
				{
					if (moveUpPressed && entry.MoveUpBounds.Contains(mi.Location))
					{
						if (!HandleMoveBlockUp(entry.Block))
							Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);
					}
					else if (moveDownPressed && entry.MoveDownBounds.Contains(mi.Location))
					{
						if (!HandleMoveBlockDown(entry.Block))
							Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);
					}

					moveUpPressed = false;
					moveDownPressed = false;
					if (HasMouseFocus)
						YieldMouseFocus(mi);
					return true;
				}

				return true;
			}

			if (mi.Event == MouseInputEvent.Move)
			{
				hoveredMoveUpIndex = null;
				hoveredMoveDownIndex = null;
			}

			var iconEntry = entries.FirstOrDefault(e => e.Bounds.Contains(mi.Location));
			if (iconEntry.Icon == null)
				return false;

			if (mi.Event == MouseInputEvent.Move)
				TooltipIcon = iconEntry.Icon;

			if (mi.Event != MouseInputEvent.Down)
				return true;

			var first = iconEntry.Icon.Queued.FirstOrDefault();
			if (first != null && PickUpCompletedBuilding(iconEntry.Icon.ProductionQueue, first))
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
			var bounds = new List<Rectangle>();

			DrawUniformCell(iconBackgroundSprite, rb);
			DrawBorders(rb);

			titleFont.DrawTextWithContrast(titleText, new float2(rb.X + 6, rb.Y + 2), Color.White, Color.Black, 1);

			Game.Renderer.EnableAntialiasingFilter();

			for (var rowIndex = 0; rowIndex < DefaultGroups.Length; rowIndex++)
			{
				var group = DefaultGroups[rowIndex];
				var categoryRect = GetCategoryRect(rb, rowIndex);
				DrawCategoryIcon(categoryRect, group, rowIndex);
				bounds.Add(categoryRect);

				var queue = SelectQueueForGroup(group);
				var blocks = GetQueueBlocks(queue);
				var maxColumns = Math.Max(0, (rb.Right - QueueStartX - rb.X) / QueueIconStride);

				for (var columnIndex = 0; columnIndex < blocks.Count && columnIndex < maxColumns; columnIndex++)
				{
					var iconRect = GetQueueIconRect(rb, rowIndex, columnIndex);
					DrawQueueIcon(blocks[columnIndex], iconRect, rowIndex, columnIndex);
					bounds.Add(iconRect);
					bounds.Add(entries[^1].MoveUpBounds);
					bounds.Add(entries[^1].MoveDownBounds);
				}
			}

			Game.Renderer.DisableAntialiasingFilter();

			eventBounds = bounds.Count > 0 ? bounds.Union() : Rectangle.Empty;
		}
	}
}
