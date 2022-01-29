#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("ChangeZoomKey")]
	public class MapEditorLogic : ChromeLogic
	{
		MapCopyFilters copyFilters = MapCopyFilters.All;

		enum MapOverlays
		{
			None = 0,
			Grid = 1,
			Buildable = 2,
		}

		MapOverlays overlays = MapOverlays.None;

		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var editorViewport = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");

			var copypasteButton = widget.GetOrNull<ButtonWidget>("COPYPASTE_BUTTON");
			if (copypasteButton != null)
			{
				// HACK: Replace Ctrl with Cmd on macOS
				// TODO: Add platform-specific override support to HotkeyManager
				// and then port the editor hotkeys to this system.
				var copyPasteKey = copypasteButton.Key.GetValue();
				if (Platform.CurrentPlatform == PlatformType.OSX && copyPasteKey.Modifiers.HasModifier(Modifiers.Ctrl))
				{
					var modified = new Hotkey(copyPasteKey.Key, copyPasteKey.Modifiers & ~Modifiers.Ctrl | Modifiers.Meta);
					copypasteButton.Key = FieldLoader.GetValue<HotkeyReference>("Key", modified.ToString());
				}

				copypasteButton.OnClick = () => editorViewport.SetBrush(new EditorCopyPasteBrush(editorViewport, worldRenderer, () => copyFilters));
				copypasteButton.IsHighlighted = () => editorViewport.CurrentBrush is EditorCopyPasteBrush;
			}

			var copyFilterDropdown = widget.Get<DropDownButtonWidget>("COPYFILTER_BUTTON");
			copyFilterDropdown.OnMouseDown = _ =>
			{
				copyFilterDropdown.RemovePanel();
				copyFilterDropdown.AttachPanel(CreateCategoriesPanel());
			};

			var coordinateLabel = widget.GetOrNull<LabelWidget>("COORDINATE_LABEL");
			if (coordinateLabel != null)
			{
				coordinateLabel.GetText = () =>
				{
					var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
					var map = worldRenderer.World.Map;
					return map.Height.Contains(cell) ? $"{cell},{map.Height[cell]} ({map.Tiles[cell].Type})" : "";
				};
			}

			var overlayDropdown = widget.GetOrNull<DropDownButtonWidget>("OVERLAY_BUTTON");
			if (overlayDropdown != null)
			{
				overlayDropdown.OnMouseDown = _ =>
				{
					overlayDropdown.RemovePanel();
					overlayDropdown.AttachPanel(CreateOverlaysPanel(world));
				};
			}

			var cashLabel = widget.GetOrNull<LabelWidget>("CASH_LABEL");
			if (cashLabel != null)
			{
				var reslayer = worldRenderer.World.WorldActor.TraitsImplementing<EditorResourceLayer>().FirstOrDefault();
				if (reslayer != null)
					cashLabel.GetText = () => $"$ {reslayer.NetWorth}";
			}

			var undoButton = widget.GetOrNull<ButtonWidget>("UNDO_BUTTON");
			var redoButton = widget.GetOrNull<ButtonWidget>("REDO_BUTTON");
			if (undoButton != null && redoButton != null)
			{
				var actionManager = world.WorldActor.Trait<EditorActionManager>();
				undoButton.IsDisabled = () => !actionManager.HasUndos();
				undoButton.OnClick = () => actionManager.Undo();
				redoButton.IsDisabled = () => !actionManager.HasRedos();
				redoButton.OnClick = () => actionManager.Redo();
			}
		}

		Widget CreateCategoriesPanel()
		{
			var categoriesPanel = Ui.LoadWidget("COPY_FILTER_PANEL", null, new WidgetArgs());
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			MapCopyFilters[] allCategories = { MapCopyFilters.Terrain, MapCopyFilters.Resources, MapCopyFilters.Actors };
			foreach (var cat in allCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat.ToString();
				category.IsChecked = () => copyFilters.HasFlag(cat);
				category.IsVisible = () => true;
				category.OnClick = () => copyFilters ^= cat;

				categoriesPanel.AddChild(category);
			}

			return categoriesPanel;
		}

		Widget CreateOverlaysPanel(World world)
		{
			var categoriesPanel = Ui.LoadWidget("OVERLAY_PANEL", null, new WidgetArgs());
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			MapOverlays[] allCategories = { MapOverlays.Grid, MapOverlays.Buildable };
			foreach (var cat in allCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat.ToString();
				category.IsChecked = () => overlays.HasFlag(cat);
				category.IsVisible = () => true;
				category.OnClick = () => overlays ^= cat;

				if (cat.HasFlag(MapOverlays.Grid))
				{
					var terrainGeometryTrait = world.WorldActor.Trait<TerrainGeometryOverlay>();
					category.OnClick = () =>
					{
						overlays ^= cat;
						terrainGeometryTrait.Enabled = overlays.HasFlag(MapOverlays.Grid);
					};
				}

				if (cat.HasFlag(MapOverlays.Buildable))
				{
					var buildableTerrainTrait = world.WorldActor.Trait<BuildableTerrainOverlay>();
					category.OnClick = () =>
					{
						overlays ^= cat;
						buildableTerrainTrait.Enabled = overlays.HasFlag(MapOverlays.Buildable);
					};
				}

				categoriesPanel.AddChild(category);
			}

			return categoriesPanel;
		}
	}
}
