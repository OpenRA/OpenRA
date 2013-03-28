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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ButtonWidget : Widget
	{
		public Func<ButtonWidget, string> GetKey = _ => null;
		public string Key
		{
			get { return GetKey(this); }
			set { GetKey = _ => value; }
		}

		public string Text = "";
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.Get<int>("ButtonDepth");
		public string Font = ChromeMetrics.Get<string>("ButtonFont");
		public bool Disabled = false;
		public bool Highlighted = false;
		public Func<string> GetText;
		public Func<bool> IsDisabled;
		public Func<bool> IsHighlighted;
		public Action<MouseInput> OnMouseDown = _ => {};
		public Action<MouseInput> OnMouseUp = _ => {};

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
		}

		protected ButtonWidget(ButtonWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			Font = widget.Font;
			Depressed = widget.Depressed;
			VisualHeight = widget.VisualHeight;
			GetText = widget.GetText;
			OnMouseDown = widget.OnMouseDown;
			Disabled = widget.Disabled;
			IsDisabled = widget.IsDisabled;
			Highlighted = widget.Highlighted;
			IsHighlighted = widget.IsHighlighted;

			OnMouseUp = mi => OnClick();
			OnKeyPress = _ => OnClick();
		}

		public override bool LoseFocus(MouseInput mi)
		{
			Depressed = false;
			return base.LoseFocus(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.KeyName != Key || e.Event != KeyInputEvent.Down)
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

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			var disabled = IsDisabled();
			if (Focused && mi.Event == MouseInputEvent.Up && mi.MultiTapCount == 2)
			{
				if (!disabled)
				{
					OnDoubleClick();
					return LoseFocus(mi);
				}
			} 
			// Only fire the onMouseUp event if we successfully lost focus, and were pressed
			else if (Focused && mi.Event == MouseInputEvent.Up)
			{
				if (Depressed && !disabled)
					OnMouseUp(mi);

				return LoseFocus(mi);
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
					LoseFocus(mi);
					Sound.PlayNotification(null, "Sounds", "ClickDisabledSound", null);
				}
			}
			else if (mi.Event == MouseInputEvent.Move && Focused)
				Depressed = RenderBounds.Contains(mi.Location);

			return Depressed;
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
			var s = font.Measure(text);
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);

			DrawBackground(rb, disabled, Depressed, Ui.MouseOverWidget == this, highlighted);
			font.DrawText(text, new int2(rb.X + (UsableWidth - s.X)/ 2, rb.Y + (Bounds.Height - s.Y) / 2) + stateOffset,
						  disabled ? Color.Gray : Color.White);
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth { get { return Bounds.Width; } }

		public virtual void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			ButtonWidget.DrawBackground("button", rect, disabled, pressed, hover, highlighted);
		}

		public static void DrawBackground(string baseName, Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
		{
			var state = disabled ? "-disabled" :
						pressed ? "-pressed" :
						hover ? "-hover" :
						"";
			if (highlighted)
				state += "-highlighted";

			WidgetUtils.DrawPanel(baseName + state, rect);
		}
	}
}
