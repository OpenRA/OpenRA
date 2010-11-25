#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ScrollPanelWidget : Widget
	{
		public readonly string Background = "dialog3";
		public readonly int ScrollbarWidth = 24;
		public readonly float ScrollVelocity = 4f;
		public readonly int HeaderHeight = 0;
		
		
		public int ContentHeight = 0;
		float ListOffset = 0;
		bool UpPressed = false;
		bool DownPressed = false;
		Rectangle upButtonRect;
		Rectangle downButtonRect;
		Rectangle backgroundRect;
		Rectangle scrollbarRect;
		
		public ScrollPanelWidget() : base() {}
		protected ScrollPanelWidget(ScrollPanelWidget other)
			: base(other)
		{
			Background = other.Background;
			upButtonRect = other.upButtonRect;
			downButtonRect = other.downButtonRect;
			scrollbarRect = other.scrollbarRect;
			backgroundRect = other.backgroundRect;
			
			UpPressed = other.UpPressed;	
			DownPressed = other.DownPressed;
		}
		
		public override void DrawInner( WorldRenderer wr ) {}
		public override void Draw( WorldRenderer wr )
		{
			if (!IsVisible())
				return;

			backgroundRect = new Rectangle(RenderBounds.X, RenderBounds.Y, RenderBounds.Width - ScrollbarWidth, RenderBounds.Height);
			upButtonRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Y + ScrollbarWidth, ScrollbarWidth, RenderBounds.Height - 2 * ScrollbarWidth);

			string upButtonBg = (UpPressed) ? "dialog3" : "dialog2";
			string downButtonBg = (DownPressed) ? "dialog3" : "dialog2";
			string scrollbarBg = "dialog3";

			WidgetUtils.DrawPanel(Background, backgroundRect);
			WidgetUtils.DrawPanel(upButtonBg, upButtonRect);
			WidgetUtils.DrawPanel(downButtonBg, downButtonRect);
			WidgetUtils.DrawPanel(scrollbarBg, scrollbarRect);

			var upOffset = UpPressed ? 4 : 3;
			var downOffset = DownPressed ? 4 : 3;
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "up_arrow"),
				new float2(upButtonRect.Left + upOffset, upButtonRect.Top + upOffset));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "down_arrow"),
				new float2(downButtonRect.Left + downOffset, downButtonRect.Top + downOffset));

			Game.Renderer.EnableScissor(backgroundRect.X, backgroundRect.Y + HeaderHeight, backgroundRect.Width, backgroundRect.Height - HeaderHeight);

			foreach (var child in Children)
				child.Draw( wr );

			Game.Renderer.DisableScissor();
		}

		public override int2 ChildOrigin { get { return RenderOrigin + new int2(0, (int)ListOffset); } }

		public override Rectangle GetEventBounds()
		{
			return EventBounds;
		}
		
		public override void Tick ()
		{
			if (UpPressed && ListOffset <= 0) ListOffset += ScrollVelocity;
			if (DownPressed) ListOffset -= ScrollVelocity;
		}
		
		public override bool LoseFocus (MouseInput mi)
		{
			UpPressed = DownPressed = false;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleInputInner(MouseInput mi)
		{						
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;
			
			if (!Focused)
				return false;

			UpPressed = upButtonRect.Contains(mi.Location.X,mi.Location.Y);
			DownPressed = downButtonRect.Contains(mi.Location.X,mi.Location.Y);
			
			if (Focused && mi.Event == MouseInputEvent.Up)
				LoseFocus(mi);
			
			return (UpPressed || DownPressed);
		}

		public override Widget Clone() { return new ScrollPanelWidget(this); }
	}
}