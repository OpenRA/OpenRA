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
	public class DropDownButtonWidget : ButtonWidget
	{
		Widget panel;
		Widget fullscreenMask;
		
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
			
			var image = ChromeProvider.GetImage("scrollbar", IsDisabled() ? "down_pressed" : "down_arrow");
			var rb = RenderBounds;
			
			WidgetUtils.DrawRGBA( image,
				stateOffset + new float2( rb.Right - rb.Height + 4, 
					rb.Top + (rb.Height - image.bounds.Height) / 2 ));

			WidgetUtils.FillRectWithColor(new Rectangle(stateOffset.X + rb.Right - rb.Height,
				stateOffset.Y + rb.Top + 3, 1, rb.Height - 6),
				Color.White);
		}

		public override Widget Clone() { return new DropDownButtonWidget(this); }
		
		// This is crap
		public override int UsableWidth { get { return Bounds.Width - Bounds.Height; } } /* space for button */	

		public void RemovePanel()
		{
			Widget.RootWidget.RemoveChild(fullscreenMask);
			Widget.RootWidget.RemoveChild(panel);
			Game.BeforeGameStart -= RemovePanel;
			panel = fullscreenMask = null;
		}
		
		public void AttachPanel(Widget p)
		{
			if (panel != null)
				throw new InvalidOperationException("Attempted to attach a panel to an open dropdown");
			panel = p;
			
			// Mask to prevent any clicks from being sent to other widgets
			fullscreenMask = new ContainerWidget();
			fullscreenMask.Bounds = new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height);
			Widget.RootWidget.AddChild(fullscreenMask);
			Game.BeforeGameStart += RemovePanel;

			fullscreenMask.OnMouseDown = mi =>
			{
				RemovePanel();
				return true;
			};
			fullscreenMask.OnMouseUp = mi => true;

			var oldBounds = panel.Bounds;
			panel.Bounds = new Rectangle(RenderOrigin.X, RenderOrigin.Y + Bounds.Height, oldBounds.Width, oldBounds.Height);
			Widget.RootWidget.AddChild(panel);
		}
		
		[Obsolete] public static void ShowDropPanel(Widget w, Widget panel, IEnumerable<Widget> dismissAfter, Func<bool> onDismiss)
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
		
		[Obsolete] public static void ShowDropDown<T>(Widget w, IEnumerable<T> ts, Func<T, int, LabelWidget> ft)
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