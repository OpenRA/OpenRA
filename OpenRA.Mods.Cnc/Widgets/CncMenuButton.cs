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
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncCheckboxWidget : ButtonWidget
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
			
			var font = Game.Renderer.Fonts[Font];
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
			: base()
		{
			Background = "panel-gray";
		}
		
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
			WidgetUtils.DrawPanel(Background, backgroundRect);
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

	public class CncTextFieldWidget : TextFieldWidget
	{
		public CncTextFieldWidget()
			: base() { }
		protected CncTextFieldWidget(CncTextFieldWidget widget)
			: base(widget) { }

		public Func<bool> IsDisabled = () => false;
		public Color TextColor = Color.White;
		public Color DisabledColor = Color.Gray;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IsDisabled())
				return false;
			return base.HandleMouseInput(mi);
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (IsDisabled())
				return false;
			return base.HandleKeyPressInner(e);
		}

		public override void DrawWithString(string text)
		{
			if (text == null) text = "";

			var font = (Bold) ? Game.Renderer.Fonts["Bold"] : Game.Renderer.Fonts["Regular"];
			var pos = RenderOrigin;

			if (CursorPosition > text.Length)
				CursorPosition = text.Length;

			var textSize = font.Measure(text);
			var cursorPosition = font.Measure(text.Substring(0,CursorPosition));

			var disabled = IsDisabled();
			var state = disabled ? "button-disabled" :
				Focused ? "button-pressed" :
				RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" :
				"button";

			WidgetUtils.DrawPanel(state,
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(LeftMargin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - LeftMargin - RightMargin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + LeftMargin, pos.Y, Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom);
			}

			var color = disabled ? DisabledColor : TextColor;

			font.DrawText(text, textPos, color);

			if (showCursor && Focused)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), color);

			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new CncTextFieldWidget(this); }
	}
	
	public class CncSliderWidget : SliderWidget
	{
		public Func<bool> IsDisabled = () => false;
		
		public CncSliderWidget() : base() { } 
		public CncSliderWidget(CncSliderWidget other) : base(other) { } 
		
		public override Widget Clone() { return new CncSliderWidget(this); }
		public override void DrawInner()
		{
			if (!IsVisible())
				return;
			
			var tr = thumbRect;
			var trackWidth = RenderBounds.Width - tr.Width;
			var trackOrigin = RenderBounds.X + tr.Width / 2;
			var trackRect = new Rectangle(trackOrigin - 1, RenderBounds.Y + (RenderBounds.Height - TrackHeight) / 2, trackWidth + 2, TrackHeight);

			// Tickmarks (hacked until we have real art)
			for (int i = 0; i < Ticks; i++)
			{
				var tickRect = new Rectangle(trackOrigin - 1 + (int)(i * trackWidth * 1f / (Ticks - 1)),
						  RenderBounds.Y + RenderBounds.Height / 2, 2, RenderBounds.Height / 2);
				WidgetUtils.DrawPanel("panel-gray", tickRect);
			}
			
			// Track
			WidgetUtils.DrawPanel("panel-gray", trackRect);

			// Thumb
			var state = IsDisabled() ? "button-disabled" : 
				isMoving ? "button-pressed" : 
				tr.Contains(Viewport.LastMousePos) ? "button-hover" : 
				"button";
			
			WidgetUtils.DrawPanel(state, tr);
		}
	}
	
	public class CncDropDownButtonWidget : DropDownButtonWidget
	{
		public Func<bool> IsDisabled = () => false;
		public string Font = "Bold";

		public CncDropDownButtonWidget() : base() { }
		protected CncDropDownButtonWidget(CncDropDownButtonWidget other) : base(other)
		{
			Font = other.Font;
		}
		public override Widget Clone() { return new CncDropDownButtonWidget(this); }

		public override void DrawInner()
		{
			var rb = RenderBounds;
			var state = IsDisabled() ? "button-disabled" : 
						Depressed ? "button-pressed" : 
						rb.Contains(Viewport.LastMousePos) ? "button-hover" : 
						"button";
			
			WidgetUtils.DrawPanel(state, rb);
			
			var font = Game.Renderer.Fonts[Font];
			var text = GetText();
			font.DrawText(text,
				new int2(rb.X + UsableWidth / 2, rb.Y + Bounds.Height / 2)
					- new int2(font.Measure(text).X / 2,
				font.Measure(text).Y / 2), IsDisabled() ? Color.Gray : Color.White);
			
			var image = ChromeProvider.GetImage("scrollbar", "down_arrow");
			WidgetUtils.DrawRGBA( image, new float2(rb.Right - rb.Height + 4, 
			                                        rb.Top + (rb.Height - image.bounds.Height) / 2));

			WidgetUtils.FillRectWithColor(new Rectangle(rb.Right - rb.Height, rb.Top + 3, 1, rb.Height - 6),
				Color.White);
		}
		
		public static new void ShowDropDown<T>(Widget w, IEnumerable<T> ts, Func<T, int, LabelWidget> ft)
		{
			var dropDown = new CncScrollPanelWidget();
			dropDown.Bounds = new Rectangle(w.RenderOrigin.X, w.RenderOrigin.Y + w.Bounds.Height, w.Bounds.Width, 100);
			dropDown.ItemSpacing = 1;
			dropDown.Background = "panel-black";

			List<LabelWidget> items = new List<LabelWidget>();
			List<Widget> dismissAfter = new List<Widget>();
			foreach (var t in ts)
			{
				var ww = ft(t, dropDown.Bounds.Width - dropDown.ScrollbarWidth);
				dismissAfter.Add(ww);
				ww.OnMouseMove = mi => items.Do(lw =>
				{
					lw.Background = null;
					ww.Background = "button-hover";
				});
	
				dropDown.AddChild(ww);
				items.Add(ww);
			}
			
			dropDown.Bounds.Height = Math.Min(150, dropDown.ContentHeight);
			ShowDropPanel(w, dropDown, dismissAfter, () => true);
		}
	}
	
	public class ScrollItemWidget : ButtonWidget
	{
		public ScrollItemWidget()
			: base()
		{
			IsVisible = () => false;
		}
		
		protected ScrollItemWidget(ScrollItemWidget other)
			: base(other)
		{
			IsVisible = () => false;
		}
		
		public Func<bool> IsSelected = () => false;

		public override void DrawInner()
		{
			var state = IsSelected() ? "button-pressed" : 
				RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : 
				null;
			
			if (state != null)
				WidgetUtils.DrawPanel(state, RenderBounds);
		}
		
		public override Widget Clone() { return new ScrollItemWidget(this); }
				
		public static ScrollItemWidget Setup(ScrollItemWidget template, Func<bool> isSelected, Action onClick)
		{
			var w = template.Clone() as ScrollItemWidget;
			w.IsVisible = () => true;
			w.IsSelected = isSelected;
			w.OnClick = onClick;
			return w;
		}
	}
}

