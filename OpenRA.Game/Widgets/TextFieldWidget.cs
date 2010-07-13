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

using System.Drawing;
using System;

namespace OpenRA.Widgets
{
	class TextFieldWidget : Widget
	{
		public string Text = "";
		public int MaxLength = 0;
		public bool Bold = false;
		public int VisualHeight = 1;
		public Func<bool> OnEnterKey = () => {return false;};
		public Func<bool> OnTabKey = () => {return false;};
		public Action OnLoseFocus = () => {};

		public TextFieldWidget()
			: base()
		{
		}
		
		public TextFieldWidget(Widget widget)
			:base(widget)
		{
			Text = (widget as TextFieldWidget).Text;
			MaxLength = (widget as TextFieldWidget).MaxLength;
			Bold = (widget as TextFieldWidget).Bold;
			VisualHeight = (widget as TextFieldWidget).VisualHeight;
		}
		
		public override bool LoseFocus(MouseInput mi)
		{
			OnLoseFocus();
			var lose = base.LoseFocus(mi);
			return lose;
		}

		public override bool HandleInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;
			
			// Lose focus if they click outside the box; return false so the next widget can grab this event
			if (mi.Event == MouseInputEvent.Down && !RenderBounds.Contains(mi.Location.X,mi.Location.Y) && LoseFocus(mi))
				return false;
			
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;
			
			blinkCycle = 10;
			showCursor = true;
			return true;
		}
		
		public override bool HandleKeyPress(System.Windows.Forms.KeyPressEventArgs e, Modifiers modifiers)
		{
			// Only take input if we are focused
			if (!Focused)
				return false;
			
			if (e.KeyChar == '\r' && OnEnterKey())
				return true;
			
			if (e.KeyChar == '\t' && OnTabKey())
				return true;
			
			TypeChar(e.KeyChar);
			return true;
		}
		
		public void TypeChar(char c)
		{
			if (c == '\b' || c == 0x7f)
			{
				if (Text.Length > 0)
					Text = Text.Remove(Text.Length - 1);
			}
			else if (!char.IsControl(c))
			{
				if (MaxLength > 0 && Text.Length >= MaxLength)
					return;
				
				Text += c;
			}
		}
		
		int blinkCycle = 10;
		bool showCursor = true;
		public override void Tick(World world)
		{
			if (--blinkCycle <= 0)
			{
				blinkCycle = 20;
				showCursor ^= true;
			}
			base.Tick(world);
		}
		
		public override void DrawInner(World world)
		{
			int margin = 5;
			var font = (Bold) ? Game.chrome.renderer.BoldFont : Game.chrome.renderer.RegularFont;
			var cursor = (showCursor && Focused) ? "|" : "";
			var textSize = font.Measure(Text + "|");
			var pos = RenderOrigin;
			
			WidgetUtils.DrawPanel("dialog3", 
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height ) );
			
			// Inset text by the margin and center vertically
			var textPos = pos + new int2( margin, (Bounds.Height - textSize.Y)/2 - VisualHeight);
		
			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - 2*margin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - 2*margin - textSize.X,0);
				
				Game.chrome.renderer.Device.EnableScissor(pos.X + margin, pos.Y, Bounds.Width - 2*margin, Bounds.Bottom);
			}
			
			font.DrawText(Text + cursor, textPos, Color.White);
			
			if (textSize.X > Bounds.Width - 2*margin)
			{
				Game.chrome.renderer.RgbaSpriteRenderer.Flush();
				Game.chrome.renderer.Device.DisableScissor();
			}
		}
		
		public override Widget Clone()
		{	
			return new TextFieldWidget(this);
		}
	}
}