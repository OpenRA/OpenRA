#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorTabsLogic : ChromeLogic
	{
		protected enum MenuType { Tiles, Layers, Actors }
		protected MenuType menuType = MenuType.Tiles;

		[ObjectCreator.UseCtor]
		public MapEditorTabsLogic(Widget widget, WorldRenderer worldRenderer)
		{
			var tabContainer = widget.Get("MAP_EDITOR_TAB_CONTAINER");

			var tilesTab = tabContainer.Get<ButtonWidget>("TILES_TAB");
			tilesTab.IsHighlighted = () => menuType == MenuType.Tiles;
			tilesTab.OnClick = () => { menuType = MenuType.Tiles; };

			var overlaysTab = tabContainer.Get<ButtonWidget>("OVERLAYS_TAB");
			overlaysTab.IsHighlighted = () => menuType == MenuType.Layers;
			overlaysTab.OnClick = () => { menuType = MenuType.Layers; };

			var actorsTab = tabContainer.Get<ButtonWidget>("ACTORS_TAB");
			actorsTab.IsHighlighted = () => menuType == MenuType.Actors;
			actorsTab.OnClick = () => { menuType = MenuType.Actors; };

			var tileContainer = widget.Parent.Get<ContainerWidget>("TILE_WIDGETS");
			tileContainer.IsVisible = () => menuType == MenuType.Tiles;

			var layerContainer = widget.Parent.Get<ContainerWidget>("LAYER_WIDGETS");
			layerContainer.IsVisible = () => menuType == MenuType.Layers;

			var actorContainer = widget.Parent.Get<ContainerWidget>("ACTOR_WIDGETS");
			actorContainer.IsVisible = () => menuType == MenuType.Actors;
		}
	}
}
