#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;
using System.IO;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapChooserLogic
	{
		Map Map = null;
		Widget scrollpanel;
		ScrollItemWidget itemTemplate;
		
		[ObjectCreator.UseCtor]
		internal MapChooserLogic(
			[ObjectCreator.Param( "widget" )] Widget bg,
			[ObjectCreator.Param] OrderManager orderManager,
			[ObjectCreator.Param] string mapName )
		{
			if (mapName != null)
				Map = Game.modData.AvailableMaps[mapName];
			else
				Map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;

			bg.GetWidget<MapPreviewWidget>("MAPCHOOSER_MAP_PREVIEW").Map = () => Map;
			bg.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => Map.Title;
			bg.GetWidget<LabelWidget>("CURMAP_AUTHOR").GetText = () => Map.Author;
			bg.GetWidget<LabelWidget>("CURMAP_DESC").GetText = () => Map.Description;
			bg.GetWidget<LabelWidget>("CURMAP_DESC_LABEL").IsVisible = () => Map.Description != null;
			bg.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => "{0}x{1}".F(Map.Bounds.Width, Map.Bounds.Height);
			bg.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => Rules.TileSets[Map.Tileset].Name;
			bg.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => Map.PlayerCount.ToString();

			bg.GetWidget<ButtonWidget>("BUTTON_OK").OnMouseUp = mi =>
			{
				orderManager.IssueOrder(Order.Command("map " + Map.Uid));
				Widget.CloseWindow();
			};

			bg.GetWidget<ButtonWidget>("BUTTON_CANCEL").OnMouseUp = mi => Widget.CloseWindow();
			
			scrollpanel = bg.GetWidget<ScrollPanelWidget>("MAP_LIST");
			itemTemplate = scrollpanel.GetWidget<ScrollItemWidget>("MAP_TEMPLATE");
			EnumerateMaps();
		}
		
		void EnumerateMaps()
		{
			scrollpanel.RemoveChildren();
			foreach (var kv in Game.modData.AvailableMaps.OrderBy(kv => kv.Value.Title).OrderBy(kv => kv.Value.PlayerCount))
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;
				
				var item = ScrollItemWidget.Setup(itemTemplate, () => Map == map, () => Map = map);
				item.GetWidget<LabelWidget>("TITLE").GetText = () => map.Title;
				item.GetWidget<LabelWidget>("PLAYERS").GetText = () => "{0}".F(map.PlayerCount);
				item.GetWidget<LabelWidget>("TYPE").GetText = () => map.Type;
				scrollpanel.AddChild(item);
			}
		}
	}
}
