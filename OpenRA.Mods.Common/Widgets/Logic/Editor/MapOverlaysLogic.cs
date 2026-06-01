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
	[ChromeLogicArgsHotkeys("ToggleGridOverlayKey", "ToggleBuildableOverlayKey", "ToggleWalkableOverlayKey", "ToggleShipTravelOverlayKey", "ToggleMarkerOverlayKey", "ToggleTilesOverlayKey", "ToggleActorsOverlayKey")]
	public class MapOverlaysLogic : ChromeLogic
	{
		[Flags]
		enum MapOverlays
		{
			None = 0,
			Grid = 1,
			Buildable = 2,
			Walkable = 4,
			ShipTravel = 8,
			Marker = 16,
			Tiles = 32,
			Actors = 64,
		}

		readonly TerrainGeometryOverlay terrainGeometryTrait;
		readonly BuildableTerrainOverlay buildableTerrainTrait;
		readonly WalkableTerrainOverlay walkableTerrainTrait;
		readonly ShipTravelTerrainOverlay shipTravelTerrainTrait;
		readonly MarkerLayerOverlay markerLayerTrait;
		readonly TemplateBoundsOverlay templateBoundsTrait;
		readonly ActorBoundsOverlay actorBoundsTrait;

		readonly HotkeyReference toggleGridKey;
		readonly HotkeyReference toggleBuildableKey;
		readonly HotkeyReference toggleWalkableKey;
		readonly HotkeyReference toggleShipTravelKey;
		readonly HotkeyReference toggleMarkerKey;
		readonly HotkeyReference toggleTilesKey;
		readonly HotkeyReference toggleActorsKey;

		readonly Widget overlayPanel;
		DropDownButtonWidget overlayDropdown;

		[ObjectCreator.UseCtor]
		public MapOverlaysLogic(Widget widget, World world, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			terrainGeometryTrait = world.WorldActor.Trait<TerrainGeometryOverlay>();
			buildableTerrainTrait = world.WorldActor.Trait<BuildableTerrainOverlay>();
			walkableTerrainTrait = world.WorldActor.Trait<WalkableTerrainOverlay>();
			shipTravelTerrainTrait = world.WorldActor.TraitOrDefault<ShipTravelTerrainOverlay>();
			markerLayerTrait = world.WorldActor.Trait<MarkerLayerOverlay>();
			templateBoundsTrait = world.WorldActor.Trait<TemplateBoundsOverlay>();
			actorBoundsTrait = world.WorldActor.Trait<ActorBoundsOverlay>();

			toggleGridKey = GetHotkey(logicArgs, modData, "ToggleGridOverlayKey");
			toggleBuildableKey = GetHotkey(logicArgs, modData, "ToggleBuildableOverlayKey");
			toggleWalkableKey = GetHotkey(logicArgs, modData, "ToggleWalkableOverlayKey");
			toggleShipTravelKey = GetHotkey(logicArgs, modData, "ToggleShipTravelOverlayKey");
			toggleMarkerKey = GetHotkey(logicArgs, modData, "ToggleMarkerOverlayKey");
			toggleTilesKey = GetHotkey(logicArgs, modData, "ToggleTilesOverlayKey");
			toggleActorsKey = GetHotkey(logicArgs, modData, "ToggleActorsOverlayKey");

			overlayPanel = CreateOverlaysPanel();

			var keyhandler = widget.Get<LogicKeyListenerWidget>("OVERLAY_KEYHANDLER");
			keyhandler.AddHandler(HandleOverlayHotkey);

			overlayDropdown = widget.GetOrNull<DropDownButtonWidget>("OVERLAY_BUTTON");
			if (overlayDropdown != null)
			{
				overlayDropdown.AdditionalKeyHandler = HandleOverlayHotkey;
				overlayDropdown.OnMouseDown = _ =>
				{
					overlayDropdown.RemovePanel();
					overlayDropdown.AttachPanel(overlayPanel);
				};
			}
		}

		static HotkeyReference GetHotkey(Dictionary<string, MiniYaml> logicArgs, ModData modData, string key)
		{
			if (logicArgs.TryGetValue(key, out var yaml))
				return modData.Hotkeys[yaml.Value];

			return new HotkeyReference();
		}

		bool HandleOverlayHotkey(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			if (toggleGridKey.IsActivatedBy(e))
			{
				terrainGeometryTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (toggleBuildableKey.IsActivatedBy(e))
			{
				buildableTerrainTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (toggleWalkableKey.IsActivatedBy(e))
			{
				walkableTerrainTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (shipTravelTerrainTrait != null && toggleShipTravelKey.IsActivatedBy(e))
			{
				shipTravelTerrainTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (toggleMarkerKey.IsActivatedBy(e))
			{
				markerLayerTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (toggleTilesKey.IsActivatedBy(e))
			{
				templateBoundsTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			if (toggleActorsKey.IsActivatedBy(e))
			{
				actorBoundsTrait.Enabled ^= true;
				OpenOverlayPanel();
				return true;
			}

			return false;
		}

		void OpenOverlayPanel()
		{
			if (overlayDropdown == null || overlayDropdown.IsPanelOpen)
				return;

			overlayDropdown.AttachPanel(overlayPanel);
		}

		Widget CreateOverlaysPanel()
		{
			var categoriesPanel = Ui.LoadWidget("OVERLAY_PANEL", null, []);
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			var allCategories = new List<MapOverlays>
			{
				MapOverlays.Grid,
				MapOverlays.Buildable,
				MapOverlays.Walkable
			};

			if (shipTravelTerrainTrait != null)
				allCategories.Add(MapOverlays.ShipTravel);

			allCategories.AddRange([MapOverlays.Marker, MapOverlays.Tiles, MapOverlays.Actors]);

			foreach (var cat in allCategories)
			{
				var category = categoryTemplate.Clone();
				category.GetText = () => cat switch
				{
					MapOverlays.ShipTravel => "Ship travel",
					MapOverlays.Walkable => "Walk / Drive",
					_ => cat.ToString()
				};
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
				else if (cat.HasFlag(MapOverlays.Walkable))
				{
					category.IsChecked = () => walkableTerrainTrait.Enabled;
					category.OnClick = () => walkableTerrainTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.ShipTravel))
				{
					category.IsChecked = () => shipTravelTerrainTrait.Enabled;
					category.OnClick = () => shipTravelTerrainTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.Marker))
				{
					category.IsChecked = () => markerLayerTrait.Enabled;
					category.OnClick = () => markerLayerTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.Tiles))
				{
					category.IsChecked = () => templateBoundsTrait.Enabled;
					category.OnClick = () => templateBoundsTrait.Enabled ^= true;
				}
				else if (cat.HasFlag(MapOverlays.Actors))
				{
					category.IsChecked = () => actorBoundsTrait.Enabled;
					category.OnClick = () => actorBoundsTrait.Enabled ^= true;
				}

				categoriesPanel.AddChild(category);
			}

			return categoriesPanel;
		}
	}
}
