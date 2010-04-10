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
namespace OpenRA.Widgets.Delegates
{
	public class MusicPlayerDelegate : IWidgetDelegate
	{
		public MusicPlayerDelegate()
		{
			var bg = Chrome.rootWidget.GetWidget("MUSIC_BG");
			bg.GetWidget("BUTTON_PLAY").OnMouseUp = mi => {
				Sound.MusicPaused = false;
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;
				return true;
			};
			bg.GetWidget("BUTTON_PAUSE").OnMouseUp = mi => {
				Sound.MusicPaused = true;
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
				return true;
			};
			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi => {
				Sound.MusicStopped = true;
				bg.Visible = false;
				return true;
			};
		}
	}
}
