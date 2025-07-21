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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapToolsLogic : ChromeLogic
	{
		readonly List<Widget> toolPanels = [];
		readonly Dictionary<Widget, string> toolLabels = [];
		Widget selectedPanel;

		[ObjectCreator.UseCtor]
		public MapToolsLogic(Widget widget, World world)
		{
			var toolDropdownWidget = widget.Get<DropDownButtonWidget>("TOOLS_DROPDOWN");
			var tools = world.WorldActor.TraitsImplementing<IEditorTool>();
			foreach (var tool in tools)
			{
				if (!tool.IsEnabled)
					continue;

				var panel = Game.LoadWidget(world, tool.PanelWidget, widget, new WidgetArgs() { { "tool", tool } });
				toolPanels.Add(panel);
				toolLabels.Add(panel, FluentProvider.GetMessage(tool.Label));
			}

			SelectTool(toolPanels.FirstOrDefault());
			toolDropdownWidget.OnMouseDown = _ => ShowToolsDropDown(toolDropdownWidget);
			toolDropdownWidget.GetText = () => toolLabels[selectedPanel];
			if (toolPanels.Count == 1)
				toolDropdownWidget.Disabled = true;
		}

		void ShowToolsDropDown(DropDownButtonWidget dropdown)
		{
			ScrollItemWidget SetupItem(Widget panel, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => selectedPanel == panel,
					() => SelectTool(panel));

				item.Get<LabelWidget>("LABEL").GetText = () => toolLabels[panel];

				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, toolPanels, SetupItem);
		}

		void SelectTool(Widget panel)
		{
			if (panel != selectedPanel && selectedPanel != null)
				selectedPanel.Visible = false;

			selectedPanel = panel;
			if (panel != null)
				selectedPanel.Visible = true;
		}
	}
}
