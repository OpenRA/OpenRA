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
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorSelectionLogic : ChromeLogic
	{
		[FluentReference]
		const string AreaSelection = "label-area-selection";

		readonly EditorViewportControllerWidget editor;
		readonly Map map;

		readonly EditorActorLayer editorActorLayer;
		readonly EditorResourceLayer editorResourceLayer;
		readonly IResourceLayer resourceLayer;

		public LabelWidget AreaEditTitle;
		public LabelWidget DiagonalLabel;
		public LabelWidget ResourceCounterLabel;

		MapBlitFilters selectionFilters = MapBlitFilters.All;
		EditorBlitSource? clipboard;

		[ObjectCreator.UseCtor]
		public MapEditorSelectionLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			map = worldRenderer.World.Map;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			editorResourceLayer = world.WorldActor.TraitOrDefault<EditorResourceLayer>();

			editor = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;
			var selectTabContainer = widget.Get("SELECT_WIDGETS");
			var actorEditPanel = selectTabContainer.Get("ACTOR_EDIT_PANEL");
			var areaEditPanel = selectTabContainer.Get("AREA_EDIT_PANEL");

			actorEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Actor != null;
			areaEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Area != null;

			var copyTerrainCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_TERRAIN_CHECKBOX");
			var copyResourcesCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_RESOURCES_CHECKBOX");
			var copyActorsCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_ACTORS_CHECKBOX");

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
					() => selectionFilters));
			};

			pasteButton.IsDisabled = () => clipboard == null;
			pasteButton.IsHighlighted = () => editor.CurrentBrush is EditorCopyPasteBrush;

			var deleteAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_DELETE_BUTTON");
			deleteAreaSelectionButton.OnClick = () => editor.DefaultBrush.DeleteSelection(selectionFilters);

			var closeAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CANCEL_BUTTON");
			closeAreaSelectionButton.OnClick = () => editor.DefaultBrush.ClearSelection(updateSelectedTab: true);

			CreateCategoryPanel(MapBlitFilters.Terrain, copyTerrainCheckbox);
			CreateCategoryPanel(MapBlitFilters.Resources, copyResourcesCheckbox);
			CreateCategoryPanel(MapBlitFilters.Actors, copyActorsCheckbox);
		}

		EditorBlitSource CopySelectionContents()
		{
			return EditorBlit.CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				editor.DefaultBrush.Selection.Area,
				selectionFilters);
		}

		void CreateCategoryPanel(MapBlitFilters copyFilter, CheckboxWidget checkbox)
		{
			checkbox.GetText = copyFilter.ToString;
			checkbox.IsChecked = () => selectionFilters.HasFlag(copyFilter);
			checkbox.IsVisible = () => true;
			checkbox.OnClick = () => selectionFilters ^= copyFilter;
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

			var areaSelectionLabel =
				$"{FluentProvider.GetMessage(AreaSelection)} ({DimensionsAsString(selectionSize)}) " +
				$"{PositionAsString(selectedRegion.TopLeft)} : {PositionAsString(selectedRegion.BottomRight)}";

			AreaEditTitle.GetText = () => areaSelectionLabel;
			DiagonalLabel.GetText = () => $"{diagonalLength}";
			ResourceCounterLabel.GetText = () => $"${resourceValueInRegion:N0}";
		}

		static string PositionAsString(CPos cell) => $"{cell.X},{cell.Y}";
		static string DimensionsAsString(CPos cell) => $"{cell.X}x{cell.Y}";
	}
}
