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
		public string Text = "";
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.GetInt("ButtonDepth");
		public string Font = ChromeMetrics.GetString("ButtonFont");
		public Func<string> GetText;
		public Func<bool> IsDisabled = () => false;
		public Action OnClick = () => {};
		
		public ButtonWidget()
			: base()
		{
			GetText = () => { return Text; };
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
		}

		protected ButtonWidget(ButtonWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			Font = widget.Font;
			Depressed = widget.Depressed;
			VisualHeight = widget.VisualHeight;
			GetText = widget.GetText;
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
		}

		public override bool LoseFocus(MouseInput mi)
		{
			Depressed = false;
			return base.LoseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;
			
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			// Only fire the onMouseUp event if we successfully lost focus, and were pressed
			if (Focused && mi.Event == MouseInputEvent.Up)
			{
				if (Depressed)
					OnMouseUp(mi);
				
				return LoseFocus(mi);
			}

			if (mi.Event == MouseInputEvent.Down)
			{
				// OnMouseDown returns false if the button shouldn't be pressed
				if (!OnMouseDown(mi))
					Depressed = true;
				else
					LoseFocus(mi);
			}
			
			else if (mi.Event == MouseInputEvent.Move && Focused)
			{
				Depressed = RenderBounds.Contains(mi.Location.X, mi.Location.Y);
				
				// All widgets should receive MouseMove events
				OnMouseMove(mi);
			}
			
			return Depressed;
		}

		public override int2 ChildOrigin { get { return RenderOrigin + 
				((Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0)); } }
		
		public override void DrawInner()
		{
			var rb = RenderBounds;
			var disabled = IsDisabled();
			
			var font = Game.Renderer.Fonts[Font];
			var text = GetText();
			var s = font.Measure(text);
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			
			DrawBackground(rb, disabled, Depressed, rb.Contains(Viewport.LastMousePos));
			font.DrawText(text, new int2(rb.X + (UsableWidth - s.X)/ 2, rb.Y + (Bounds.Height - s.Y) / 2) + stateOffset,
			              disabled ? Color.Gray : Color.White);
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth { get { return Bounds.Width; } }
		
		public static void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover)
		{
			var state = disabled ? "button-disabled" : 
						pressed ? "button-pressed" : 
						hover ? "button-hover" : 
						"button";
			
			WidgetUtils.DrawPanel(state, rect);
		}
	}
}