#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionIcon
	{
		public ActorInfo Actor;
		public string Name;
		public Hotkey Hotkey;
		public Sprite Sprite;
		public float2 Pos;
		public List<ProductionItem> Queued;
	}

	public class ProductionPaletteWidget : Widget
	{
		public enum ReadyTextStyleOptions { Solid, AlternatingColor, Blinking }
		public readonly ReadyTextStyleOptions ReadyTextStyle = ReadyTextStyleOptions.AlternatingColor;
		public readonly Color ReadyTextAltColor = Color.Gold;
		public readonly int Columns = 3;
		public readonly int2 IconSize = new int2(64, 48);
		public readonly int2 IconMargin = int2.Zero;
		public readonly int2 IconSpriteOffset = int2.Zero;

		public readonly string TabClick = null;
		public readonly string DisabledTabClick = null;
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";

		[Translate] public readonly string ReadyText = "";
		[Translate] public readonly string HoldText = "";

		public int DisplayedIconCount { get; private set; }
		public int TotalIconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => { };

		public ProductionIcon TooltipIcon { get; private set; }
		public readonly World World;
		readonly OrderManager orderManager;

		public int MinimumRows = 4;
		public int MaximumRows = int.MaxValue;

		public int IconRowOffset = 0;
		public int MaxIconRowOffset = int.MaxValue;

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

		public void ScrollDown()
		{
			if (CanScrollDown)
				IconRowOffset++;
		}

		public bool CanScrollDown
		{
			get
			{
				var totalRows = (TotalIconCount + Columns - 1) / Columns;

				return IconRowOffset < totalRows - MaxIconRowOffset;
			}
		}

		public void ScrollUp()
		{
			if (CanScrollUp)
				IconRowOffset--;
		}

		public bool CanScrollUp
		{
			get { return IconRowOffset > 0; }
		}

		public void ScrollToTop()
		{
			IconRowOffset = 0;
		}

		public IEnumerable<ActorInfo> AllBuildables
		{
			get
			{
				if (CurrentQueue == null)
					return Enumerable.Empty<ActorInfo>();

				return CurrentQueue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			}
		}

		public override void Tick()
		{
			TotalIconCount = AllBuildables.Count();

			if (CurrentQueue != null && !CurrentQueue.Actor.IsInWorld)
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
				TooltipIcon = icon;

			if (icon == null)
				return false;

			// Only support left and right clicks
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			// Eat mouse-up events
			if (mi.Event != MouseInputEvent.Down)
				return true;

			return HandleEvent(icon, mi.Button == MouseButton.Left, mi.Modifiers.HasModifier(Modifiers.Shift));
		}

		bool HandleEvent(ProductionIcon icon, bool isLeftClick, bool handleMultiple)
		{
			var actor = World.Map.Rules.Actors[icon.Name];
			var first = icon.Queued.FirstOrDefault();

			if (isLeftClick)
			{
				// Pick up a completed building
				if (first != null && first.Done && actor.Traits.Contains<BuildingInfo>())
				{
					Sound.Play(TabClick);
					World.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue, icon.Name);
				}
				else if (first != null && first.Paused)
				{
					// Resume a paused item
					Sound.Play(TabClick);
					World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, false));
				}
				else if (CurrentQueue.BuildableItems().Any(a => a.Name == icon.Name))
				{
					// Queue a new item
					Sound.Play(TabClick);
					Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.QueuedAudio, World.LocalPlayer.Country.Race);
					World.IssueOrder(Order.StartProduction(CurrentQueue.Actor, icon.Name,
						handleMultiple ? 5 : 1));
				}
				else
					Sound.Play(DisabledTabClick);
			}
			else
			{
				// Hold/Cancel an existing item
				if (first != null)
				{
					Sound.Play(TabClick);

					// instant cancel of things we havent started yet and things that are finished
					if (first.Paused || first.Done || first.TotalCost == first.RemainingCost)
					{
						Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Country.Race);
						World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name,
							handleMultiple ? 5 : 1));
					}
					else
					{
						Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.OnHoldAudio, World.LocalPlayer.Country.Race);
						World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, true));
					}
				}
				else
					Sound.Play(DisabledTabClick);
			}

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up || CurrentQueue == null)
				return false;

			var hotkey = Hotkey.FromKeyInput(e);
			var toBuild = icons.Values.FirstOrDefault(i => i.Hotkey == hotkey);
			return toBuild != null ? HandleEvent(toBuild, true, false) : false;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, ProductionIcon>();
			var producer = CurrentQueue != null ? CurrentQueue.MostLikelyProducer() : default(TraitPair<Production>);
			if (CurrentQueue == null || producer.Trait == null)
			{
				if (DisplayedIconCount != 0)
				{
					OnIconCountChanged(DisplayedIconCount, 0);
					DisplayedIconCount = 0;
				}

				return;
			}

			var oldIconCount = DisplayedIconCount;
			DisplayedIconCount = 0;

			var ks = Game.Settings.Keys;
			var rb = RenderBounds;
			var race = producer.Trait.Race;

			foreach (var item in AllBuildables.Skip(IconRowOffset * Columns).Take(MaxIconRowOffset * Columns))
			{
				var x = DisplayedIconCount % Columns;
				var y = DisplayedIconCount / Columns;
				var rect = new Rectangle(rb.X + x * (IconSize.X + IconMargin.X), rb.Y + y * (IconSize.Y + IconMargin.Y), IconSize.X, IconSize.Y);

				var rsi = item.Traits.Get<RenderSpritesInfo>();
				var icon = new Animation(World, rsi.GetImage(item, World.Map.SequenceProvider, race));
				icon.Play(item.Traits.Get<TooltipInfo>().Icon);

				var pi = new ProductionIcon()
				{
					Actor = item,
					Name = item.Name,
					Hotkey = ks.GetProductionHotkey(DisplayedIconCount),
					Sprite = icon.Image,
					Pos = new float2(rect.Location),
					Queued = CurrentQueue.AllQueued().Where(a => a.Item == item.Name).ToList()
				};

				icons.Add(rect, pi);
				DisplayedIconCount++;
			}

			eventBounds = icons.Any() ? icons.Keys.Aggregate(Rectangle.Union) : Rectangle.Empty;

			if (oldIconCount != DisplayedIconCount)
				OnIconCountChanged(oldIconCount, DisplayedIconCount);
		}

		public override void Draw()
		{
			var iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;

			overlayFont = Game.Renderer.Fonts["TinyBold"];
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0)) / 2;
			queuedOffset = new float2(4, 2);
			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

			if (CurrentQueue == null)
				return;

			var buildableItems = CurrentQueue.BuildableItems();

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
						if (ReadyTextStyle == ReadyTextStyleOptions.Solid || orderManager.LocalFrameNumber / 9 % 2 == 0)
							overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, Color.White, Color.Black, 1);
						else if (ReadyTextStyle == ReadyTextStyleOptions.AlternatingColor)
							overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, ReadyTextAltColor, Color.Black, 1);
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
