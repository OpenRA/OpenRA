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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorTabsLogic : ChromeLogic
	{
		readonly Widget widget;

		protected enum MenuType { Tiles, Layers, Actors, History }
		protected MenuType menuType = MenuType.Tiles;
		readonly Widget tabContainer;

		[ObjectCreator.UseCtor]
		public MapEditorTabsLogic(Widget widget)
		{
			this.widget = widget;
			tabContainer = widget.Get("MAP_EDITOR_TAB_CONTAINER");

			SetupTab("TILES_TAB", "TILE_WIDGETS", MenuType.Tiles);
			SetupTab("OVERLAYS_TAB", "LAYER_WIDGETS", MenuType.Layers);
			SetupTab("ACTORS_TAB", "ACTOR_WIDGETS", MenuType.Actors);
			SetupTab("HISTORY_TAB", "HISTORY_WIDGETS", MenuType.History);
		}

		void SetupTab(string buttonId, string tabId, MenuType tabType)
		{
			var tab = tabContainer.Get<ButtonWidget>(buttonId);
			tab.IsHighlighted = () => menuType == tabType;
			tab.OnClick = () => { menuType = tabType; };

			var container = widget.Parent.Get<ContainerWidget>(tabId);
			container.IsVisible = () => menuType == tabType;
		}
	}
}
