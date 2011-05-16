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
		public bool Bold = false;
		public bool Depressed = false;
		public int VisualHeight = ChromeMetrics.GetInt("ButtonDepth");
		public Func<string> GetText;

		public ButtonWidget()
			: base()
		{
			GetText = () => { return Text; };
		}

		protected ButtonWidget(ButtonWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			Depressed = widget.Depressed;
			VisualHeight = widget.VisualHeight;
			GetText = widget.GetText;
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
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			WidgetUtils.DrawPanel(Depressed ? "dialog3" : "dialog2", RenderBounds);

			var text = GetText();

			font.DrawText(text,
				RenderOrigin + new int2(UsableWidth / 2, Bounds.Height / 2)
					- new int2(font.Measure(text).X / 2,
				font.Measure(text).Y / 2) + stateOffset, Color.White);
		}

		public override Widget Clone() { return new ButtonWidget(this); }
		public virtual int UsableWidth { get { return Bounds.Width; } }
	}

	public class DropDownButtonWidget : ButtonWidget
	{
		public DropDownButtonWidget()
			: base()
		{
		}

		protected DropDownButtonWidget(DropDownButtonWidget widget)
			: base(widget)
		{
		}

		public override void DrawInner()
		{
			base.DrawInner();
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			
			var image = ChromeProvider.GetImage("scrollbar", "down_arrow");
			var rb = RenderBounds;
			
			WidgetUtils.DrawRGBA( image,
				stateOffset + new float2( rb.Right - rb.Height + 4, 
					rb.Top + (rb.Height - image.bounds.Height) / 2 ));

			WidgetUtils.FillRectWithColor(new Rectangle(stateOffset.X + rb.Right - rb.Height,
				stateOffset.Y + rb.Top + 3, 1, rb.Height - 6),
				Color.White);
		}

		public override Widget Clone() { return new DropDownButtonWidget(this); }
		public override int UsableWidth { get { return Bounds.Width - Bounds.Height; } } /* space for button */

		public static void ShowDropPanel(Widget w, Widget panel, IEnumerable<Widget> dismissAfter, Func<bool> onDismiss)
		{
			// Mask to prevent any clicks from being sent to other widgets
			var fullscreenMask = new ContainerWidget();
			fullscreenMask.Bounds = new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height);
			Widget.RootWidget.AddChild(fullscreenMask);
			
			Action HideDropDown = () =>
			{
				Widget.RootWidget.RemoveChild(fullscreenMask);
				Widget.RootWidget.RemoveChild(panel);
			};
			
			HideDropDown += () => Game.BeforeGameStart -= HideDropDown;
			Game.BeforeGameStart += HideDropDown;

			fullscreenMask.OnMouseDown = mi =>
			{
				if (onDismiss()) HideDropDown();
				return true;
			};
			fullscreenMask.OnMouseUp = mi => true;

			var oldBounds = panel.Bounds;
			panel.Bounds = new Rectangle(w.RenderOrigin.X, w.RenderOrigin.Y + w.Bounds.Height, oldBounds.Width, oldBounds.Height);
			panel.OnMouseUp = mi => true;
			
			foreach (var ww in dismissAfter)
			{
				var origMouseUp = ww.OnMouseUp;
				ww.OnMouseUp = mi => { var result = origMouseUp(mi); if (onDismiss()) HideDropDown(); return result; };
			}
			Widget.RootWidget.AddChild(panel);
		}
		
		public static void ShowDropDown<T>(Widget w, IEnumerable<T> ts, Func<T, int, LabelWidget> ft)
		{
			var dropDown = new ScrollPanelWidget();
			dropDown.Bounds = new Rectangle(w.RenderOrigin.X, w.RenderOrigin.Y + w.Bounds.Height, w.Bounds.Width, 100);
			dropDown.ItemSpacing = 1;

			List<LabelWidget> items = new List<LabelWidget>();
			List<Widget> dismissAfter = new List<Widget>();
			foreach (var t in ts)
			{
				var ww = ft(t, dropDown.Bounds.Width - dropDown.ScrollbarWidth);
				dismissAfter.Add(ww);
				ww.OnMouseMove = mi => items.Do(lw =>
				{
					lw.Background = null; ww.Background = "dialog2";
				});
	
				dropDown.AddChild(ww);
				items.Add(ww);
			}
			
			dropDown.Bounds.Height = Math.Min(150, dropDown.ContentHeight);
			ShowDropPanel(w, dropDown, dismissAfter, () => true);
		}
	}
}