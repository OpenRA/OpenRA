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
		MapStub Map = null;
		public MapChooserDelegate()
		{
			var r = Chrome.rootWidget;
			var bg = r.GetWidget("MAP_CHOOSER");
			bg.SpecialOneArg = (map) => RefreshMapList(map);
			var ml = bg.GetWidget<ListBoxWidget>("MAP_LIST");
			
			bg.GetWidget<MapPreviewWidget>("MAPCHOOSER_MAP_PREVIEW").Map = () => {return Map;};
			bg.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => {return Map.Title;};
			bg.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => {return "{0}x{1}".F(Map.Width, Map.Height);};
			bg.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => {return Rules.TileSets[Map.Tileset].Name;};
			bg.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => {return Map.PlayerCount.ToString();};
			
			bg.GetWidget("BUTTON_OK").OnMouseUp = mi => {
				Game.IssueOrder(Order.Command("map " + Map.Uid));
				r.CloseWindow();
				return true;
			};
			
			bg.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
			
			var itemTemplate = ml.GetWidget<LabelWidget>("MAP_TEMPLATE");
			int offset = itemTemplate.Bounds.Y;
			foreach (var kv in Game.AvailableMaps)
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;
				
				var template = itemTemplate.Clone() as LabelWidget;
				template.Id = "MAP_{0}".F(map.Uid);
				template.GetText = () => "   "+map.Title;
				template.GetBackground = () => ((Map == map) ? "dialog2" : null);
				template.OnMouseDown = mi => {Map = map; return true;};
				template.Parent = ml;			
				
				template.Bounds = new Rectangle(template.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				ml.AddChild(template);
				
				offset += template.Bounds.Height;
				ml.ContentHeight += template.Bounds.Height;
			}
		}
		
		public void RefreshMapList(object uidobj)
		{
			// Set the default selected map
			var uid = uidobj as string;
			if (uid != null)
				Map = Game.AvailableMaps[ uid ];
			else
				Map = Game.AvailableMaps.FirstOrDefault().Value;
		}
	}
}
