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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class MapChooserDelegate : IWidgetDelegate
	{	
		public MapChooserDelegate()
		{
			var r = Chrome.rootWidget;
			var bg = r.GetWidget("MAP_CHOOSER");
			
			bg.GetWidget<MapPreviewWidget>("MAPCHOOSER_MAP_PREVIEW").Map = () => {return Game.chrome.currentMap;};
			bg.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => {return Game.chrome.currentMap.Title;};
			bg.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => {return "{0}x{1}".F(Game.chrome.currentMap.Width, Game.chrome.currentMap.Height);};
			bg.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => {return Rules.TileSets[Game.chrome.currentMap.Tileset].Name;};
			bg.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => {return Game.chrome.currentMap.PlayerCount.ToString();};
			
			bg.GetWidget("BUTTON_OK").OnMouseUp = mi => {
				Game.IssueOrder(Order.Chat("/map " + Game.chrome.currentMap.Uid));
				r.CloseWindow();
				return true;
			};
			
			bg.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
		}
	}
}
