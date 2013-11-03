#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

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
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.Get<int>("ButtonDepth");
		public string Font = ChromeMetrics.Get<string>("ButtonFont");
		public bool Disabled = false;
		public bool Highlighted = false;
		public Color TextColor = Color.White;
		public Color ColorDisabled = Color.Gray;
		public bool Contrast = false;
		public Color ContrastColor = Color.Black;
		public Func<string> GetText;
		public Func<bool> IsDisabled;
		public Func<bool> IsHighlighted;
		public Func<Color> GetColor;
		public Func<Color> GetColorDisabled;
		public Func<Color> GetContrastColor;
		public Action<MouseInput> OnMouseDown = _ => {};
		public Action<MouseInput> OnMouseUp = _ => {};

		public readonly string TooltipTemplate = "BUTTON_TOOLTIP";
		public readonly string TooltipText;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		// Equivalent to OnMouseUp, but without an input arg
		public Action OnClick = () => {};
		public Action OnDoubleClick = () => {}; 
		public Action<KeyInput> OnKeyPress = _ => {};

		public ButtonWidget()
			: base()
		{
			GetText = () => { return Text; };
			OnMouseUp = _ => OnClick();
			OnKeyPress = _ => OnClick();
			IsDisabled = () => Disabled;
			IsHighlighted = () => Highlighted;
			GetColor = () => TextColor;
			GetColorDisabled = () => ColorDisabled;
			GetContrastColor = () => ContrastColor;
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ButtonWidget(ButtonWidget other)
			: base(other)
		{
			Text = other.Text;
			Font = other.Font;
			TextColor = other.TextColor;
			ColorDisabled = other.ColorDisabled;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			Depressed = other.Depressed;
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
			tooltipContainer = Lazy.New(() =>
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
				Sound.PlayNotification(null, "Sounds", "ClickSound", null);
			}
			else
				Sound.PlayNotification(null, "Sounds", "ClickDisabledSound", null);

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
					Sound.PlayNotification(null, "Sounds", "ClickSound", null);
				}
				else
				{
					YieldMouseFocus(mi);
					Sound.PlayNotification(null, "Sounds", "ClickDisabledSound", null);
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

			DrawBackground(rb, disabled, Depressed, Ui.MouseOverWidget == this, highlighted);
			if (Contrast)
				font.DrawTextWithContrast(text, new int2(rb.X + (UsableWidth - s.X) / 2, rb.Y + (Bounds.Height - s.Y) / 2) + stateOffset,
						  disabled ? colordisabled : color, contrast, 2);
			else
				font.DrawText(text, new int2(rb.X + (UsableWidth - s.X)/ 2, rb.Y + (Bounds.Height - s.Y) / 2) + stateOffset,
						  disabled ? colordisabled : color);
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth { get { return Bounds.Width; } }

		public virtual void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			ButtonWidget.DrawBackground("button", rect, disabled, pressed, hover, highlighted);
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
