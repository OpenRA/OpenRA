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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorSelectionLogic : ChromeLogic
	{
		[TranslationReference]
		const string AreaSelection = "label-area-selection";

		readonly EditorViewportControllerWidget editor;
		readonly WorldRenderer worldRenderer;

		readonly ContainerWidget actorEditPanel;
		readonly ContainerWidget areaEditPanel;

		readonly CheckboxWidget copyTerrainCheckbox;
		readonly CheckboxWidget copyResourcesCheckbox;
		readonly CheckboxWidget copyActorsCheckbox;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorResourceLayer editorResourceLayer;
		readonly IResourceLayer resourceLayer;

		public LabelWidget AreaEditTitle;
		public LabelWidget DiagonalLabel;
		public LabelWidget ResourceCounterLabel;

		MapCopyFilters copyFilters = MapCopyFilters.All;
		EditorClipboard? clipboard;

		[ObjectCreator.UseCtor]
		public MapEditorSelectionLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			editorResourceLayer = world.WorldActor.TraitOrDefault<EditorResourceLayer>();

			editor = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;
			var selectTabContainer = widget.Get("SELECT_WIDGETS");
			actorEditPanel = selectTabContainer.Get<ContainerWidget>("ACTOR_EDIT_PANEL");
			areaEditPanel = selectTabContainer.Get<ContainerWidget>("AREA_EDIT_PANEL");

			actorEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Actor != null;
			areaEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Area != null;

			copyTerrainCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_TERRAIN_CHECKBOX");
			copyResourcesCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_RESOURCES_CHECKBOX");
			copyActorsCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_ACTORS_CHECKBOX");

			copyTerrainCheckbox.IsDisabled = () => editor.CurrentBrush is EditorCopyPasteBrush;
			copyResourcesCheckbox.IsDisabled = () => editor.CurrentBrush is EditorCopyPasteBrush;
			copyActorsCheckbox.IsDisabled = () => editor.CurrentBrush is EditorCopyPasteBrush;

			var copyButton = widget.Get<ButtonWidget>("COPY_BUTTON");
			copyButton.OnClick = () => clipboard = CopySelectionContents();
			copyButton.IsDisabled = () => editor.DefaultBrush.Selection.Area == null;

			AreaEditTitle = areaEditPanel.Get<LabelWidget>("AREA_EDIT_TITLE");
			DiagonalLabel = areaEditPanel.Get<LabelWidget>("DIAGONAL_COUNTER_LABEL");
			ResourceCounterLabel = areaEditPanel.Get<LabelWidget>("RESOURCES_COUNTER_LABEL");

			var pasteButton = widget.Get<ButtonWidget>("PASTE_BUTTON");
			pasteButton.OnClick = () =>
			{
				if (clipboard == null)
					return;

				editor.SetBrush(new EditorCopyPasteBrush(
					editor,
					worldRenderer,
					clipboard.Value,
					resourceLayer,
					() => copyFilters));
			};

			pasteButton.IsDisabled = () => clipboard == null;
			pasteButton.IsHighlighted = () => editor.CurrentBrush is EditorCopyPasteBrush;

			var closeAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CANCEL_BUTTON");
			closeAreaSelectionButton.OnClick = () => editor.DefaultBrush.ClearSelection(updateSelectedTab: true);

			CreateCategoryPanel(MapCopyFilters.Terrain, copyTerrainCheckbox);
			CreateCategoryPanel(MapCopyFilters.Resources, copyResourcesCheckbox);
			CreateCategoryPanel(MapCopyFilters.Actors, copyActorsCheckbox);
		}

		EditorClipboard CopySelectionContents()
		{
			var selection = editor.DefaultBrush.Selection.Area;
			var source = new CellCoordsRegion(selection.TopLeft, selection.BottomRight);

			var mapTiles = worldRenderer.World.Map.Tiles;
			var mapHeight = worldRenderer.World.Map.Height;
			var mapResources = worldRenderer.World.Map.Resources;

			var previews = new Dictionary<string, EditorActorPreview>();
			var tiles = new Dictionary<CPos, ClipboardTile>();

			foreach (var cell in source)
			{
				if (!mapTiles.Contains(cell))
					continue;

				tiles.Add(cell, new ClipboardTile(mapTiles[cell], mapResources[cell], resourceLayer?.GetResource(cell), mapHeight[cell]));

				if (copyFilters.HasFlag(MapCopyFilters.Actors))
					foreach (var preview in selection.SelectMany(editorActorLayer.PreviewsAt).Distinct())
						previews.TryAdd(preview.ID, preview);
			}

			return new EditorClipboard(selection, previews, tiles);
		}

		void CreateCategoryPanel(MapCopyFilters copyFilter, CheckboxWidget checkbox)
		{
			checkbox.GetText = () => copyFilter.ToString();
			checkbox.IsChecked = () => copyFilters.HasFlag(copyFilter);
			checkbox.IsVisible = () => true;
			checkbox.OnClick = () => copyFilters ^= copyFilter;
		}

		protected override void Dispose(bool disposing)
		{
			editor.DefaultBrush.SelectionChanged -= HandleSelectionChanged;
			base.Dispose(disposing);
		}

		void HandleSelectionChanged()
		{
			var selectedRegion = editor.DefaultBrush.Selection.Area;
			if (selectedRegion == null)
				return;

			if (editorResourceLayer == null)
				return;

			var selectionSize = selectedRegion.BottomRight - selectedRegion.TopLeft + new CPos(1, 1);
			var diagonalLength = Math.Round(Math.Sqrt(Math.Pow(selectionSize.X, 2) + Math.Pow(selectionSize.Y, 2)), 3);
			var resourceValueInRegion = editorResourceLayer.CalculateRegionValue(selectedRegion);

			var areaSelectionLabel = $"{TranslationProvider.GetString(AreaSelection)} ({DimensionsAsString(selectionSize)}) {PositionAsString(selectedRegion.TopLeft)} : {PositionAsString(selectedRegion.BottomRight)}";

			AreaEditTitle.GetText = () => areaSelectionLabel;
			DiagonalLabel.GetText = () => $"{diagonalLength}";
			ResourceCounterLabel.GetText = () => $"${resourceValueInRegion:N0}";
		}

		static string PositionAsString(CPos cell) => $"{cell.X},{cell.Y}";
		static string DimensionsAsString(CPos cell) => $"{cell.X}x{cell.Y}";
	}
}
