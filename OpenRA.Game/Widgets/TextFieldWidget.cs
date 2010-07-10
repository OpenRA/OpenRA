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
		string TextBuffer = "zomg text";
		
		public TextFieldWidget()
			: base()
		{
		}
		
		public TextFieldWidget(Widget widget)
			:base(widget)
		{
			TextBuffer = (widget as TextFieldWidget).TextBuffer;
		}

		public override bool HandleInput(MouseInput mi)
		{
			// We get this first if we are focussed; if the click was somewhere else remove focus
			if (Chrome.selectedWidget == this && mi.Event == MouseInputEvent.Down && !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
			{
				Chrome.selectedWidget = null;
				return false;
			}
				
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return base.HandleInput(mi);
			
			
			if (base.HandleInput(mi))
				return true;
			
			if (mi.Event == MouseInputEvent.Down)
			{
				Chrome.selectedWidget = this;
				return true;
			}
			
			return false;
		}
		
		public override bool HandleKeyPress(System.Windows.Forms.KeyPressEventArgs e, Modifiers modifiers)
		{
			if (base.HandleKeyPress(e,modifiers))
				return true;
			
			TypeChar(e.KeyChar);
			return true;
		}
		
		public void TypeChar(char c)
		{
			if (c == '\b' || c == 0x7f)
			{
				if (TextBuffer.Length > 0)
					TextBuffer = TextBuffer.Remove(TextBuffer.Length - 1);
			}
			else if (!char.IsControl(c))
				TextBuffer += c;
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
			var pos = DrawPosition();
			int margin = 10;
			WidgetUtils.DrawPanel("dialog3", 
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height ) );
			
			var text = TextBuffer + ((showCursor && Chrome.selectedWidget == this) ? "|" : "");
			Game.chrome.renderer.BoldFont.DrawText(text,
				new int2( pos.X + margin, pos.Y + Bounds.Height / 2)
					- new int2(0, Game.chrome.renderer.BoldFont.Measure(text).Y / 2),
			    Color.White);
		}
		
		public override Widget Clone()
		{	
			return new TextFieldWidget(this);
		}
	}
}