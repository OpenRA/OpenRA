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
	public class ScrollPanelWidget : Widget
	{
		public string Background = "dialog3";
		public int ScrollbarWidth = 24;
		public float ScrollVelocity = 4f;		
		public int ItemSpacing = 2;
		
		public int ContentHeight = 0;
		protected float ListOffset = 0;
		protected bool UpPressed = false;
		protected bool DownPressed = false;
		protected bool ThumbPressed = false;
		protected Rectangle upButtonRect;
		protected Rectangle downButtonRect;
		protected Rectangle backgroundRect;
		protected Rectangle scrollbarRect;
		protected Rectangle thumbRect;
		
		public ScrollPanelWidget() : base() {}
		protected ScrollPanelWidget(ScrollPanelWidget other)
			: base(other)
		{
			throw new NotImplementedException();
		}
		
		public override void RemoveChildren()
		{
			ContentHeight = 0;
			base.RemoveChildren();
		}
		
		public override void AddChild(Widget child)
		{
			// Initial setup of margins/height
			if (Children.Count == 0)
				ContentHeight = ItemSpacing;
			
			child.Bounds.Y += ContentHeight;
			ContentHeight += child.Bounds.Height + ItemSpacing;
			base.AddChild(child);
		}
		
		public override void DrawInner() {}
		public override void Draw()
		{
			if (!IsVisible())
				return;
			
			var rb = RenderBounds;
			
			var ScrollbarHeight = rb.Height - 2 * ScrollbarWidth;
			
			var thumbHeight = ContentHeight == 0 ? 0 : (int)(ScrollbarHeight*Math.Min(rb.Height*1f/ContentHeight, 1f));
			var thumbOrigin = rb.Y + ScrollbarWidth + (int)((ScrollbarHeight - thumbHeight)*(-1f*ListOffset/(ContentHeight - rb.Height)));
			if (thumbHeight == ScrollbarHeight)
				thumbHeight = 0;
			
			backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width - ScrollbarWidth, rb.Height);
			upButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y + ScrollbarWidth, ScrollbarWidth, ScrollbarHeight);
			thumbRect = new Rectangle(rb.Right - ScrollbarWidth, thumbOrigin, ScrollbarWidth, thumbHeight);
			
			string upButtonBg = UpPressed || thumbHeight == 0 ? "dialog3" : "dialog2";
			string downButtonBg = DownPressed || thumbHeight == 0 ? "dialog3" : "dialog2";
			string scrollbarBg = "dialog3";
			string thumbBg = "dialog2";

			WidgetUtils.DrawPanel(Background, backgroundRect);
			WidgetUtils.DrawPanel(upButtonBg, upButtonRect);
			WidgetUtils.DrawPanel(downButtonBg, downButtonRect);
			WidgetUtils.DrawPanel(scrollbarBg, scrollbarRect);
			
			if (thumbHeight > 0)
				WidgetUtils.DrawPanel(thumbBg, thumbRect);

			var upOffset = UpPressed || thumbHeight == 0 ? 4 : 3;
			var downOffset = DownPressed || thumbHeight == 0 ? 4 : 3;
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "up_arrow"),
				new float2(upButtonRect.Left + upOffset, upButtonRect.Top + upOffset));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "down_arrow"),
				new float2(downButtonRect.Left + downOffset, downButtonRect.Top + downOffset));

			Game.Renderer.EnableScissor(backgroundRect.X + 1, backgroundRect.Y + 1, backgroundRect.Width - 2, backgroundRect.Height - 2);

			foreach (var child in Children)
				child.Draw();

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
		// TODO: ScrollPanelWidget doesn't support delegate methods for mouse input
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
				UpPressed = upButtonRect.Contains(mi.Location.X, mi.Location.Y);
				DownPressed = downButtonRect.Contains(mi.Location.X, mi.Location.Y);
				ThumbPressed = thumbRect.Contains(mi.Location.X, mi.Location.Y);
				if (ThumbPressed)
					lastMouseLocation = mi.Location;
			}
			
			return (UpPressed || DownPressed || ThumbPressed);
		}

		public override Widget Clone() { return new ScrollPanelWidget(this); }
	}
}