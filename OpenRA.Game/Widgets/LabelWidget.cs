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
	class LabelWidget : Widget
	{
		public string Text = "";
		public string Align = "Left";
		public bool Bold = true;
		public Func<string> GetText;

		public LabelWidget()
			: base()
		{
			GetText = () => { return Text; };
		}

		public override void Draw(World world)
		{		
			if (!IsVisible())
			{
				base.Draw(world);
				return;
			}
			
			var font = (Bold) ? Game.chrome.renderer.BoldFont : Game.chrome.renderer.RegularFont;
			var text = GetText();
			int2 textSize = font.Measure(text);
			int2 position = new int2(Parent.Bounds.X + Bounds.X,Parent.Bounds.Y + Bounds.Y);
			
			if (Align == "Center")
				position = new int2(Bounds.X + Parent.Bounds.X +Bounds.Width/2, Bounds.Y + Parent.Bounds.Y + Bounds.Height/2) 
					- new int2(textSize.X / 2, textSize.Y/2);
			
			font.DrawText(text, position, Color.White);
			base.Draw(world);
		}
	}
}