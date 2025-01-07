#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapToolsLogic : ChromeLogic
	{
		[FluentReference]
		const string MarkerTiles = "label-tool-marker-tiles";
		[FluentReference]
		const string MapGenerator = "label-tool-map-generator";

		enum MapTool
		{
			MarkerTiles,
			MapGenerator
		}

		readonly DropDownButtonWidget toolsDropdown;
		readonly Dictionary<MapTool, string> toolNames = new()
		{
			{ MapTool.MarkerTiles, MarkerTiles },
			{ MapTool.MapGenerator, MapGenerator }
		};

		readonly Dictionary<MapTool, Widget> toolPanels = new();

		MapTool selectedTool = MapTool.MarkerTiles;

		[ObjectCreator.UseCtor]
		public MapToolsLogic(Widget widget, World world, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			toolsDropdown = widget.Get<DropDownButtonWidget>("TOOLS_DROPDOWN");

			var markerToolPanel = widget.Get("MARKER_TOOL_PANEL");
			toolPanels.Add(MapTool.MarkerTiles, markerToolPanel);
			if (world.WorldActor.TraitsImplementing<IMapGenerator>().Any())
			{
				var mapGeneratorToolPanel = widget.GetOrNull("MAP_GENERATOR_TOOL_PANEL");
				if (mapGeneratorToolPanel != null)
					toolPanels.Add(MapTool.MapGenerator, mapGeneratorToolPanel);
			}

			toolsDropdown.OnMouseDown = _ => ShowToolsDropDown(toolsDropdown);
			toolsDropdown.GetText = () => FluentProvider.GetMessage(toolNames[selectedTool]);
			if (toolPanels.Count <= 1)
				toolsDropdown.Disabled = true;
		}

		void ShowToolsDropDown(DropDownButtonWidget dropdown)
		{
			ScrollItemWidget SetupItem(MapTool tool, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => selectedTool == tool,
					() => SelectTool(tool));

				item.Get<LabelWidget>("LABEL").GetText = () => FluentProvider.GetMessage(toolNames[tool]);

				return item;
			}

			var options = new[] { MapTool.MarkerTiles, MapTool.MapGenerator };
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, SetupItem);
		}

		void SelectTool(MapTool tool)
		{
			if (tool != selectedTool)
			{
				var currentToolPanel = toolPanels[selectedTool];
				currentToolPanel.Visible = false;
			}

			selectedTool = tool;

			var toolPanel = toolPanels[selectedTool];
			toolPanel.Visible = true;
		}
	}
}
