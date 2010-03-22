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

namespace OpenRA.Widgets
{
	class LabelWidget : Widget
	{
		public string Text = "";
		public string Align = "Left";
		
		public override void Draw()
		{		
			if (!Visible)
			{
				base.Draw();
				return;
			}
		
			Rectangle r = Bounds;
			Game.chrome.renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			
			int2 bounds = Game.chrome.renderer.BoldFont.Measure(Text);
			int2 position = new int2(Bounds.X,Bounds.Y);
			
			if (Align == "Center")
				position = new int2(Bounds.X+Bounds.Width/2, Bounds.Y+Bounds.Height/2) - new int2(bounds.X / 2, bounds.Y/2);
			
			
			Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, Text, position, Color.White);
			Game.chrome.renderer.Device.DisableScissor();
			base.Draw();
		}
	}
}