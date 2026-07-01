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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionIcon
	{
		public ActorInfo Actor;
		public string Name;
		public HotkeyReference Hotkey;
		public Sprite Sprite;
		public PaletteReference Palette;
		public PaletteReference IconClockPalette;
		public PaletteReference IconDarkenPalette;
		public float2 Pos;
		public List<ProductionItem> Queued;
		public ProductionQueue ProductionQueue;
	}

	public readonly struct ProductionBarButton
	{
		public readonly ProductionIcon Icon;
		public readonly int ButtonIndex;
		public readonly Rectangle Bounds;

		public ProductionBarButton(ProductionIcon icon, int buttonIndex, Rectangle bounds)
		{
			Icon = icon;
			ButtonIndex = buttonIndex;
			Bounds = bounds;
		}
	}

	static class ProductionBarButtonGraphics
	{
		const string UpArrowCollection = "scrollpanel-decorations";
		const string UpArrowImage = "up";
		const string UpArrowDisabledImage = "up-disabled";
		const string LeftArrowImage = "left";
		const string LeftArrowDisabledImage = "left-disabled";
		const string RightArrowImage = "right";
		const string RightArrowDisabledImage = "right-disabled";

		static Sprite upArrowEnabled;
		static Sprite upArrowDisabled;
		static Sprite leftArrowEnabled;
		static Sprite leftArrowDisabled;
		static Sprite rightArrowEnabled;
		static Sprite rightArrowDisabled;

		static Sprite GetUpArrow(bool disabled)
		{
			upArrowEnabled ??= ChromeProvider.TryGetImage(UpArrowCollection, UpArrowImage);
			upArrowDisabled ??= ChromeProvider.TryGetImage(UpArrowCollection, UpArrowDisabledImage);
			return disabled ? upArrowDisabled ?? upArrowEnabled : upArrowEnabled;
		}

		static Sprite GetLeftArrow(bool disabled)
		{
			leftArrowEnabled ??= ChromeProvider.TryGetImage(UpArrowCollection, LeftArrowImage);
			leftArrowDisabled ??= ChromeProvider.TryGetImage(UpArrowCollection, LeftArrowDisabledImage);
			return disabled ? leftArrowDisabled ?? leftArrowEnabled : leftArrowEnabled;
		}

		static Sprite GetRightArrow(bool disabled)
		{
			rightArrowEnabled ??= ChromeProvider.TryGetImage(UpArrowCollection, RightArrowImage);
			rightArrowDisabled ??= ChromeProvider.TryGetImage(UpArrowCollection, RightArrowDisabledImage);
			return disabled ? rightArrowDisabled ?? rightArrowEnabled : rightArrowEnabled;
		}

		public static int2 GetTextPosition(string text, SpriteFont font, Rectangle rb, int usableWidth)
		{
			var textSize = font.Measure(text);
			var y = rb.Y + (rb.Height - textSize.Y - font.TopOffset) / 2;
			return new int2(rb.X + (usableWidth - textSize.X) / 2, y);
		}

		public static void DrawButtonText(string text, SpriteFont font, Rectangle rb, int usableWidth, int2 stateOffset,
			Color color, Color disabledColor, Color bgDark, Color bgLight, bool disabled, bool contrast, bool shadow, int contrastRadius)
		{
			var position = GetTextPosition(text, font, rb, usableWidth);
			if (contrast)
				font.DrawTextWithContrast(text, position + stateOffset,
					disabled ? disabledColor : color, bgDark, bgLight, contrastRadius);
			else if (shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position + stateOffset, disabled ? disabledColor : color);
		}

		public static void DrawMinus(SpriteFont font, Rectangle rb, int usableWidth, int2 stateOffset,
			Color color, Color disabledColor, Color contrastColor, bool disabled, bool contrast, int contrastRadius)
		{
			var refSize = font.Measure("1");
			var thickness = Math.Max(1f, refSize.Y * 0.16f);
			var lineWidth = Math.Min(usableWidth - 4, refSize.X * 0.75f);
			var centerY = rb.Y + rb.Height / 2f + stateOffset.Y;
			var centerX = rb.X + usableWidth / 2f + stateOffset.X;
			var drawColor = disabled ? disabledColor : color;
			var cr = Game.Renderer.RgbaColorRenderer;
			var a = new float3(centerX - lineWidth / 2f, centerY, 0);
			var b = new float3(centerX + lineWidth / 2f, centerY, 0);

			if (contrast)
				cr.DrawLine(a, b, thickness + 2 * contrastRadius, contrastColor);

			cr.DrawLine(a, b, thickness, drawColor);
		}

		public static bool TryDrawUpTriangle(Rectangle rb, int usableWidth, int2 stateOffset, bool disabled)
		{
			var sprite = GetUpArrow(disabled);
			if (sprite == null)
				return false;

			const int padding = 1;
			var maxWidth = usableWidth - 2 * padding;
			var maxHeight = rb.Height - 2 * padding;
			var scale = Math.Min(maxWidth / sprite.Size.X, maxHeight / sprite.Size.Y);
			var center = new float2(
				rb.X + usableWidth / 2f + stateOffset.X,
				rb.Y + rb.Height / 2f + stateOffset.Y);
			DrawChromeSpriteCentered(sprite, center, scale);
			return true;
		}

		public static bool TryDrawLeftTriangle(Rectangle rb, int usableWidth, int2 stateOffset, bool disabled)
		{
			var sprite = GetLeftArrow(disabled);
			if (sprite == null)
				return false;

			const int padding = 1;
			var maxWidth = usableWidth - 2 * padding;
			var maxHeight = rb.Height - 2 * padding;
			var scale = Math.Min(maxWidth / sprite.Size.X, maxHeight / sprite.Size.Y);
			var center = new float2(
				rb.X + usableWidth / 2f + stateOffset.X,
				rb.Y + rb.Height / 2f + stateOffset.Y);
			DrawChromeSpriteCentered(sprite, center, scale);
			return true;
		}

		public static bool TryDrawRightTriangle(Rectangle rb, int usableWidth, int2 stateOffset, bool disabled)
		{
			var sprite = GetRightArrow(disabled);
			if (sprite == null)
				return false;

			const int padding = 1;
			var maxWidth = usableWidth - 2 * padding;
			var maxHeight = rb.Height - 2 * padding;
			var scale = Math.Min(maxWidth / sprite.Size.X, maxHeight / sprite.Size.Y);
			var center = new float2(
				rb.X + usableWidth / 2f + stateOffset.X,
				rb.Y + rb.Height / 2f + stateOffset.Y);
			DrawChromeSpriteCentered(sprite, center, scale);
			return true;
		}

		static void DrawChromeSpriteCentered(Sprite s, float2 center, float scale)
		{
			var size = new float2(scale * s.Size.X, scale * s.Size.Y);
			WidgetUtils.DrawSprite(s, center - 0.5f * size, size);
		}
	}

	public class ProductionBarCancelButtonWidget : ButtonWidget
	{
		static readonly Color Fill = Color.FromArgb(255, 210, 25, 25);
		static readonly Color FillHover = Color.FromArgb(255, 255, 45, 45);
		static readonly Color FillDisabled = Color.FromArgb(255, 90, 35, 35);

		[ObjectCreator.UseCtor]
		public ProductionBarCancelButtonWidget(ModData modData)
			: base(modData)
		{
			VisualHeight = 0;
			Text = "X";
			Font = "TinyBold";
			TextColor = Color.White;
			Contrast = true;
			ContrastRadius = 1;
		}

		protected ProductionBarCancelButtonWidget(ProductionBarCancelButtonWidget other)
			: base(other) { }

		public override ProductionBarCancelButtonWidget Clone() { return new ProductionBarCancelButtonWidget(this); }

		public override void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			var fill = disabled ? FillDisabled : pressed || hover ? FillHover : Fill;
			WidgetUtils.FillRectWithColor(rect, fill);
		}
	}

	public class ProductionBarPriorityButtonWidget : ButtonWidget
	{
		static readonly Color Fill = Color.FromArgb(255, 25, 160, 25);
		static readonly Color FillHover = Color.FromArgb(255, 40, 200, 40);
		static readonly Color FillDisabled = Color.FromArgb(255, 50, 50, 70);

		[ObjectCreator.UseCtor]
		public ProductionBarPriorityButtonWidget(ModData modData)
			: base(modData)
		{
			VisualHeight = 0;
			Text = "1";
			Font = "TinyBold";
			TextColor = Color.White;
			Contrast = true;
			ContrastRadius = 1;
		}

		protected ProductionBarPriorityButtonWidget(ProductionBarPriorityButtonWidget other)
			: base(other) { }

		public override ProductionBarPriorityButtonWidget Clone() { return new ProductionBarPriorityButtonWidget(this); }

		public override void Draw()
		{
			var rb = RenderBounds;
			var disabled = IsDisabled();
			var highlighted = IsHighlighted();
			var font = Game.Renderer.Fonts[Font];
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
			var stateOffset = Depressed ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			var hover = Ui.MouseOverWidget == this || Children.FirstOrDefault(c => c == Ui.MouseOverWidget) != null;

			DrawBackground(rb, disabled, Depressed, hover, highlighted);

			if (!ProductionBarButtonGraphics.TryDrawUpTriangle(rb, UsableWidth, stateOffset, disabled))
				ProductionBarButtonGraphics.DrawButtonText(Text, font, rb, UsableWidth, stateOffset,
					color, colordisabled, bgDark, bgLight, disabled, Contrast, Shadow, ContrastRadius);
		}

		public override void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			if (highlighted)
			{
				var fill = disabled ? Fill : pressed || hover ? FillHover : Fill;
				WidgetUtils.FillRectWithColor(rect, fill);
				return;
			}

			if (!string.IsNullOrEmpty(Background))
				ButtonWidget.DrawBackground(Background, rect, disabled, pressed, hover, false);
			else
				WidgetUtils.FillRectWithColor(rect, disabled ? FillDisabled : Color.FromArgb(255, 35, 35, 55));
		}
	}

	public class ProductionBarBulkButtonWidget : ButtonWidget
	{
		const int MaxDigits = 3;

		static readonly Color FillEditing = Color.FromArgb(255, 20, 40, 110);
		static readonly Color FillEditingHover = Color.FromArgb(255, 30, 55, 140);
		static readonly Color FillActive = Color.FromArgb(255, 50, 120, 230);
		static readonly Color FillActiveHover = Color.FromArgb(255, 70, 145, 255);
		static readonly Color FillDisabled = Color.FromArgb(255, 50, 50, 70);

		bool editing;
		string editText = "";
		string boundIconName;
		uint? boundQueueActorId;

		public Func<int> GetTargetCount;
		public Action<int> OnTargetConfirmed;

		[ObjectCreator.UseCtor]
		public ProductionBarBulkButtonWidget(ModData modData)
			: base(modData)
		{
			VisualHeight = 0;
			Font = "TinyBold";
			TextColor = Color.White;
			Contrast = true;
			ContrastRadius = 1;
			GetText = () => editing ? editText : GetDisplayText();
		}

		protected ProductionBarBulkButtonWidget(ProductionBarBulkButtonWidget other)
			: base(other)
		{
			GetTargetCount = other.GetTargetCount;
			OnTargetConfirmed = other.OnTargetConfirmed;
		}

		public override ProductionBarBulkButtonWidget Clone() { return new ProductionBarBulkButtonWidget(this); }

		public override void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			if (disabled)
			{
				WidgetUtils.FillRectWithColor(rect, FillDisabled);
				return;
			}

			if (editing)
			{
				WidgetUtils.FillRectWithColor(rect, pressed || hover ? FillEditingHover : FillEditing);
				return;
			}

			if ((GetTargetCount?.Invoke() ?? 0) > 0)
			{
				WidgetUtils.FillRectWithColor(rect, pressed || hover ? FillActiveHover : FillActive);
				return;
			}

			if (!string.IsNullOrEmpty(Background))
				ButtonWidget.DrawBackground(Background, rect, disabled, pressed, hover, false);
			else
				WidgetUtils.FillRectWithColor(rect, Color.FromArgb(255, 35, 35, 55));
		}

		string GetDisplayText()
		{
			var count = GetTargetCount?.Invoke() ?? 0;
			return count > 0 ? count.ToString(NumberFormatInfo.CurrentInfo) : "";
		}

		bool ShouldDrawIdleIcon()
		{
			if (editing)
				return false;

			return (GetTargetCount?.Invoke() ?? 0) <= 0;
		}

		public void SyncBinding(string iconName, uint queueActorId)
		{
			if (boundQueueActorId.HasValue &&
				(boundIconName != iconName || boundQueueActorId != queueActorId))
				CancelEditingWithoutConfirm();

			boundIconName = iconName;
			boundQueueActorId = queueActorId;
		}

		void CancelEditingWithoutConfirm()
		{
			if (!editing)
				return;

			editing = false;
			editText = "";

			if (Ui.KeyboardFocusWidget == this)
				base.YieldKeyboardFocus();
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			var disabled = IsDisabled();
			var highlighted = IsHighlighted();
			var font = Game.Renderer.Fonts[Font];
			var text = GetText();
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
			var stateOffset = Depressed ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			var hover = Ui.MouseOverWidget == this || Children.FirstOrDefault(c => c == Ui.MouseOverWidget) != null;

			DrawBackground(rb, disabled, Depressed, hover, highlighted);

			if (!string.IsNullOrEmpty(text))
			{
				ProductionBarButtonGraphics.DrawButtonText(text, font, rb, UsableWidth, stateOffset,
					color, colordisabled, bgDark, bgLight, disabled, Contrast, Shadow, ContrastRadius);
			}
			else if (ShouldDrawIdleIcon())
			{
				ProductionBarButtonGraphics.DrawMinus(font, rb, UsableWidth, stateOffset,
					color, colordisabled, bgDark, disabled, Contrast, ContrastRadius);
			}
		}

		void BeginEditing()
		{
			editing = true;
			editText = "";
			TakeKeyboardFocus();
		}

		void CancelEditing()
		{
			editing = false;
			editText = "";
		}

		void ConfirmEditing()
		{
			if (editing && editText.Length > 0 && int.TryParse(editText, NumberStyles.None, NumberFormatInfo.CurrentInfo, out var count))
				OnTargetConfirmed?.Invoke(Math.Min(count, 999));

			CancelEditing();
			base.YieldKeyboardFocus();
		}

		static bool TryGetDigit(Keycode key, out char digit)
		{
			if (key >= Keycode.NUMBER_0 && key <= Keycode.NUMBER_9)
			{
				digit = (char)key;
				return true;
			}

			if (key >= Keycode.KP_0 && key <= Keycode.KP_9)
			{
				digit = (char)('0' + (key - Keycode.KP_0));
				return true;
			}

			digit = default;
			return false;
		}

		public override bool YieldKeyboardFocus()
		{
			if (editing)
			{
				if (editText.Length > 0 && int.TryParse(editText, NumberStyles.None, NumberFormatInfo.CurrentInfo, out var count))
					OnTargetConfirmed?.Invoke(Math.Min(count, 999));

				CancelEditing();
			}

			return base.YieldKeyboardFocus();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!editing || IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			switch (e.Key)
			{
				case Keycode.RETURN:
				case Keycode.KP_ENTER:
					ConfirmEditing();
					return true;
				case Keycode.ESCAPE:
					CancelEditing();
					base.YieldKeyboardFocus();
					return true;
				case Keycode.BACKSPACE:
				case Keycode.KP_BACKSPACE:
					if (editText.Length > 0)
						editText = editText[..^1];
					return true;
				default:
					if (TryGetDigit(e.Key, out var digit) && editText.Length < MaxDigits)
						editText += digit;
					return true;
			}
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IsDisabled())
				return false;

			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left)
			{
				BeginEditing();
				return true;
			}

			return editing;
		}
	}

	public class ProductionPaletteWidget : Widget
	{
		public enum ReadyTextStyleOptions { Solid, AlternatingColor, Blinking }
		public readonly ReadyTextStyleOptions ReadyTextStyle = ReadyTextStyleOptions.AlternatingColor;
		public readonly Color TextColor = Color.White;
		public readonly Color ReadyTextAltColor = Color.Gold;
		public readonly int Columns = 3;
		public readonly int2 IconSize = new(64, 48);
		public readonly int2 IconMargin = int2.Zero;
		public readonly int2 IconSpriteOffset = int2.Zero;
		public readonly int BarHeight = 0;
		public readonly int ButtonsPerBar = 4;
		public readonly string BarButtonBackground = null;
		public readonly int BarPriorityButtonIndex = 0;
		public readonly int BarBulkButtonIndex = 1;
		public readonly int BarCancelButtonIndex = 3;

		public readonly float2 QueuedOffset = new(4, 2);

		public readonly float2 BulkOffset = new(4, 2);
		public readonly TextAlign QueuedTextAlign = TextAlign.Left;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";

		// Note: LinterHotkeyNames assumes that these are disabled by default
		public readonly string HotkeyPrefix = null;
		public readonly int HotkeyCount = 0;
		public readonly HotkeyReference SelectProductionBuildingHotkey = new();

		public readonly string ClockAnimation = "clock";
		public readonly string ClockSequence = "idle";
		public readonly string ClockPalette = "chrome";

		public readonly string NotBuildableAnimation = "clock";
		public readonly string NotBuildableSequence = "idle";
		public readonly string NotBuildablePalette = "chrome";

		public readonly string OverlayFont = "TinyBold";
		public readonly string SymbolsFont = "Symbols";

		public readonly bool DrawTime = true;

		[FluentReference]
		public string ReadyText = "";

		[FluentReference]
		public string HoldText = "";

		public readonly string InfiniteSymbol = "\u221E";

		public int DisplayedIconCount { get; private set; }
		public int TotalIconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => { };

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;
		public readonly World World;
		readonly ModData modData;
		readonly OrderManager orderManager;

		public int MinimumRows = 4;
		public int MaximumRows = int.MaxValue;

		public int IconRowOffset = 0;
		public int MaxIconRowOffset = int.MaxValue;

		public int RowStride => IconSize.Y + IconMargin.Y + BarHeight;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		ProductionQueue currentQueue;
		HotkeyReference[] hotkeys;

		public ProductionQueue CurrentQueue
		{
			get => currentQueue;
			set
			{
				currentQueue = value;
				if (currentQueue != null)
					UpdateCachedProductionIconOverlays();

				RefreshIcons();
			}
		}

		public override Rectangle EventBounds => eventBounds;
		Dictionary<Rectangle, ProductionIcon> icons = [];
		List<ProductionBarButton> barButtons = [];
		string resolvedBarButtonBackground;
		Animation cantBuild;
		Animation clock;
		Rectangle eventBounds = Rectangle.Empty;

		readonly WorldRenderer worldRenderer;

		SpriteFont overlayFont, symbolFont;
		float2 iconOffset, holdOffset, readyOffset, timeOffset, infiniteOffset;

		Player cachedQueueOwner;
		IProductionIconOverlay[] pios;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError)
		{
			var prefix = "";
			var prefixNode = widgetNode.Value.NodeWithKeyOrDefault("HotkeyPrefix");
			if (prefixNode != null)
				prefix = prefixNode.Value.Value;

			var count = 0;
			var countNode = widgetNode.Value.NodeWithKeyOrDefault("HotkeyCount");
			if (countNode != null)
				count = FieldLoader.GetValue<int>("HotkeyCount", countNode.Value.Value);

			if (count == 0)
				return [];

			if (string.IsNullOrEmpty(prefix))
				emitError($"{widgetNode.Location} must define HotkeyPrefix if HotkeyCount > 0.");

			return Exts.MakeArray(count, i => prefix + (i + 1).ToStringInvariant("D2"));
		}

		[ObjectCreator.UseCtor]
		public ProductionPaletteWidget(ModData modData, OrderManager orderManager, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.orderManager = orderManager;
			World = world;
			this.worldRenderer = worldRenderer;
			GetTooltipIcon = () => TooltipIcon;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			clock = new Animation(World, ClockAnimation);
			cantBuild = new Animation(World, NotBuildableAnimation);
			cantBuild.PlayFetchIndex(NotBuildableSequence, () => 0);
			hotkeys = Exts.MakeArray(HotkeyCount,
				i => modData.Hotkeys[HotkeyPrefix + (i + 1).ToStringInvariant("D2")]);

			overlayFont = Game.Renderer.Fonts[OverlayFont];
			Game.Renderer.Fonts.TryGetValue(SymbolsFont, out symbolFont);

			iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;
			HoldText = FluentProvider.GetMessage(HoldText);
			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			ReadyText = FluentProvider.GetMessage(ReadyText);
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

			if (ChromeMetrics.TryGet("InfiniteOffset", out infiniteOffset))
				infiniteOffset += QueuedOffset;
			else
				infiniteOffset = QueuedOffset;

			resolvedBarButtonBackground = ResolveBarButtonBackground(BarButtonBackground);
		}

		string ResolveBarButtonBackground(string background)
		{
			if (string.IsNullOrEmpty(background))
				return null;

			var player = World.LocalPlayer;
			if (player == null || player.Spectating)
				return background;

			if (ChromeMetrics.TryGet("FactionSuffix-" + player.Faction.InternalName, out string faction))
				return background + "-" + faction;

			return background + "-" + player.Faction.InternalName;
		}

		bool IsBarCancelButtonDisabled(ProductionIcon icon) => icon.Queued.Count == 0;

		bool QueueSupportsPriority =>
			CurrentQueue is not ClassicParallelProductionQueue &&
			CurrentQueue is not ParallelProductionQueue;

		ProductionItem GetNextQueuedItem()
		{
			if (CurrentQueue == null)
				return null;

			return CurrentQueue.AllQueued().Skip(1).FirstOrDefault();
		}

		bool IsNextInLine(ProductionIcon icon)
		{
			var next = GetNextQueuedItem();
			return next != null && next.Item == icon.Name;
		}

		bool IsBarPriorityButtonDisabled(ProductionIcon icon)
		{
			if (CurrentQueue == null || !QueueSupportsPriority)
				return true;

			if (IsNextInLine(icon))
				return false;

			if (icon.Queued.Count > 0)
				return false;

			return CurrentQueue.BuildableItems().All(a => a.Name != icon.Name);
		}

		bool HandleCancelAllQueued(ProductionIcon icon)
		{
			if (CurrentQueue == null || IsBarCancelButtonDisabled(icon))
				return false;

			var cancelCount = icon.Queued.Count;
			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(World.LocalPlayer, CurrentQueue.Info.CancelledTextNotification);

			World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, cancelCount));
			World.IssueOrder(Order.SetProductionTarget(CurrentQueue.Actor, icon.Name, 0));
			return true;
		}

		bool HandleSetProductionTarget(ProductionIcon icon, int count)
		{
			if (CurrentQueue == null)
				return false;

			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
			World.IssueOrder(Order.SetProductionTarget(CurrentQueue.Actor, icon.Name, count));
			return true;
		}

		bool IsBarBulkButtonDisabled(ProductionIcon icon)
		{
			if (CurrentQueue == null)
				return true;

			return CurrentQueue.BuildableItems().All(a => a.Name != icon.Name);
		}

		bool HandlePrioritizeProduction(ProductionIcon icon)
		{
			if (CurrentQueue == null || !QueueSupportsPriority)
				return false;

			if (IsNextInLine(icon))
				return true;

			if (IsBarPriorityButtonDisabled(icon))
				return false;

			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);

			var hasQueuedWaiting = icon.Queued.Any(i => !CurrentQueue.IsProducing(i));
			if (hasQueuedWaiting)
			{
				World.IssueOrder(Order.PrioritizeProduction(CurrentQueue.Actor, icon.Name));
				return true;
			}

			var buildable = CurrentQueue.BuildableItems().FirstOrDefault(a => a.Name == icon.Name);
			if (buildable == null)
				return false;

			if (CurrentQueue.Info.PayUpFront &&
				currentQueue.GetProductionCost(buildable) > CurrentQueue.Actor.Owner.PlayerActor.Trait<PlayerResources>().GetCashAndResources())
				return false;

			if (!CurrentQueue.CanQueue(buildable, out _, out _))
				return false;

			World.IssueOrder(Order.StartProduction(CurrentQueue.Actor, icon.Name, 1, queued: false));
			return true;
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

		public bool CanScrollUp => IconRowOffset > 0;

		public void ScrollToTop()
		{
			IconRowOffset = 0;
		}

		public IEnumerable<ActorInfo> AllBuildables
		{
			get
			{
				if (CurrentQueue == null)
					return [];

				return CurrentQueue.AllItems().OrderBy(a => a.TraitInfo<BuildableInfo>().BuildPaletteOrder);
			}
		}

		public override void Tick()
		{
			TotalIconCount = AllBuildables.Count();

			if (CurrentQueue != null && !CurrentQueue.Actor.IsInWorld)
				CurrentQueue = null;

			if (CurrentQueue != null)
			{
				if (CurrentQueue.Actor.Owner != cachedQueueOwner)
					UpdateCachedProductionIconOverlays();

				RefreshIcons();
			}
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate,
					new WidgetArgs() { { "player", World.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", World } });
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

			if (mi.Event == MouseInputEvent.Scroll)
			{
				if (mi.Delta.Y < 0 && CanScrollDown)
				{
					ScrollDown();
					Ui.ResetTooltips();
					Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				}
				else if (mi.Delta.Y > 0 && CanScrollUp)
				{
					ScrollUp();
					Ui.ResetTooltips();
					Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				}
			}

			if (icon == null)
				return false;

			// Eat mouse-up events
			if (mi.Event != MouseInputEvent.Down)
				return true;

			return HandleEvent(icon, mi.Button, mi.Modifiers);
		}

		protected bool PickUpCompletedBuildingIcon(ProductionItem item)
		{
			if (item == null)
				return false;

			var actor = World.Map.Rules.Actors[item.Item];

			if (item.Done && actor.HasTraitInfo<BuildingInfo>())
			{
				World.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue, item.Item, worldRenderer);
				return true;
			}

			return false;
		}

		public void PickUpCompletedBuilding()
		{
			PickUpCompletedBuildingIcon(CurrentQueue.CurrentItem());
		}

		bool HandleLeftClick(ProductionItem item, ProductionIcon icon, int handleCount, Modifiers modifiers)
		{
			if (PickUpCompletedBuildingIcon(item))
			{
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				return true;
			}

			if (item != null && item.Paused)
			{
				// Resume a paused item
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.QueuedAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(World.LocalPlayer, CurrentQueue.Info.QueuedTextNotification);

				World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, false));
				return true;
			}

			var buildable = CurrentQueue.BuildableItems().FirstOrDefault(a => a.Name == icon.Name);

			if (buildable != null)
			{
				if (CurrentQueue.Info.PayUpFront &&
					currentQueue.GetProductionCost(buildable) > CurrentQueue.Actor.Owner.PlayerActor.Trait<PlayerResources>().GetCashAndResources())
					return false;
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);

				// Queue a new item
				var canQueue = CurrentQueue.CanQueue(buildable, out var notification, out var textNotification);

				if (!CurrentQueue.AllQueued().Any())
				{
					Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", notification, World.LocalPlayer.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(World.LocalPlayer, textNotification);
				}

				if (canQueue)
				{
					var queued = !modifiers.HasModifier(Modifiers.Ctrl);
					World.IssueOrder(Order.StartProduction(CurrentQueue.Actor, icon.Name, handleCount, queued));
					return true;
				}
			}

			return false;
		}

		bool HandleRightClick(ProductionItem item, ProductionIcon icon, int handleCount)
		{
			if (CurrentQueue is BulkProductionQueue bulkProductionQueue && !bulkProductionQueue.HasDeliveryStarted())
			{
				var readyActors = bulkProductionQueue.GetActorsReadyForDelivery();
				if (readyActors.Any(a => a.Actor.Name == icon.Name))
				{
					World.IssueOrder(
						new Order("ReturnOrder", CurrentQueue.Actor, false)
						{
							ExtraData = (uint)handleCount,
							TargetString = icon.Name
						});
					Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
					return true;
				}
				else
					return false;
			}

			if (item == null)
				return false;

			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);

			if (CurrentQueue.Info.DisallowPaused || item.Paused || item.Done || item.TotalCost == item.RemainingCost || !item.Started)
			{
				// Instantly cancel items that haven't started, have finished, or if the queue doesn't support pausing
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(World.LocalPlayer, CurrentQueue.Info.CancelledTextNotification);

				World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));
			}
			else
			{
				// Pause an existing item
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.OnHoldAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(World.LocalPlayer, CurrentQueue.Info.OnHoldTextNotification);

				World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, true));
			}

			return true;
		}

		bool HandleMiddleClick(ProductionItem item, ProductionIcon icon, int handleCount)
		{
			if (item != null)
			{
				// Directly cancel, skipping "on-hold"
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(World.LocalPlayer, CurrentQueue.Info.CancelledTextNotification);

				World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));
				return true;
			}

			return false;
		}

		bool HandleEvent(ProductionIcon icon, MouseButton btn, Modifiers modifiers)
		{
			var startCount = modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

			// PERF: avoid an unnecessary enumeration by casting back to its known type
			var cancelCount = modifiers.HasModifier(Modifiers.Ctrl) ? ((List<ProductionItem>)CurrentQueue.AllQueued()).Count : startCount;
			var item = icon.Queued.FirstOrDefault();
			var handled = btn == MouseButton.Left ? HandleLeftClick(item, icon, startCount, modifiers)
				: btn == MouseButton.Right ? HandleRightClick(item, icon, cancelCount)
				: btn == MouseButton.Middle && HandleMiddleClick(item, icon, cancelCount);

			if (!handled)
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickDisabledSound, null);

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up || CurrentQueue == null)
				return false;

			if (SelectProductionBuildingHotkey.IsActivatedBy(e))
				return SelectProductionBuilding();

			var batchModifiers = e.Modifiers.HasModifier(Modifiers.Shift) ? Modifiers.Shift : Modifiers.None;

			// HACK: enable production if the shift key is pressed
			e.Modifiers &= ~Modifiers.Shift;
			var toBuild = icons.Values.FirstOrDefault(i => i.Hotkey != null && i.Hotkey.IsActivatedBy(e));
			return toBuild != null && HandleEvent(toBuild, MouseButton.Left, batchModifiers);
		}

		bool SelectProductionBuilding()
		{
			var viewport = worldRenderer.Viewport;
			var selection = World.Selection;

			if (CurrentQueue == null)
				return true;

			var facility = CurrentQueue.MostLikelyProducer().Actor;

			if (facility == null || facility.OccupiesSpace == null)
				return true;

			if (selection.Actors.Count == 1 && selection.Contains(facility))
				viewport.Center(selection.Actors);
			else
				selection.Combine(World, [facility], false, true);

			Game.Sound.PlayNotification(World.Map.Rules, null, "Sounds", ClickSound, null);
			return true;
		}

		void UpdateCachedProductionIconOverlays()
		{
			cachedQueueOwner = CurrentQueue.Actor.Owner;
			pios = cachedQueueOwner.PlayerActor.TraitsImplementing<IProductionIconOverlay>().ToArray();
		}

		void AddBarButtons(ProductionIcon pi, Rectangle iconRect)
		{
			if (BarHeight <= 0 || ButtonsPerBar <= 0)
				return;

			var barY = iconRect.Y + IconSize.Y;
			var buttonWidth = IconSize.X / ButtonsPerBar;
			for (var i = 0; i < ButtonsPerBar; i++)
			{
				if (i == BarCancelButtonIndex || i == BarPriorityButtonIndex || i == BarBulkButtonIndex)
					continue;

				var buttonRect = new Rectangle(iconRect.X + i * buttonWidth, barY, buttonWidth, BarHeight);
				barButtons.Add(new ProductionBarButton(pi, i, buttonRect));
			}
		}

		void SyncPriorityButtonWidgets()
		{
			var priorityButtons = Children.OfType<ProductionBarPriorityButtonWidget>().ToList();

			if (BarHeight <= 0 || ButtonsPerBar <= 0 || icons.Count == 0)
			{
				foreach (var btn in priorityButtons)
					RemoveChild(btn);

				return;
			}

			var iconRects = icons.Keys.ToList();
			var buttonWidth = IconSize.X / ButtonsPerBar;
			var ro = RenderOrigin;

			while (priorityButtons.Count > iconRects.Count)
			{
				RemoveChild(priorityButtons[^1]);
				priorityButtons.RemoveAt(priorityButtons.Count - 1);
			}

			for (var i = 0; i < iconRects.Count; i++)
			{
				var iconRect = iconRects[i];
				var pi = icons[iconRect];

				ProductionBarPriorityButtonWidget btn;
				if (i >= priorityButtons.Count)
				{
					btn = new ProductionBarPriorityButtonWidget(modData);
					AddChild(btn);
				}
				else
					btn = priorityButtons[i];

				btn.Background = resolvedBarButtonBackground;
				btn.Bounds = new WidgetBounds(
					iconRect.X - ro.X + BarPriorityButtonIndex * buttonWidth,
					iconRect.Y - ro.Y + IconSize.Y,
					buttonWidth,
					BarHeight);
				btn.OnClick = () => HandlePrioritizeProduction(pi);
				btn.IsDisabled = () => IsBarPriorityButtonDisabled(pi);
				btn.IsHighlighted = () => IsNextInLine(pi);
			}
		}

		void SyncBulkButtonWidgets()
		{
			var bulkButtons = Children.OfType<ProductionBarBulkButtonWidget>().ToList();

			if (BarHeight <= 0 || ButtonsPerBar <= 0 || icons.Count == 0)
			{
				foreach (var btn in bulkButtons)
					RemoveChild(btn);

				return;
			}

			var iconRects = icons.Keys.ToList();
			var buttonWidth = IconSize.X / ButtonsPerBar;
			var ro = RenderOrigin;

			while (bulkButtons.Count > iconRects.Count)
			{
				RemoveChild(bulkButtons[^1]);
				bulkButtons.RemoveAt(bulkButtons.Count - 1);
			}

			for (var i = 0; i < iconRects.Count; i++)
			{
				var iconRect = iconRects[i];
				var pi = icons[iconRect];

				ProductionBarBulkButtonWidget btn;
				if (i >= bulkButtons.Count)
				{
					btn = new ProductionBarBulkButtonWidget(modData);
					AddChild(btn);
				}
				else
					btn = bulkButtons[i];

				btn.Background = resolvedBarButtonBackground;
				btn.Bounds = new WidgetBounds(
					iconRect.X - ro.X + BarBulkButtonIndex * buttonWidth,
					iconRect.Y - ro.Y + IconSize.Y,
					buttonWidth,
					BarHeight);
				btn.GetTargetCount = () => pi.ProductionQueue?.GetProductionTarget(pi.Name) ?? 0;
				btn.OnTargetConfirmed = count => HandleSetProductionTarget(pi, count);
				btn.IsDisabled = () => IsBarBulkButtonDisabled(pi);
				btn.SyncBinding(pi.Name, pi.ProductionQueue.Actor.ActorID);
			}
		}

		void SyncCancelButtonWidgets()
		{
			var cancelButtons = Children.OfType<ProductionBarCancelButtonWidget>().ToList();

			if (BarHeight <= 0 || ButtonsPerBar <= 0 || icons.Count == 0)
			{
				foreach (var btn in cancelButtons)
					RemoveChild(btn);

				return;
			}

			var iconRects = icons.Keys.ToList();
			var buttonWidth = IconSize.X / ButtonsPerBar;
			var ro = RenderOrigin;

			while (cancelButtons.Count > iconRects.Count)
			{
				RemoveChild(cancelButtons[^1]);
				cancelButtons.RemoveAt(cancelButtons.Count - 1);
			}

			for (var i = 0; i < iconRects.Count; i++)
			{
				var iconRect = iconRects[i];
				var pi = icons[iconRect];

				ProductionBarCancelButtonWidget btn;
				if (i >= cancelButtons.Count)
				{
					btn = new ProductionBarCancelButtonWidget(modData);
					AddChild(btn);
				}
				else
					btn = cancelButtons[i];

				btn.Bounds = new WidgetBounds(
					iconRect.X - ro.X + BarCancelButtonIndex * buttonWidth,
					iconRect.Y - ro.Y + IconSize.Y,
					buttonWidth,
					BarHeight);
				btn.OnClick = () => HandleCancelAllQueued(pi);
				btn.IsDisabled = () => IsBarCancelButtonDisabled(pi);
			}
		}

		public void RefreshIcons()
		{
			icons = [];
			barButtons = [];
			var producer = CurrentQueue != null ? CurrentQueue.MostLikelyProducer() : default;
			if (CurrentQueue == null || producer.Trait == null)
			{
				foreach (var btn in Children.OfType<ProductionBarCancelButtonWidget>().ToList())
					RemoveChild(btn);
				foreach (var btn in Children.OfType<ProductionBarPriorityButtonWidget>().ToList())
					RemoveChild(btn);
				foreach (var btn in Children.OfType<ProductionBarBulkButtonWidget>().ToList())
					RemoveChild(btn);

				if (DisplayedIconCount != 0)
				{
					OnIconCountChanged(DisplayedIconCount, 0);
					DisplayedIconCount = 0;
				}

				return;
			}

			var oldIconCount = DisplayedIconCount;
			DisplayedIconCount = 0;

			var rb = RenderBounds;
			var faction = producer.Trait.Faction;

			foreach (var item in AllBuildables.Skip(IconRowOffset * Columns).Take(MaxIconRowOffset * Columns))
			{
				var x = DisplayedIconCount % Columns;
				var y = DisplayedIconCount / Columns;
				var rect = new Rectangle(rb.X + x * (IconSize.X + IconMargin.X), rb.Y + y * RowStride, IconSize.X, IconSize.Y);

				var rsi = item.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(World, rsi.GetImage(item, faction));
				var bi = item.TraitInfo<BuildableInfo>();
				icon.Play(bi.Icon);

				var palette = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + producer.Actor.Owner.InternalName : bi.IconPalette;

				var pi = new ProductionIcon()
				{
					Actor = item,
					Name = item.Name,
					Hotkey = DisplayedIconCount < HotkeyCount ? hotkeys[DisplayedIconCount] : null,
					Sprite = icon.Image,
					Palette = worldRenderer.Palette(palette),
					IconClockPalette = worldRenderer.Palette(ClockPalette),
					IconDarkenPalette = worldRenderer.Palette(NotBuildablePalette),
					Pos = new float2(rect.Location),
					Queued = currentQueue.AllQueued().Where(a => a.Item == item.Name).ToList(),
					ProductionQueue = currentQueue
				};

				icons.Add(rect, pi);
				AddBarButtons(pi, rect);
				DisplayedIconCount++;
			}

			SyncPriorityButtonWidgets();
			SyncBulkButtonWidgets();
			SyncCancelButtonWidgets();

			eventBounds = BarHeight > 0
				? icons.Keys.Concat(barButtons.Select(b => b.Bounds)).Union()
				: icons.Keys.Union();

			if (oldIconCount != DisplayedIconCount)
				OnIconCountChanged(oldIconCount, DisplayedIconCount);
		}

		public override void Draw()
		{
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0, World.Timestep)) / 2;

			if (CurrentQueue == null)
				return;

			var buildableItems = CurrentQueue.BuildableItems();

			// Icons
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var icon in icons.Values)
			{
				WidgetUtils.DrawSpriteCentered(icon.Sprite, icon.Palette, icon.Pos + iconOffset);

				// Draw the ProductionIconOverlay's sprites
				foreach (var pio in pios.Where(p => p.IsOverlayActive(icon.Actor)))
					WidgetUtils.DrawSpriteCentered(pio.Sprite, worldRenderer.Palette(pio.Palette), icon.Pos + iconOffset + pio.Offset(IconSize));

				// Build progress
				if (icon.Queued.Count > 0)
				{
					var first = icon.Queued[0];
					clock.PlayFetchIndex(ClockSequence,
						() => (first.TotalTime - first.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / first.TotalTime);
					clock.Tick();

					WidgetUtils.DrawSpriteCentered(clock.Image, icon.IconClockPalette, icon.Pos + iconOffset);
				}
				else if (!buildableItems.Any(a => a.Name == icon.Name))
					WidgetUtils.DrawSpriteCentered(cantBuild.Image, icon.IconDarkenPalette, icon.Pos + iconOffset);
			}

			Game.Renderer.DisableAntialiasingFilter();

			// Overlays
			foreach (var icon in icons.Values)
			{
				var total = icon.Queued.Count;
				if (total > 0)
				{
					var first = icon.Queued[0];
					var waiting = !CurrentQueue.IsProducing(first) && !first.Done;
					if (first.Done)
					{
						if (CurrentQueue is not BulkProductionQueue)
						{
							if (ReadyTextStyle == ReadyTextStyleOptions.Solid || orderManager.LocalFrameNumber * worldRenderer.World.Timestep / 360 % 2 == 0)
								overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, TextColor, Color.Black, 1);
							else if (ReadyTextStyle == ReadyTextStyleOptions.AlternatingColor)
								overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, ReadyTextAltColor, Color.Black, 1);
						}
					}
					else if (first.Paused)
						overlayFont.DrawTextWithContrast(HoldText,
							icon.Pos + holdOffset,
							TextColor, Color.Black, 1);
					else if (!waiting && DrawTime)
						overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(first.Queue.RemainingTimeActual(first), World.Timestep),
							icon.Pos + timeOffset,
							TextColor, Color.Black, 1);

					if (first.Infinite && symbolFont != null)
						symbolFont.DrawTextWithContrast(InfiniteSymbol,
							icon.Pos + infiniteOffset,
							TextColor, Color.Black, 1);
					else if (total > 1 || waiting)
					{
						var pos = QueuedOffset;
						if (QueuedTextAlign != TextAlign.Left)
						{
							var size = overlayFont.Measure(total.ToString(NumberFormatInfo.CurrentInfo));

							pos = QueuedTextAlign == TextAlign.Center ?
								new float2(QueuedOffset.X - size.X / 2, QueuedOffset.Y) :
								new float2(QueuedOffset.X - size.X, QueuedOffset.Y);
						}

						overlayFont.DrawTextWithContrast(total.ToString(NumberFormatInfo.CurrentInfo),
							icon.Pos + pos,
							TextColor, Color.Black, 1);
					}
				}

				if (CurrentQueue is BulkProductionQueue bulkProductionQueue)
				{
					var readyActors = bulkProductionQueue.GetActorsReadyForDelivery().
						Count(a => a.Actor.Name == icon.Name);
					overlayFont.DrawTextWithContrast(readyActors.ToString(NumberFormatInfo.CurrentInfo),
						icon.Pos + BulkOffset, TextColor, Color.Black, 1);
				}
			}

			if (BarHeight > 0 && !string.IsNullOrEmpty(resolvedBarButtonBackground))
			{
				foreach (var barButton in barButtons)
				{
					var hover = barButton.Bounds.Contains(Viewport.LastMousePos);
					var pressed = Ui.MouseFocusWidget == this && barButton.Bounds.Contains(Viewport.LastMousePos);
					ButtonWidget.DrawBackground(resolvedBarButtonBackground, barButton.Bounds, false, pressed, hover, false);
				}
			}
		}

		public override string GetCursor(int2 pos)
		{
			foreach (var child in Children)
			{
				var cursor = child.GetCursor(pos);
				if (cursor != null)
					return cursor;
			}

			var icon = icons.Where(i => i.Key.Contains(pos))
				.Select(i => i.Value).FirstOrDefault();

			return icon != null ? base.GetCursor(pos) : null;
		}
	}
}
