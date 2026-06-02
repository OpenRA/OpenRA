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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorTabsLogic : ChromeLogic
	{
		public enum MenuType { Select, Tiles, Layers, Actors, Tools, History }

		readonly World world;
		readonly Widget panelContainer;
		readonly Widget tabContainer;
		readonly EditorViewportControllerWidget editor;

		MenuType menuType = MenuType.Tiles;
		MenuType lastSelectedTab = MenuType.Tiles;

		static MapEditorTabsLogic instance;

		public static event Action OnTabChanged;

		public static void ShowTab(MenuType tab)
		{
			instance?.ActivateTab(tab);
		}

		[ObjectCreator.UseCtor]
		public MapEditorTabsLogic(Widget widget, World world)
		{
			instance = this;
			this.world = world;
			panelContainer = widget.Parent;
			tabContainer = widget.Get("MAP_EDITOR_TAB_CONTAINER");

			editor = widget.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.UpdateSelectedTab += HandleUpdateSelectedTab;
			editor.LocateAssetRequested += HandleLocateAssetRequested;

			SetupTab("SELECT_TAB", "SELECT_WIDGETS", MenuType.Select);
			SetupTab("TILES_TAB", "TILE_WIDGETS", MenuType.Tiles);
			SetupTab("OVERLAYS_TAB", "LAYER_WIDGETS", MenuType.Layers);
			SetupTab("ACTORS_TAB", "ACTOR_WIDGETS", MenuType.Actors);
			SetupTab("TOOLS_TAB", "TOOLS_WIDGETS", MenuType.Tools);
			SetupTab("HISTORY_TAB", "HISTORY_WIDGETS", MenuType.History);
		}

		protected override void Dispose(bool disposing)
		{
			if (instance == this)
				instance = null;

			editor.DefaultBrush.UpdateSelectedTab -= HandleUpdateSelectedTab;
			editor.LocateAssetRequested -= HandleLocateAssetRequested;

			base.Dispose(disposing);
		}

		void ActivateTab(MenuType tab)
		{
			if (tab == MenuType.Select)
				return;

			lastSelectedTab = tab;
			menuType = tab;
			OnTabChanged?.Invoke();
			Ui.KeyboardFocusWidget = null;
		}

		void HandleLocateAssetRequested(EditorLocateAssetRequest request)
		{
			if (request.Kind == EditorLocateAssetKind.RestoreAllCategories)
				return;

			var tab = request.Kind switch
			{
				EditorLocateAssetKind.Tile => MenuType.Tiles,
				EditorLocateAssetKind.Actor => MenuType.Actors,
				EditorLocateAssetKind.Resource => MenuType.Layers,
				_ => MenuType.Tiles
			};

			if (tab != MenuType.Select)
				lastSelectedTab = tab;

			menuType = tab;
			OnTabChanged?.Invoke();
			Ui.KeyboardFocusWidget = null;
		}

		void SetupTab(string buttonId, string tabId, MenuType tabType)
		{
			var tab = tabContainer.Get<ButtonWidget>(buttonId);
			tab.IsHighlighted = () => tabType == MenuType.Select
				? editor.DefaultBrush.AreaPanelOpen
				: menuType == tabType;
			tab.OnClick = () =>
			{
				if (tabType == MenuType.Select)
				{
					if (editor.DefaultBrush.AreaPanelOpen)
						editor.DefaultBrush.HideAreaPanel();
					else
						editor.DefaultBrush.ShowAreaPanel();
				}
				else
				{
					lastSelectedTab = tabType;
					menuType = tabType;
				}

				OnTabChanged?.Invoke();

				// Clear keyboard focus when switching tabs.
				Ui.KeyboardFocusWidget = null;
			};

			if (tabType == MenuType.Tools)
			{
				var toolsAvailable = world.WorldActor.TraitsImplementing<IEditorTool>().Any();
				tab.IsDisabled = () => !toolsAvailable;
			}

			var container = panelContainer.Get<ContainerWidget>(tabId);
			if (tabType == MenuType.Select)
			{
				container.IsVisible = () => editor.HasClipboard
					|| editor.DefaultBrush.Selection.Actor != null
					|| editor.DefaultBrush.AreaPanelOpen;
			}
			else
				container.IsVisible = () => menuType == tabType;
		}

		void HandleUpdateSelectedTab()
		{
			OnTabChanged?.Invoke();
		}
	}
}
