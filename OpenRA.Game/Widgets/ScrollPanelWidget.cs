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
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public interface ILayout { void AdjustChild(Widget w); void AdjustChildren(); }

	public class ScrollPanelWidget : Widget
	{
		public int ScrollbarWidth = 24;
		public float ScrollVelocity = 4f;
		public int ItemSpacing = 2;
		public int ButtonDepth = ChromeMetrics.Get<int>("ButtonDepth");
		public string Background = "scrollpanel-bg";
		public int ContentHeight = 0;
		public ILayout Layout;
		protected float ListOffset = 0;
		protected bool UpPressed = false;
		protected bool DownPressed = false;
		protected bool ThumbPressed = false;
		protected Rectangle upButtonRect;
		protected Rectangle downButtonRect;
		protected Rectangle backgroundRect;
		protected Rectangle scrollbarRect;
		protected Rectangle thumbRect;

		public ScrollPanelWidget() : base() { Layout = new ListLayout(this); }

		public override void RemoveChildren()
		{
			ContentHeight = 0;
			base.RemoveChildren();
		}

		public override void AddChild(Widget child)
		{
			// Initial setup of margins/height
			Layout.AdjustChild(child);
			base.AddChild(child);
		}

		public override void DrawOuter()
		{
			if (!IsVisible())
				return;

			var rb = RenderBounds;

			var ScrollbarHeight = rb.Height - 2 * ScrollbarWidth;

			var thumbHeight = ContentHeight == 0 ? 0 : (int)(ScrollbarHeight*Math.Min(rb.Height*1f/ContentHeight, 1f));
			var thumbOrigin = rb.Y + ScrollbarWidth + (int)((ScrollbarHeight - thumbHeight)*(-1f*ListOffset/(ContentHeight - rb.Height)));
			if (thumbHeight == ScrollbarHeight)
				thumbHeight = 0;

			backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width - ScrollbarWidth+1, rb.Height);
			upButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y + ScrollbarWidth - 1, ScrollbarWidth, ScrollbarHeight + 2);
			thumbRect = new Rectangle(rb.Right - ScrollbarWidth, thumbOrigin, ScrollbarWidth, thumbHeight);

			var upHover = Ui.MouseOverWidget == this && upButtonRect.Contains(Viewport.LastMousePos);
			var upDisabled = thumbHeight == 0 || ListOffset >= 0;

			var downHover = Ui.MouseOverWidget == this && downButtonRect.Contains(Viewport.LastMousePos);
			var downDisabled = thumbHeight == 0 || ListOffset <= Bounds.Height - ContentHeight;

			var thumbHover = Ui.MouseOverWidget == this && thumbRect.Contains(Viewport.LastMousePos);
			WidgetUtils.DrawPanel(Background, backgroundRect);
			WidgetUtils.DrawPanel("scrollpanel-bg", scrollbarRect);
			ButtonWidget.DrawBackground("button", upButtonRect, upDisabled, UpPressed, upHover, false);
			ButtonWidget.DrawBackground("button", downButtonRect, downDisabled, DownPressed, downHover, false);

			if (thumbHeight > 0)
				ButtonWidget.DrawBackground("scrollthumb", thumbRect, false, Focused && thumbHover, thumbHover, false);

			var upOffset = !UpPressed || upDisabled ? 4 : 4 + ButtonDepth;
			var downOffset = !DownPressed || downDisabled ? 4 : 4 + ButtonDepth;

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", UpPressed || upDisabled ? "up_pressed" : "up_arrow"),
				new float2(upButtonRect.Left + upOffset, upButtonRect.Top + upOffset));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", DownPressed || downDisabled ? "down_pressed" : "down_arrow"),
				new float2(downButtonRect.Left + downOffset, downButtonRect.Top + downOffset));

			Game.Renderer.EnableScissor(backgroundRect.X + 1, backgroundRect.Y + 1, backgroundRect.Width - 2, backgroundRect.Height - 2);

			Layout.AdjustChildren();

			foreach (var child in Children)
				child.DrawOuter();

			Game.Renderer.DisableScissor();
		}

		public override int2 ChildOrigin { get { return RenderOrigin + new int2(0, (int)ListOffset); } }

		public override Rectangle GetEventBounds()
		{
			return EventBounds;
		}

		void Scroll(int direction)
		{
			ListOffset += direction*ScrollVelocity;
			ListOffset = Math.Min(0,Math.Max(Bounds.Height - ContentHeight, ListOffset));
		}

		public void ScrollToBottom()
		{
			ListOffset = Math.Min(0,Bounds.Height - ContentHeight);
		}

		public void ScrollToTop()
		{
			ListOffset = 0;
		}

		public override void Tick ()
		{
			if (UpPressed) Scroll(1);
			if (DownPressed) Scroll(-1);
		}

		public override bool LoseFocus (MouseInput mi)
		{
			UpPressed = DownPressed = ThumbPressed = false;
			return base.LoseFocus(mi);
		}

		int2 lastMouseLocation;
		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.WheelDown)
			{
				Scroll(-1);
				return true;
			}

			if (mi.Button == MouseButton.WheelUp)
			{
				Scroll(1);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			if (!Focused)
				return false;

			if (Focused && mi.Event == MouseInputEvent.Up)
				return LoseFocus(mi);

			if (ThumbPressed && mi.Event == MouseInputEvent.Move)
			{
				var rb = RenderBounds;
				var ScrollbarHeight = rb.Height - 2 * ScrollbarWidth;
				var thumbHeight = ContentHeight == 0 ? 0 : (int)(ScrollbarHeight*Math.Min(rb.Height*1f/ContentHeight, 1f));
				var oldOffset = ListOffset;
				ListOffset += (int)((lastMouseLocation.Y - mi.Location.Y)*(ContentHeight - rb.Height)*1f/(ScrollbarHeight - thumbHeight));
				ListOffset = Math.Min(0,Math.Max(rb.Height - ContentHeight, ListOffset));

				if (oldOffset != ListOffset)
					lastMouseLocation = mi.Location;
			}
			else
			{
				UpPressed = upButtonRect.Contains(mi.Location);
				DownPressed = downButtonRect.Contains(mi.Location);
				ThumbPressed = thumbRect.Contains(mi.Location);
				if (ThumbPressed)
					lastMouseLocation = mi.Location;
			}

			return UpPressed || DownPressed || ThumbPressed;
		}
	}
}
