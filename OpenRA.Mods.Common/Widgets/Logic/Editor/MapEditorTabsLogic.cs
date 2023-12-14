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
		readonly EditorViewportControllerWidget editor;

		protected enum MenuType { Select, Tiles, Layers, Actors, Tools, History }
		protected MenuType menuType = MenuType.Tiles;
		readonly Widget tabContainer;

		MenuType lastSelectedTab = MenuType.Tiles;

		[ObjectCreator.UseCtor]
		public MapEditorTabsLogic(Widget widget)
		{
			this.widget = widget;
			editor = widget.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;

			tabContainer = widget.Get("MAP_EDITOR_TAB_CONTAINER");

			SetupTab(null, "SELECT_WIDGETS", MenuType.Select);
			SetupTab("TILES_TAB", "TILE_WIDGETS", MenuType.Tiles);
			SetupTab("OVERLAYS_TAB", "LAYER_WIDGETS", MenuType.Layers);
			SetupTab("ACTORS_TAB", "ACTOR_WIDGETS", MenuType.Actors);
			SetupTab("TOOLS_TAB", "TOOLS_WIDGETS", MenuType.Tools);
			SetupTab("HISTORY_TAB", "HISTORY_WIDGETS", MenuType.History);
		}

		protected override void Dispose(bool disposing)
		{
			editor.DefaultBrush.SelectionChanged -= HandleSelectionChanged;

			base.Dispose(disposing);
		}

		void SetupTab(string buttonId, string tabId, MenuType tabType)
		{
			if (buttonId != null)
			{
				var tab = tabContainer.Get<ButtonWidget>(buttonId);
				tab.IsHighlighted = () => menuType == tabType;
				tab.OnClick = () => menuType = SelectTab(tabType);
			}

			var container = widget.Parent.Get<ContainerWidget>(tabId);
			container.IsVisible = () => menuType == tabType;
		}

		MenuType SelectTab(MenuType newMenuType)
		{
			if (menuType == MenuType.Select)
			{
				editor.SetBrush(editor.DefaultBrush);
				editor.DefaultBrush.ClearSelection();
			}

			if (newMenuType != MenuType.Select)
				lastSelectedTab = newMenuType;

			return newMenuType;
		}

		void HandleSelectionChanged()
		{
			var actor = editor.DefaultBrush.Selection.Actor;
			var area = editor.DefaultBrush.Selection.Area;

			if (menuType != MenuType.Select && (actor != null || area != null))
				menuType = MenuType.Select;
			else if (menuType == MenuType.Select && actor == null && area == null)
				menuType = lastSelectedTab;
		}
	}
}
