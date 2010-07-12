using System.Drawing;
#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRA.Widgets
{
	class ListBoxWidget : Widget
	{
		public readonly string Background = "dialog";
		public readonly int ScrollbarWidth = 24;
		public readonly float ScrollVelocity = 4f;
		public readonly int HeaderHeight = 25;
		
		float ListOffset = 0;
		bool UpPressed = false;
		bool DownPressed = false;
		Rectangle upButtonRect;
		Rectangle downButtonRect;
		Rectangle backgroundRect;
		Rectangle scrollbarRect;
		
		public ListBoxWidget() : base() {}
		public ListBoxWidget(Widget other)
			: base(other)
		{
			Background = (other as ListBoxWidget).Background;
			upButtonRect = (other as ListBoxWidget).upButtonRect;
			downButtonRect = (other as ListBoxWidget).downButtonRect;
			scrollbarRect = (other as ListBoxWidget).scrollbarRect;
			backgroundRect = (other as ListBoxWidget).backgroundRect;
			
			UpPressed = (other as ListBoxWidget).UpPressed;	
			DownPressed = (other as ListBoxWidget).DownPressed;
		}
		
		public override void DrawInner(World world) {}
		public override void Draw(World world)
		{
			if (!IsVisible())
				return;
			
			backgroundRect = new Rectangle(RenderBounds.X, RenderBounds.Y, RenderBounds.Width - ScrollbarWidth, RenderBounds.Height);
			upButtonRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(RenderBounds.Right - ScrollbarWidth, RenderBounds.Y + ScrollbarWidth, ScrollbarWidth, RenderBounds.Height - 2*ScrollbarWidth);

			string upButtonBg = (UpPressed) ? "dialog3" : "dialog2";
			string downButtonBg = (DownPressed) ? "dialog3" : "dialog2";
			string scrolbarBg = "dialog3";
			string scrollbarButton = "dialog2";
			
			WidgetUtils.DrawPanel(Background, backgroundRect);
			WidgetUtils.DrawPanel(upButtonBg, upButtonRect);
			WidgetUtils.DrawPanel(downButtonBg, downButtonRect);
			WidgetUtils.DrawPanel(scrolbarBg, scrollbarRect);
			
			
			Game.chrome.renderer.Device.EnableScissor(backgroundRect.X, backgroundRect.Y + HeaderHeight, backgroundRect.Width, backgroundRect.Height - HeaderHeight);

			foreach (var child in Children)
					child.Draw(world);
			
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
			Game.chrome.renderer.Device.DisableScissor();
		}
		public override int2 ChildOrigin { get { return RenderOrigin + new int2(0, (int)ListOffset); } }

		public override void Tick (World world)
		{
			if (UpPressed && ListOffset <= 0) ListOffset += ScrollVelocity;
			if (DownPressed) ListOffset -= ScrollVelocity;
		}
		
		public override bool LoseFocus (MouseInput mi)
		{
			UpPressed = DownPressed = false;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleInput(MouseInput mi)
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

		public override Widget Clone() { return new ListBoxWidget(this); }
	}
}