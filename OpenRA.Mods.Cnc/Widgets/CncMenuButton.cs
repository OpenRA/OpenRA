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
using OpenRA.Widgets;
using OpenRA.Graphics;
using System.Drawing;
namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncMenuButtonWidget : ButtonWidget
	{
		public Func<bool> IsDisabled = () => false;
		public Action OnClick = () => {};
		public Renderer.FontType Font = Renderer.FontType.Bold;
		
		public CncMenuButtonWidget()
			: base()
		{
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
		}
		
		protected CncMenuButtonWidget(CncMenuButtonWidget other)
			: base(other)
		{
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
			Font = other.Font;
		}
		
		public override int2 ChildOrigin { get { return RenderOrigin; } }
		public override void DrawInner()
		{
			var rb = RenderBounds;
			var font = Game.Renderer.Fonts[Font];
			var state = IsDisabled() ? "button-disabled" : 
						Depressed ? "button-pressed" : 
						rb.Contains(Viewport.LastMousePos) ? "button-hover" : 
						"button";
			
			WidgetUtils.DrawPanel(state, rb);
			var text = GetText();

			font.DrawText(text,
				new int2(rb.X + UsableWidth / 2, rb.Y + Bounds.Height / 2)
					- new int2(font.Measure(text).X / 2,
				font.Measure(text).Y / 2), IsDisabled() ? Color.Gray : Color.White);
		}
		public override Widget Clone() { return new CncMenuButtonWidget(this); }
	}
	
	public class CncCheckboxWidget : CncMenuButtonWidget
	{
		public CncCheckboxWidget()
			: base() { }
		protected CncCheckboxWidget(CncCheckboxWidget widget)
			: base(widget) { }
		
		public Func<bool> IsChecked = () => false;
		public int baseLine = 1;

		public override void DrawInner()
		{
			var state = IsDisabled() ? "button-disabled" : 
				Depressed ? "button-pressed" : 
				RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : 
				"button";
			
			var font = Game.Renderer.BoldFont;
			var rect = RenderBounds;
			WidgetUtils.DrawPanel(state, new Rectangle(rect.Location, new Size(Bounds.Height, Bounds.Height)));

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, RenderOrigin.Y - baseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if (IsChecked())
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("checkbox", "checked"),
					new float2(rect.Left + 2, rect.Top + 2));
		}
		public override Widget Clone() { return new CncCheckboxWidget(this); }
	}
	
	public class CncScrollPanelWidget : ScrollPanelWidget
	{
		public CncScrollPanelWidget()
			: base() { }
		
		protected CncScrollPanelWidget(CncScrollPanelWidget widget)
			: base(widget) { }
		
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
			
			backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width - ScrollbarWidth + 1, rb.Height);
			upButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y + ScrollbarWidth - 1, ScrollbarWidth, ScrollbarHeight + 2);
			thumbRect = new Rectangle(rb.Right - ScrollbarWidth, thumbOrigin, ScrollbarWidth, thumbHeight);
			
			string upButtonBg = (thumbHeight == 0 || ListOffset >= 0) ? "button-disabled" :
					UpPressed ? "button-pressed" : 
					upButtonRect.Contains(Viewport.LastMousePos) ? "button-hover" : "button";
			
			string downButtonBg = (thumbHeight == 0 || ListOffset <= Bounds.Height - ContentHeight) ? "button-disabled" :
					DownPressed ? "button-pressed" : 
					downButtonRect.Contains(Viewport.LastMousePos) ? "button-hover" : "button";
			
			string scrollbarBg = "panel-gray";
			string thumbBg = (Focused && thumbRect.Contains(Viewport.LastMousePos)) ? "button-pressed" : 
					thumbRect.Contains(Viewport.LastMousePos) ? "button-hover" : "button";

			WidgetUtils.DrawPanel(scrollbarBg, scrollbarRect);
			WidgetUtils.DrawPanel("panel-gray", backgroundRect);
			WidgetUtils.DrawPanel(upButtonBg, upButtonRect);
			WidgetUtils.DrawPanel(downButtonBg, downButtonRect);
			
			if (thumbHeight > 0)
				WidgetUtils.DrawPanel(thumbBg, thumbRect);

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "up_arrow"),
				new float2(upButtonRect.Left + 4, upButtonRect.Top + 4));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", "down_arrow"),
				new float2(downButtonRect.Left + 4, downButtonRect.Top + 4));

			Game.Renderer.EnableScissor(backgroundRect.X + 1, backgroundRect.Y + 1, backgroundRect.Width - 2, backgroundRect.Height - 2);

			foreach (var child in Children)
				child.Draw();

			Game.Renderer.DisableScissor();
		}
		
		public void ScrollToBottom()
		{
			ListOffset = Math.Min(0,Bounds.Height - ContentHeight);
		}
		public override Widget Clone() { return new CncScrollPanelWidget(this); }

	}
}

