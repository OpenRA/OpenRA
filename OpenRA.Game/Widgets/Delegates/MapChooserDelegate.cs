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

namespace OpenRA.Widgets.Delegates
{
	public class MapChooserDelegate : IWidgetDelegate
	{
		MapStub Map = null;

		[ObjectCreator.UseCtor]
		internal MapChooserDelegate( [ObjectCreator.Param( "widget" )] Widget bg, [ObjectCreator.Param] OrderManager orderManager )
		{
			bg.SpecialOneArg = (map) => RefreshMapList(map);
			var ml = bg.GetWidget<ListBoxWidget>("MAP_LIST");

			bg.GetWidget<MapPreviewWidget>("MAPCHOOSER_MAP_PREVIEW").Map = () => Map;
			bg.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => Map.Title;
			bg.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => "{0}x{1}".F(Map.Width, Map.Height);
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

			var itemTemplate = ml.GetWidget<LabelWidget>("MAP_TEMPLATE");
			int offset = itemTemplate.Bounds.Y;
			foreach (var kv in Game.modData.AvailableMaps)
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;

				var template = itemTemplate.Clone() as LabelWidget;
				template.Id = "MAP_{0}".F(map.Uid);
				template.GetText = () => "   " + map.Title;
				template.GetBackground = () => ((Map == map) ? "dialog2" : null);
				template.OnMouseDown = mi => { Map = map; return true; };
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
				Map = Game.modData.AvailableMaps[uid];
			else
				Map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;
		}
	}
}
