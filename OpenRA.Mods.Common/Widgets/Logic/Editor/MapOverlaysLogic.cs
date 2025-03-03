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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("ToggleGridOverlayKey", "ToggleBuildableOverlayKey", "ToggleMarkerOverlayKey")]
	public class MapOverlaysLogic : ChromeLogic
	{
		[Flags]
		enum MapOverlays
		{
			None = 0,
			Grid = 1,
			Buildable = 2,
			Marker = 4,
		}

		readonly TerrainGeometryOverlay terrainGeometryTrait;
		readonly BuildableTerrainOverlay buildableTerrainTrait;
		readonly MarkerLayerOverlay markerLayerTrait;

		[ObjectCreator.UseCtor]
		public MapOverlaysLogic(Widget widget, World world, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			terrainGeometryTrait = world.WorldActor.Trait<TerrainGeometryOverlay>();
			buildableTerrainTrait = world.WorldActor.Trait<BuildableTerrainOverlay>();
			markerLayerTrait = world.WorldActor.Trait<MarkerLayerOverlay>();

			var toggleGridKey = new HotkeyReference();
			if (logicArgs.TryGetValue("ToggleGridOverlayKey", out var yaml))
				toggleGridKey = modData.Hotkeys[yaml.Value];

			var toggleBuildableKey = new HotkeyReference();
			if (logicArgs.TryGetValue("ToggleBuildableOverlayKey", out yaml))
				toggleBuildableKey = modData.Hotkeys[yaml.Value];

			var toggleMarkerKey = new HotkeyReference();
			if (logicArgs.TryGetValue("ToggleMarkerOverlayKey", out yaml))
				toggleMarkerKey = modData.Hotkeys[yaml.Value];

			var keyhandler = widget.Get<LogicKeyListenerWidget>("OVERLAY_KEYHANDLER");
			keyhandler.AddHandler(e =>
			{
				if (e.Event != KeyInputEvent.Down)
					return false;

				if (toggleGridKey.IsActivatedBy(e))
				{
					terrainGeometryTrait.Enabled ^= true;
					return true;
				}

				if (toggleBuildableKey.IsActivatedBy(e))
				{
					buildableTerrainTrait.Enabled ^= true;
					return true;
				}

				if (toggleMarkerKey.IsActivatedBy(e))
				{
					markerLayerTrait.Enabled ^= true;
					return true;
				}

				return false;
			});

			var overlayPanel = CreateOverlaysPanel();

			var overlayDropdown = widget.GetOrNull<DropDownButtonWidget>("OVERLAY_BUTTON");
			if (overlayDropdown != null)
			{
				overlayDropdown.OnMouseDown = _ =>
				{
					overlayDropdown.RemovePanel();
					overlayDropdown.AttachPanel(overlayPanel);
				};
			}
		}

		Widget CreateOverlaysPanel()
		{
			var categoriesPanel = Ui.LoadWidget("OVERLAY_PANEL", null, []);
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			MapOverlays[] allCategories = [MapOverlays.Grid, MapOverlays.Buildable, MapOverlays.Marker];
			foreach (var cat in allCategories)
			{
				var category = categoryTemplate.Clone();
				category.GetText = cat.ToString;
				category.IsVisible = () => true;

				if (cat.HasFlag(MapOverlays.Grid))
				{
					category.IsChecked = () => terrainGeometryTrait.Enabled;
					category.OnClick = () => terrainGeometryTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.Buildable))
				{
					category.IsChecked = () => buildableTerrainTrait.Enabled;
					category.OnClick = () => buildableTerrainTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.Marker))
				{
					category.IsChecked = () => markerLayerTrait.Enabled;
					category.OnClick = () => markerLayerTrait.Enabled ^= true;
				}

				categoriesPanel.AddChild(category);
			}

			return categoriesPanel;
		}
	}
}
