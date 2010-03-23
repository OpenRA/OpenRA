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
	class ButtonWidget : Widget
	{
		public string Text = "";
		public bool Depressed = false;
		public int VisualHeight = 1;
		public override bool HandleInput(MouseInput mi)
		{
			if (Game.chrome.selectedWidget == this)
				Depressed = (GetEventBounds().Contains(mi.Location.X,mi.Location.Y)) ? true : false;
			
			// Relinquish focus
			if (Game.chrome.selectedWidget == this && mi.Event == MouseInputEvent.Up)
			{
				Game.chrome.selectedWidget = null;
				Depressed = false;
			}
			
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return base.HandleInput(mi);
			
			
			if (base.HandleInput(mi))
				return true;
			
			// Give button focus only while the mouse is down
			// This is a bit of a hack: it will become cleaner soonish
			// It will also steal events from any potential children
			// We also want to play a click sound
			if (mi.Event == MouseInputEvent.Down)
			{
				Game.chrome.selectedWidget = this;
				Depressed = true;
				return true;
			}
			
			return false;
		}

		public override void Draw()
		{
			if (!Visible)
			{
				base.Draw();
				return;
			}

			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			WidgetUtils.DrawPanel(Depressed ? "dialog3" : "dialog2", Bounds,
				() => Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, Text,
				new int2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2)
					- new int2(Game.chrome.renderer.BoldFont.Measure(Text).X / 2,
				Game.chrome.renderer.BoldFont.Measure(Text).Y / 2) + stateOffset, Color.White));

			base.Draw();
		}
	}
}