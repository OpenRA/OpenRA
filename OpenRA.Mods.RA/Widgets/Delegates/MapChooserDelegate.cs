#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class MapChooserDelegate : IWidgetDelegate
	{
		MapStub Map = null;

		[ObjectCreator.UseCtor]
		internal MapChooserDelegate(
			[ObjectCreator.Param( "widget" )] Widget bg,
			[ObjectCreator.Param] OrderManager orderManager,
			[ObjectCreator.Param] string mapName )
		{
			if (mapName != null)
				Map = Game.modData.AvailableMaps[mapName];
			else
				Map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;

			var ml = bg.GetWidget<ScrollPanelWidget>("MAP_LIST");
			bg.GetWidget<MapPreviewWidget>("MAPCHOOSER_MAP_PREVIEW").Map = () => Map;
			bg.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => Map.Title;
			bg.GetWidget<LabelWidget>("CURMAP_AUTHOR").GetText = () => Map.Author;
			bg.GetWidget<LabelWidget>("CURMAP_DESC").GetText = () => Map.Description;
			bg.GetWidget<LabelWidget>("CURMAP_DESC_LABEL").IsVisible = () => Map.Description != null;
			bg.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => "{0}x{1}".F(Map.Bounds.Width, Map.Bounds.Height);
			bg.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => Rules.TileSets[Map.Tileset].Name;
			bg.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => Map.PlayerCount.ToString();

			bg.GetWidget("BUTTON_OK").OnMouseUp = mi =>
			{
				orderManager.IssueOrder(Order.Command("map " + Map.Uid));
				Widget.CloseWindow();
				return true;
			};

			bg.GetWidget("BUTTON_CANCEL").OnMouseUp = mi =>
			{
				Widget.CloseWindow();
				return true;
			};

			var itemTemplate = ml.GetWidget<ContainerWidget>("MAP_TEMPLATE");
			foreach (var kv in Game.modData.AvailableMaps.OrderBy(kv => kv.Value.Title))
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;

				var template = itemTemplate.Clone() as ContainerWidget;
				template.Id = "MAP_{0}".F(map.Uid);
				template.GetBackground = () => ((Map == map) ? "dialog2" : null);
				template.OnMouseDown = mi => { Map = map; return true; };
				template.IsVisible = () => true;
				template.GetWidget<LabelWidget>("TITLE").GetText = () => "   " + map.Title;
				template.GetWidget<LabelWidget>("TYPE").GetText = () => map.Type + "   ";
				ml.AddChild(template);
			}
		}
	}
}
