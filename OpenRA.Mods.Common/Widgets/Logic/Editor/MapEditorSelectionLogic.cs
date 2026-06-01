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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(CopyPasteEditorAction),
		typeof(FillSelectionWithActorEditorAction),
		typeof(FillSelectionWithTileEditorAction))]
	public class MapEditorSelectionLogic : ChromeLogic
	{
		[FluentReference]
		const string AreaSelection = "label-area-selection";

		[FluentReference]
		const string MixModeRandom = "label-editor-mix-mode-random";

		[FluentReference]
		const string MixModeSequential = "label-editor-mix-mode-sequential";

		[FluentReference]
		const string SelectedAreaPreview = "label-selected-area-preview";

		readonly EditorViewportControllerWidget editor;
		readonly Map map;

		readonly EditorActorLayer editorActorLayer;
		readonly EditorResourceLayer editorResourceLayer;
		readonly IResourceLayer resourceLayer;
		readonly EditorActionManager editorActionManager;

		public LabelWidget AreaEditTitle;
		public LabelWidget DiagonalLabel;
		public LabelWidget ResourceCounterLabel;

		readonly TerrainTemplatePreviewWidget selectedTilePreview;
		readonly ActorPreviewWidget selectedActorPreview;
		readonly EditorBlitPreviewWidget clipboardPreview;
		readonly LabelWidget selectedPreviewLabel;
		readonly Widget selectedPreviewPanel;
		readonly WorldRenderer worldRenderer;
		readonly List<Widget> multiPreviewWidgets = [];
		MapBlitFilters selectionFilters = MapBlitFilters.All;
		CellCoordsRegion? cachedPreviewRegion;
		MapBlitFilters cachedPreviewFilters;
		EditorBlitSource? cachedPreviewSource;

		[ObjectCreator.UseCtor]
		public MapEditorSelectionLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			map = worldRenderer.World.Map;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			editorResourceLayer = world.WorldActor.TraitOrDefault<EditorResourceLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editor = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;
			editor.BrushChanged += UpdateSelectedPreview;
			editor.BrushChanged += UpdateAreaPreview;
			var selectTabContainer = widget.Get("SELECT_WIDGETS");
			var actorEditPanel = selectTabContainer.Get("ACTOR_EDIT_PANEL");
			var areaEditPanel = selectTabContainer.Get("AREA_EDIT_PANEL");

			actorEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Actor != null;
			areaEditPanel.IsVisible = () => editor.DefaultBrush.AreaPanelOpen || editor.HasClipboard;

			var copyTerrainCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_TERRAIN_CHECKBOX");
			var copyResourcesCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_RESOURCES_CHECKBOX");
			var copyActorsCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_ACTORS_CHECKBOX");

			copyTerrainCheckbox.IsDisabled = () => false;
			copyResourcesCheckbox.IsDisabled = () => false;
			copyActorsCheckbox.IsDisabled = () => false;

			SetupCopyPasteButton(widget.Get<ButtonWidget>("COPY_BUTTON"));
			SetupCopyPasteButton(areaEditPanel.Get<ButtonWidget>("SELECTION_COPY_BUTTON"));
			SetupCopyPasteButton(areaEditPanel.Get<ButtonWidget>("SELECTION_PASTE_BUTTON"), paste: true);
			SetupCopyPasteButton(widget.Get<ButtonWidget>("PASTE_BUTTON"), paste: true);

			var clearCopyButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CLEAR_COPY_BUTTON");
			clearCopyButton.OnClick = () =>
			{
				editor.ClearClipboard();
			};
			clearCopyButton.IsDisabled = () => !editor.HasClipboard;

			var rotateLeftButton = areaEditPanel.Get<ButtonWidget>("SELECTION_ROTATE_LEFT_BUTTON");
			rotateLeftButton.OnClick = () => RotateClipboard(RotationDirection.CounterClockwise);
			rotateLeftButton.IsDisabled = () => !editor.HasClipboard;

			var rotateRightButton = areaEditPanel.Get<ButtonWidget>("SELECTION_ROTATE_RIGHT_BUTTON");
			rotateRightButton.OnClick = () => RotateClipboard(RotationDirection.Clockwise);
			rotateRightButton.IsDisabled = () => !editor.HasClipboard;

			AreaEditTitle = areaEditPanel.Get<LabelWidget>("AREA_EDIT_TITLE");
			DiagonalLabel = areaEditPanel.Get<LabelWidget>("DIAGONAL_COUNTER_LABEL");
			ResourceCounterLabel = areaEditPanel.Get<LabelWidget>("RESOURCES_COUNTER_LABEL");
			selectedPreviewPanel = areaEditPanel.Get("SELECTION_PREVIEW_PANEL");
			selectedPreviewLabel = selectedPreviewPanel.Get<LabelWidget>("SELECTION_PREVIEW_LABEL");
			selectedTilePreview = selectedPreviewPanel.Get<TerrainTemplatePreviewWidget>("SELECTION_TILE_PREVIEW");
			selectedActorPreview = selectedPreviewPanel.Get<ActorPreviewWidget>("SELECTION_ACTOR_PREVIEW");
			clipboardPreview = selectedPreviewPanel.Get<EditorBlitPreviewWidget>("SELECTION_CLIPBOARD_PREVIEW");
			clipboardPreview.SetPreviewSource(GetAreaPreviewSource);
			clipboardPreview.IsVisible = () => editor.HasClipboard || ShowAreaPreview();
			var mixModeLabel = selectedPreviewPanel.Get<LabelWidget>("MIX_MODE_LABEL");
			var mixModeDropDown = selectedPreviewPanel.Get<DropDownButtonWidget>("MIX_MODE_DROPDOWN");
			mixModeLabel.IsVisible = mixModeDropDown.IsVisible = () => editor.CurrentBrush is EditorTileBrush or EditorActorBrush;
			mixModeDropDown.GetText = () => MixModeText(editor.AssetMixMode);
			mixModeDropDown.OnClick = () => ShowMixModeDropDown(mixModeDropDown);

			var deleteAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_DELETE_BUTTON");
			deleteAreaSelectionButton.OnClick = () =>
			{
				editor.DefaultBrush.DeleteSelection(selectionFilters);
				InvalidateAreaPreview();
				UpdateAreaPreview();
			};

			var fillAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_FILL_BUTTON");
			fillAreaSelectionButton.OnClick = () =>
			{
				if (!editor.DefaultBrush.Selection.Area.HasValue)
					return;

				if (editor.CurrentBrush is EditorTileBrush tileBrush)
					editorActionManager.Add(new FillSelectionWithTileEditorAction(
						tileBrush.Templates,
						editor.AssetMixMode,
						map,
						editor.DefaultBrush.Selection.Area.Value));
				else if (editor.CurrentBrush is EditorActorBrush actorBrush)
					editorActionManager.Add(new FillSelectionWithActorEditorAction(
						editorActorLayer,
						actorBrush.ActorReferences,
						editor.AssetMixMode,
						map,
						editor.DefaultBrush.Selection.Area.Value));
			};

			fillAreaSelectionButton.IsDisabled = () => editor.CurrentBrush is not EditorTileBrush && editor.CurrentBrush is not EditorActorBrush;

			var closeAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CANCEL_BUTTON");
			closeAreaSelectionButton.OnClick = () => editor.DefaultBrush.CloseAreaPanel();

			CreateCategoryPanel(MapBlitFilters.Terrain, copyTerrainCheckbox);
			CreateCategoryPanel(MapBlitFilters.Resources, copyResourcesCheckbox);
			CreateCategoryPanel(MapBlitFilters.Actors, copyActorsCheckbox);
			UpdateSelectedPreview();
			UpdateAreaPreview();
		}

		void SetupCopyPasteButton(ButtonWidget button, bool paste = false)
		{
			if (paste)
			{
				button.OnClick = PasteSelection;
				button.IsDisabled = () => !editor.HasClipboard || !editor.DefaultBrush.Selection.Area.HasValue;
				button.IsHighlighted = () => editor.HasClipboard && editor.DefaultBrush.Selection.Area.HasValue;
			}
			else
			{
				button.OnClick = CopySelection;
				button.IsDisabled = () => !editor.DefaultBrush.Selection.Area.HasValue;
				button.IsHighlighted = () => editor.CopySourceRegion.HasValue;
			}
		}

		void CopySelection()
		{
			if (!editor.DefaultBrush.Selection.Area.HasValue)
				return;

			var region = editor.DefaultBrush.Selection.Area.Value;
			editor.SetClipboard(CopySelectionContents(), region);
			UpdateAreaPreview();
		}

		void RotateClipboard(RotationDirection direction)
		{
			if (!editor.HasClipboard || !editor.CopySourceRegion.HasValue)
				return;

			var rotated = EditorBlit.Rotate(editor.Clipboard.Value, direction, worldRenderer);
			editor.SetClipboard(rotated, editor.CopySourceRegion.Value);
			UpdateAreaPreview();
		}

		void PasteSelection()
		{
			if (!editor.HasClipboard || !editor.DefaultBrush.Selection.Area.HasValue)
				return;

			editor.ClearBrush();
			var pastePosition = editor.DefaultBrush.Selection.Area.Value.TopLeft;
			var editorBlit = new EditorBlit(
				selectionFilters,
				resourceLayer,
				pastePosition,
				map,
				editor.Clipboard.Value,
				editorActorLayer,
				true);
			editorActionManager.Add(new CopyPasteEditorAction(editorBlit));
			InvalidateAreaPreview();
			UpdateAreaPreview();
		}

		EditorBlitSource CopySelectionContents()
		{
			return EditorBlit.CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				editor.DefaultBrush.Selection.Area.Value,
				selectionFilters);
		}

		void CreateCategoryPanel(MapBlitFilters copyFilter, CheckboxWidget checkbox)
		{
			checkbox.GetText = copyFilter.ToString;
			checkbox.IsChecked = () => selectionFilters.HasFlag(copyFilter);
			checkbox.IsVisible = () => true;
			checkbox.OnClick = () =>
			{
				selectionFilters ^= copyFilter;
				InvalidateAreaPreview();
				UpdateAreaPreview();
			};
		}

		protected override void Dispose(bool disposing)
		{
			editor.DefaultBrush.SelectionChanged -= HandleSelectionChanged;
			editor.BrushChanged -= UpdateSelectedPreview;
			editor.BrushChanged -= UpdateAreaPreview;
			base.Dispose(disposing);
		}

		bool ShowAreaPreview()
		{
			return editor.DefaultBrush.Selection.Area.HasValue
				&& editor.CurrentBrush is not EditorTileBrush and not EditorActorBrush;
		}

		(EditorBlitSource Source, MapBlitFilters Filters)? GetAreaPreviewSource()
		{
			if (editor.HasClipboard)
				return (editor.Clipboard.Value, selectionFilters);

			if (!ShowAreaPreview())
				return null;

			var region = editor.DefaultBrush.Selection.Area.Value;
			if (cachedPreviewSource == null || cachedPreviewRegion is not CellCoordsRegion cachedRegion ||
				cachedRegion.TopLeft != region.TopLeft || cachedRegion.BottomRight != region.BottomRight ||
				cachedPreviewFilters != selectionFilters)
			{
				cachedPreviewRegion = region;
				cachedPreviewFilters = selectionFilters;
				cachedPreviewSource = EditorBlit.CopyRegionContents(
					map,
					editorActorLayer,
					resourceLayer,
					region,
					selectionFilters);
			}

			return (cachedPreviewSource.Value, selectionFilters);
		}

		void InvalidateAreaPreview()
		{
			cachedPreviewSource = null;
			cachedPreviewRegion = null;
		}

		void UpdateAreaPreview()
		{
			selectedPreviewLabel.GetText = () => FluentProvider.GetMessage(SelectedAreaPreview);

			if (editor.HasClipboard || ShowAreaPreview())
				clipboardPreview.PrepareRenderables();
		}

		void UpdateSelectedPreview()
		{
			ClearMultiPreview();
			if (editor.CurrentBrush is EditorTileBrush tileBrush)
			{
				if (tileBrush.Templates.Length > 1)
				{
					ShowTileMultiPreview(tileBrush);
					selectedTilePreview.IsVisible = () => false;
				}
				else
				{
					selectedTilePreview.SetTemplate(tileBrush.TerrainTemplate);
					ScaleTerrainPreview();
					selectedTilePreview.IsVisible = () => true;
				}

				selectedActorPreview.IsVisible = () => false;
			}
			else if (editor.CurrentBrush is EditorActorBrush actorBrush)
			{
				if (actorBrush.Actors.Length > 1)
				{
					ShowActorMultiPreview(actorBrush);
					selectedActorPreview.IsVisible = () => false;
				}
				else
				{
					selectedActorPreview.SetPreview(actorBrush.Preview.Export());
					ScaleActorPreview();
					selectedActorPreview.IsVisible = () => true;
				}

				selectedTilePreview.IsVisible = () => false;
			}
			else
			{
				selectedTilePreview.IsVisible = () => false;
				selectedActorPreview.IsVisible = () => false;
			}
		}

		void ShowMixModeDropDown(DropDownButtonWidget dropDown)
		{
			var options = new[] { EditorAssetMixMode.Random, EditorAssetMixMode.Sequential };

			ScrollItemWidget SetupItem(EditorAssetMixMode option, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => editor.AssetMixMode == option,
					() => editor.SetAssetMixMode(option));

				item.Get<LabelWidget>("LABEL").GetText = () => MixModeText(option);
				return item;
			}

			dropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 60, options, SetupItem);
		}

		static string MixModeText(EditorAssetMixMode mode)
		{
			return FluentProvider.GetMessage(mode == EditorAssetMixMode.Random ? MixModeRandom : MixModeSequential);
		}

		void ClearMultiPreview()
		{
			foreach (var preview in multiPreviewWidgets)
				selectedPreviewPanel.RemoveChild(preview);

			multiPreviewWidgets.Clear();
		}

		void ShowTileMultiPreview(EditorTileBrush tileBrush)
		{
			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			var bounds = MultiPreviewBounds();
			var previews = tileBrush.Templates.Select(t => terrainInfo.Templates[t]).ToArray();

			for (var i = 0; i < previews.Length; i++)
			{
				var preview = selectedTilePreview.Clone();
				preview.SetTemplate(previews[i]);
				PlaceMultiPreview(preview, i, previews.Length, bounds);
				preview.Scale = PreviewScale(preview.IdealPreviewSize, preview.Bounds.Width, preview.Bounds.Height);
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedPreviewPanel.AddChild(preview);
			}
		}

		void ShowActorMultiPreview(EditorActorBrush actorBrush)
		{
			var previews = actorBrush.ActorReferences.Select(a => a.Clone()).ToArray();
			var bounds = MultiPreviewBounds();

			for (var i = 0; i < previews.Length; i++)
			{
				var preview = selectedActorPreview.Clone();
				preview.SetPreview(previews[i]);
				PlaceMultiPreview(preview, i, previews.Length, bounds);
				preview.Scale = PreviewScale(preview.IdealPreviewSize, preview.Bounds.Width, preview.Bounds.Height);
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedPreviewPanel.AddChild(preview);
			}
		}

		Rectangle MultiPreviewBounds()
		{
			const int PreviewSize = 130;
			var x = (selectedPreviewPanel.Bounds.Width - PreviewSize) / 2;
			return new Rectangle(x, 55, PreviewSize, PreviewSize);
		}

		static void PlaceMultiPreview(Widget preview, int index, int count, Rectangle bounds)
		{
			var columns = Math.Min(3, Math.Max(1, count));
			var rows = (count + columns - 1) / columns;
			var width = bounds.Width / columns;
			var height = bounds.Height / rows;

			preview.Bounds.X = bounds.X + index % columns * width;
			preview.Bounds.Y = bounds.Y + index / columns * height;
			preview.Bounds.Width = width;
			preview.Bounds.Height = height;
		}

		void ScaleTerrainPreview()
		{
			var bounds = MultiPreviewBounds();
			var scale = PreviewScale(selectedTilePreview.IdealPreviewSize, bounds.Width, bounds.Height);
			selectedTilePreview.Scale = scale;
			selectedTilePreview.Bounds.Width = (int)(scale * selectedTilePreview.IdealPreviewSize.X);
			selectedTilePreview.Bounds.Height = (int)(scale * selectedTilePreview.IdealPreviewSize.Y);
			selectedTilePreview.Bounds.X = bounds.X + (bounds.Width - selectedTilePreview.Bounds.Width) / 2;
			selectedTilePreview.Bounds.Y = bounds.Y + (bounds.Height - selectedTilePreview.Bounds.Height) / 2;
		}

		void ScaleActorPreview()
		{
			var bounds = MultiPreviewBounds();
			var scale = PreviewScale(selectedActorPreview.IdealPreviewSize, bounds.Width, bounds.Height);
			selectedActorPreview.Scale = scale;
			selectedActorPreview.Bounds.X = bounds.X;
			selectedActorPreview.Bounds.Y = bounds.Y;
			selectedActorPreview.Bounds.Width = bounds.Width;
			selectedActorPreview.Bounds.Height = bounds.Height;
		}

		static float PreviewScale(int2 idealSize, int maxWidth, int maxHeight)
		{
			if (idealSize.X <= 0 || idealSize.Y <= 0)
				return 1f;

			return Math.Min(maxWidth / (float)idealSize.X, maxHeight / (float)idealSize.Y);
		}

		void HandleSelectionChanged()
		{
			InvalidateAreaPreview();
			UpdateAreaPreview();

			if (!editor.DefaultBrush.Selection.Area.HasValue)
				return;

			var selectedRegion = editor.DefaultBrush.Selection.Area.Value;

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
