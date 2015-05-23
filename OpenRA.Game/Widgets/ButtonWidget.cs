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
using System.Drawing;

namespace OpenRA.Widgets
{
	public class ButtonWidget : Widget
	{
		public Func<ButtonWidget, Hotkey> GetKey = _ => Hotkey.Invalid;
		public Hotkey Key
		{
			get { return GetKey(this); }
			set { GetKey = _ => value; }
		}

		[Translate] public string Text = "";
		public string Background = "button";
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.Get<int>("ButtonDepth");
		public string Font = ChromeMetrics.Get<string>("ButtonFont");
		public Color TextColor = ChromeMetrics.Get<Color>("ButtonTextColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("ButtonTextColorDisabled");
		public bool Contrast = ChromeMetrics.Get<bool>("ButtonTextContrast");
		public Color ContrastColor = ChromeMetrics.Get<Color>("ButtonTextContrastColor");
		public bool Disabled = false;
		public bool Highlighted = false;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetColorDisabled;
		public Func<Color> GetContrastColor;
		public Func<bool> IsDisabled;
		public Func<bool> IsHighlighted;
		public Action<MouseInput> OnMouseDown = _ => {};
		public Action<MouseInput> OnMouseUp = _ => {};

		Lazy<TooltipContainerWidget> tooltipContainer;
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "BUTTON_TOOLTIP";
		[Translate] public string TooltipText;

		// Equivalent to OnMouseUp, but without an input arg
		public Action OnClick = () => {};
		public Action OnDoubleClick = () => {}; 
		public Action<KeyInput> OnKeyPress = _ => {};

		protected readonly Ruleset ModRules;

		[ObjectCreator.UseCtor]
		public ButtonWidget(Ruleset modRules)
		{
			ModRules = modRules;

			GetText = () => Text;
			GetColor = () => TextColor;
			GetColorDisabled = () => TextColorDisabled;
			GetContrastColor = () => ContrastColor;
			OnMouseUp = _ => OnClick();
			OnKeyPress = _ => OnClick();
			IsDisabled = () => Disabled;
			IsHighlighted = () => Highlighted;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ButtonWidget(ButtonWidget other)
			: base(other)
		{
			ModRules = other.ModRules;

			Text = other.Text;
			Font = other.Font;
			TextColor = other.TextColor;
			TextColorDisabled = other.TextColorDisabled;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			Depressed = other.Depressed;
			Background = other.Background;
			VisualHeight = other.VisualHeight;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetColorDisabled = other.GetColorDisabled;
			GetContrastColor = other.GetContrastColor;
			OnMouseDown = other.OnMouseDown;
			Disabled = other.Disabled;
			IsDisabled = other.IsDisabled;
			Highlighted = other.Highlighted;
			IsHighlighted = other.IsHighlighted;

			OnMouseUp = mi => OnClick();
			OnKeyPress = _ => OnClick();

			TooltipTemplate = other.TooltipTemplate;
			TooltipText = other.TooltipText;
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
			if (Hotkey.FromKeyInput(e) != Key || e.Event != KeyInputEvent.Down)
				return false;

			if (!IsDisabled())
			{
				OnKeyPress(e);
				Sound.PlayNotification(ModRules, null, "Sounds", "ClickSound", null);
			}
			else
				Sound.PlayNotification(ModRules, null, "Sounds", "ClickDisabledSound", null);

			return true;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			var disabled = IsDisabled();
			if (HasMouseFocus && mi.Event == MouseInputEvent.Up && mi.MultiTapCount == 2)
			{
				if (!disabled)
				{
					OnDoubleClick();
					return YieldMouseFocus(mi);
				}
			} 
			// Only fire the onMouseUp event if we successfully lost focus, and were pressed
			else if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
			{
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
					Sound.PlayNotification(ModRules, null, "Sounds", "ClickSound", null);
				}
				else
				{
					YieldMouseFocus(mi);
					Sound.PlayNotification(ModRules, null, "Sounds", "ClickDisabledSound", null);
				}
			}
			else if (mi.Event == MouseInputEvent.Move && HasMouseFocus)
				Depressed = RenderBounds.Contains(mi.Location);

			return Depressed;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.SetTooltip(TooltipTemplate,
			                                  new WidgetArgs() {{ "button", this }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public override int2 ChildOrigin { get { return RenderOrigin +
				((Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0)); } }

		public override void Draw()
		{
			var rb = RenderBounds;
			var disabled = IsDisabled();
			var highlighted = IsHighlighted();

			var font = Game.Renderer.Fonts[Font];
			var text = GetText();
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var contrast = GetContrastColor();
			var s = font.Measure(text);
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			var position = new int2(rb.X + (UsableWidth - s.X) / 2, rb.Y + (Bounds.Height - s.Y) / 2);

			DrawBackground(rb, disabled, Depressed, Ui.MouseOverWidget == this, highlighted);
			if (Contrast)
				font.DrawTextWithContrast(text, position + stateOffset,
						  disabled ? colordisabled : color, contrast, 2);
			else
				font.DrawText(text, position + stateOffset,
						  disabled ? colordisabled : color);
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth { get { return Bounds.Width; } }

		public virtual void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			DrawBackground(Background, rect, disabled, pressed, hover, highlighted);
		}

		public static void DrawBackground(string baseName, Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			var variant = highlighted ? "-highlighted" : "";
			var state = disabled ? "-disabled" :
						pressed ? "-pressed" :
						hover ? "-hover" :
						"";

			WidgetUtils.DrawPanel(baseName + variant + state, rect);
		}
	}
}
