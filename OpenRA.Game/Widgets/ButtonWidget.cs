#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	public class ButtonWidget : Widget
	{
		public string Text = "";
		public bool Bold = false;
		public bool Depressed = false;
		public int VisualHeight = 1;
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

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			// Only fire the onMouseUp order if we successfully lost focus, and were pressed
			if (Focused && mi.Event == MouseInputEvent.Up)
			{
				var wasPressed = Depressed;
				return (LoseFocus(mi) && wasPressed);
			}

			if (mi.Event == MouseInputEvent.Down)
				Depressed = true;
			else if (mi.Event == MouseInputEvent.Move && Focused)
				Depressed = RenderBounds.Contains(mi.Location.X, mi.Location.Y);

			return Depressed;
		}

		public override void DrawInner( WorldRenderer wr )
		{
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			WidgetUtils.DrawPanel(Depressed ? "dialog3" : "dialog2", RenderBounds);

			var text = GetText();

			font.DrawText(text,
				new int2(RenderOrigin.X + UsableWidth / 2, RenderOrigin.Y + Bounds.Height / 2)
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

		public override void DrawInner(WorldRenderer wr)
		{
			base.DrawInner(wr);
			var image = ChromeProvider.GetImage("scrollbar", "down_arrow");
			WidgetUtils.DrawRGBA( image,
				new float2( RenderBounds.Right - RenderBounds.Height + 4, 
					RenderBounds.Top + (RenderBounds.Height - image.bounds.Height) / 2 ));

			WidgetUtils.FillRectWithColor(new Rectangle(RenderBounds.Right - RenderBounds.Height,
				RenderBounds.Top + 3, 1, RenderBounds.Height - 6),
				Color.White);
		}

		public override Widget Clone() { return new DropDownButtonWidget(this); }
		public override int UsableWidth { get { return Bounds.Width - Bounds.Height; } } /* space for button */

		public static void ShowDropDown<T>(Widget w, IEnumerable<T> ts, Func<T, int, LabelWidget> ft)
		{
			var fullscreenMask = new ContainerWidget
			{
				Bounds = new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height),
				ClickThrough = false,
				Visible = true
			};

			Widget.RootWidget.AddChild(fullscreenMask);

			var origin = w.RenderOrigin;
			var dropDown = new ScrollPanelWidget
			{
				Bounds = new Rectangle(w.RenderOrigin.X, w.RenderOrigin.Y + w.Bounds.Height, w.Bounds.Width, 100),
				Visible = true,
				ClickThrough = false,
				OnMouseUp = mi => true,
			};

			Widget.RootWidget.AddChild(dropDown);

			Action HideDropDown = () =>
			{
				Widget.RootWidget.Children.Remove(fullscreenMask);
				Widget.RootWidget.Children.Remove(dropDown);
			};

			fullscreenMask.OnMouseUp = mi =>
			{
				HideDropDown();
				return false;
			};

			var y = 0;
			List<LabelWidget> items = new List<LabelWidget>();

			foreach (var t in ts)
			{
				var tt = t;
				var ww = ft(t, dropDown.Bounds.Width);
				var origMouseUp = ww.OnMouseUp;
				ww.OnMouseUp = mi => { var result = origMouseUp(mi); HideDropDown(); return result; };
				ww.ClickThrough = false;
				ww.IsVisible = () => true;
				ww.Bounds = new Rectangle(0, y, ww.Bounds.Width, ww.Bounds.Height);

				ww.OnMouseMove = mi =>
				{
					items.Do(lw =>
					{
						lw.Background = null; ww.Background = "dialog2";
					}); return true;
				};

				dropDown.AddChild(ww);
				items.Add(ww);

				y += ww.Bounds.Height;
			}

			dropDown.ContentHeight = y;
			dropDown.Bounds.Height = y + 2;
		}
	}
}