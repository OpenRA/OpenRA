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

using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	class LabelWidget : Widget
	{
		public enum TextAlign { Left, Center, Right }
		
		public string Text = null;
		public string Background = null;
		public TextAlign Align = TextAlign.Left;
		public bool Bold = false;
		public Func<string> GetText;
		public Func<string> GetBackground;
		
		public LabelWidget()
			: base()
		{
			GetText = () => { return Text; };
			GetBackground = () => { return Background; };
		}

		protected LabelWidget(LabelWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			Bold = other.Bold;
			GetText = other.GetText;
			GetBackground = other.GetBackground;
		}

		public override void DrawInner(World world)
		{		
			var bg = GetBackground();

			if (bg != null)
				WidgetUtils.DrawPanel(bg, RenderBounds );
						
			var font = (Bold) ? Game.chrome.renderer.BoldFont : Game.chrome.renderer.RegularFont;
			var text = GetText();
			if (text == null)
				return;
			
			int2 textSize = font.Measure(text);
			int2 position = RenderOrigin + new int2(0, (Bounds.Height - textSize.Y)/2);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X)/2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X,0); 

			font.DrawText(text, position, Color.White);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}