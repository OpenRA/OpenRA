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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ButtonWidget : InputWidget
	{
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "BUTTON_TOOLTIP";

		public HotkeyReference Key = new HotkeyReference();
		public bool DisableKeyRepeat = false;
		public bool DisableKeySound = false;

		public string Text = "";
		public TextAlign Align = TextAlign.Center;
		public int LeftMargin = 5;
		public int RightMargin = 5;
		public string Background = "button";
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.Get<int>("ButtonDepth");
		public string Font = ChromeMetrics.Get<string>("ButtonFont");
		public Color TextColor = ChromeMetrics.Get<Color>("ButtonTextColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("ButtonTextColorDisabled");
		public bool Contrast = ChromeMetrics.Get<bool>("ButtonTextContrast");
		public bool Shadow = ChromeMetrics.Get<bool>("ButtonTextShadow");
		public Color ContrastColorDark = ChromeMetrics.Get<Color>("ButtonTextContrastColorDark");
		public Color ContrastColorLight = ChromeMetrics.Get<Color>("ButtonTextContrastColorLight");
		public int ContrastRadius = ChromeMetrics.Get<int>("ButtonTextContrastRadius");
		public string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		public bool Highlighted = false;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetColorDisabled;
		public Func<Color> GetContrastColorDark;
		public Func<Color> GetContrastColorLight;
		public Func<bool> IsHighlighted;
		public Action<MouseInput> OnMouseDown = _ => { };
		public Action<MouseInput> OnMouseUp = _ => { };

		protected Lazy<TooltipContainerWidget> tooltipContainer;

		public string TooltipText;
		public Func<string> GetTooltipText;

		public string TooltipDesc;
		public Func<string> GetTooltipDesc;

		// Equivalent to OnMouseUp, but without an input arg
		public Action OnClick = () => { };
		public Action OnDoubleClick = null;
		public Action<KeyInput> OnKeyPress = _ => { };

		public string Cursor = ChromeMetrics.Get<string>("ButtonCursor");

		protected readonly Ruleset ModRules;

		[ObjectCreator.UseCtor]
		public ButtonWidget(ModData modData)
		{
			ModRules = modData.DefaultRules;

			GetText = () => Text;
			GetColor = () => TextColor;
			GetColorDisabled = () => TextColorDisabled;
			GetContrastColorDark = () => ContrastColorDark;
			GetContrastColorLight = () => ContrastColorLight;
			OnMouseUp = _ => OnClick();
			OnKeyPress = _ => OnClick();
			IsHighlighted = () => Highlighted;
			GetTooltipText = () => TooltipText;
			GetTooltipDesc = () => TooltipDesc;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ButtonWidget(ButtonWidget other)
			: base(other)
		{
			ModRules = other.ModRules;

			Text = other.Text;
			Align = other.Align;
			LeftMargin = other.LeftMargin;
			RightMargin = other.RightMargin;
			Font = other.Font;
			TextColor = other.TextColor;
			TextColorDisabled = other.TextColorDisabled;
			Contrast = other.Contrast;
			Shadow = other.Shadow;
			Depressed = other.Depressed;
			Background = other.Background;
			VisualHeight = other.VisualHeight;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetColorDisabled = other.GetColorDisabled;
			ContrastColorDark = other.ContrastColorDark;
			ContrastColorLight = other.ContrastColorLight;
			ContrastRadius = other.ContrastRadius;
			GetContrastColorDark = other.GetContrastColorDark;
			GetContrastColorLight = other.GetContrastColorLight;
			OnMouseDown = other.OnMouseDown;
			Disabled = other.Disabled;
			Highlighted = other.Highlighted;
			IsHighlighted = other.IsHighlighted;

			OnMouseUp = mi => OnClick();
			OnKeyPress = _ => OnClick();

			TooltipTemplate = other.TooltipTemplate;
			TooltipText = other.TooltipText;
			GetTooltipText = other.GetTooltipText;
			TooltipDesc = other.TooltipDesc;
			GetTooltipDesc = other.GetTooltipDesc;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			Depressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!Key.IsActivatedBy(e) || e.Event != KeyInputEvent.Down || (DisableKeyRepeat && e.IsRepeat))
				return false;

			if (!IsDisabled())
			{
				OnKeyPress(e);
				if (!DisableKeySound)
					Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null);
			}
			else if (!DisableKeySound)
				Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickDisabledSound, null);

			return true;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			var disabled = IsDisabled();
			if (HasMouseFocus && mi.Event == MouseInputEvent.Up && mi.MultiTapCount == 2 && OnDoubleClick != null)
			{
				if (!disabled)
				{
					OnDoubleClick();
					return YieldMouseFocus(mi);
				}
			}
			else if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
			{
				// Only fire the onMouseUp event if we successfully lost focus, and were pressed
				if (Depressed && !disabled)
					OnMouseUp(mi);

				return YieldMouseFocus(mi);
			}

			if (mi.Event == MouseInputEvent.Down)
			{
				// OnMouseDown returns false if the button shouldn't be pressed
				if (!disabled)
				{
					OnMouseDown(mi);
					Depressed = true;
					Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null);
				}
				else
				{
					YieldMouseFocus(mi);
					Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickDisabledSound, null);
				}
			}
			else if (mi.Event == MouseInputEvent.Move && HasMouseFocus)
				Depressed = RenderBounds.Contains(mi.Location);

			return Depressed;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			if (GetTooltipText != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "button", this }, { "getText", GetTooltipText }, { "getDesc", GetTooltipDesc } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null || !tooltipContainer.IsValueCreated)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override string GetCursor(int2 pos) { return Cursor; }

		public override int2 ChildOrigin =>
			RenderOrigin +
			(Depressed ? new int2(VisualHeight, VisualHeight) : new int2(0, 0));

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

			var position = GetTextPosition(text, font, rb);

			// PERF: Avoid LINQ by using Children.Find(...) != null instead of Children.Any(...)
			var hover = Ui.MouseOverWidget == this || Children.Find(c => c == Ui.MouseOverWidget) != null;
			DrawBackground(rb, disabled, Depressed, hover, highlighted);
			if (Contrast)
				font.DrawTextWithContrast(text, position + stateOffset,
					disabled ? colordisabled : color, bgDark, bgLight, ContrastRadius);
			else if (Shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position + stateOffset,
					disabled ? colordisabled : color);
		}

		int2 GetTextPosition(string text, SpriteFont font, Rectangle rb)
		{
			var textSize = font.Measure(text);
			var y = rb.Y + (Bounds.Height - textSize.Y - font.TopOffset) / 2;

			switch (Align)
			{
				case TextAlign.Left:
					return new int2(rb.X + LeftMargin, y);
				case TextAlign.Center:
					return new int2(rb.X + (UsableWidth - textSize.X) / 2, y);
				case TextAlign.Right:
					return new int2(rb.X + UsableWidth - textSize.X - RightMargin, y);
				default:
					throw new ArgumentOutOfRangeException("Align");
			}
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth => Bounds.Width;

		public virtual void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			DrawBackground(Background, rect, disabled, pressed, hover, highlighted);
		}

		public static void DrawBackground(string baseName, Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			if (string.IsNullOrEmpty(baseName))
				return;

			var variantName = highlighted ? baseName + "-highlighted" : baseName;
			var imageName = WidgetUtils.GetStatefulImageName(variantName, disabled, pressed, hover);

			WidgetUtils.DrawPanel(imageName, rect);
		}
	}
}
