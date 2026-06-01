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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(CopyPasteEditorAction),
		typeof(ClearAndFillEditorAction),
		typeof(FillSelectionWithActorEditorAction),
		typeof(FillSelectionWithResourceEditorAction),
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

		const int SelectionPreviewBoxSize = 148;
		const float SelectionPreviewImageScale = 0.8f;

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
		readonly ResourcePreviewWidget selectedResourcePreview;
		readonly EditorBlitPreviewWidget clipboardPreview;
		readonly LabelWidget selectedPreviewLabel;
		readonly Widget selectedPreviewPanel;
		readonly Widget selectedAssetPreviewBox;
		readonly WorldRenderer worldRenderer;
		readonly List<Widget> multiPreviewWidgets = [];
		MapBlitFilters selectionFilters = MapBlitFilters.All;
		CellCoordsRegion? cachedPreviewRegion;
		MapBlitFilters cachedPreviewFilters;
		EditorBlitSource? cachedPreviewSource;
		HashSet<CPos> cachedPreviewCells;
		ushort? cachedPreviewTemplateType;
		CPos? cachedPreviewTemplateAnchor;
		TilePlacementPreviewDisplayMode cachedPreviewDisplayMode;
		readonly TemplateBoundsOverlay templateBoundsOverlay;
		readonly ActorBoundsOverlay actorBoundsOverlay;
		TilePlacementPreviewDisplayMode placementPreviewDisplayMode = TilePlacementPreviewDisplayMode.Current;

		[ObjectCreator.UseCtor]
		public MapEditorSelectionLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			map = worldRenderer.World.Map;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			editorResourceLayer = world.WorldActor.TraitOrDefault<EditorResourceLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			templateBoundsOverlay = world.WorldActor.TraitOrDefault<TemplateBoundsOverlay>();
			actorBoundsOverlay = world.WorldActor.TraitOrDefault<ActorBoundsOverlay>();

			editor = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;
			editor.BrushChanged += UpdateSelectedPreview;
			editor.BrushChanged += UpdateAreaPreview;
			var selectTabContainer = widget.Get("SELECT_WIDGETS");
			var actorEditPanel = selectTabContainer.Get("ACTOR_EDIT_PANEL");
			var areaEditPanel = selectTabContainer.Get("AREA_EDIT_PANEL");

			actorEditPanel.IsVisible = () => editor.DefaultBrush.Selection.Actor != null
				&& (actorBoundsOverlay == null || !actorBoundsOverlay.Enabled);
			areaEditPanel.IsVisible = () => (editor.DefaultBrush.AreaPanelOpen || editor.HasClipboard)
				&& editor.DefaultBrush.Selection.Actor == null;

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
			selectedAssetPreviewBox = selectedPreviewPanel.Get("SELECTION_ASSET_PREVIEW_BOX");
			selectedPreviewLabel = selectedPreviewPanel.Get<LabelWidget>("SELECTION_PREVIEW_LABEL");
			selectedTilePreview = selectedAssetPreviewBox.Get<TerrainTemplatePreviewWidget>("SELECTION_TILE_PREVIEW");
			selectedActorPreview = selectedAssetPreviewBox.Get<ActorPreviewWidget>("SELECTION_ACTOR_PREVIEW");
			selectedResourcePreview = selectedAssetPreviewBox.Get<ResourcePreviewWidget>("SELECTION_RESOURCE_PREVIEW");
			clipboardPreview = selectedAssetPreviewBox.Get<EditorBlitPreviewWidget>("SELECTION_CLIPBOARD_PREVIEW");
			clipboardPreview.SetPreviewSource(GetAreaPreviewSource);
			clipboardPreview.SetPlacementDisplay(GetTemplatePlacementDisplay);
			clipboardPreview.IsVisible = () => (editor.HasClipboard || ShowAreaPreview()) && !ShowActorOverlayPreview();

			var tilePreviewCurrentButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("TILE_PREVIEW_CURRENT_BUTTON");
			var tilePreviewOriginalButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("TILE_PREVIEW_ORIGINAL_BUTTON");
			if (tilePreviewCurrentButton != null && tilePreviewOriginalButton != null)
			{
				var showTilePreviewMode = () => ShowTilePlacementPreviewControls();
				tilePreviewCurrentButton.IsVisible = showTilePreviewMode;
				tilePreviewOriginalButton.IsVisible = showTilePreviewMode;
				tilePreviewCurrentButton.IsHighlighted = () => placementPreviewDisplayMode == TilePlacementPreviewDisplayMode.Current;
				tilePreviewOriginalButton.IsHighlighted = () => placementPreviewDisplayMode == TilePlacementPreviewDisplayMode.Original;
				tilePreviewCurrentButton.OnClick = () => SetPlacementPreviewDisplayMode(TilePlacementPreviewDisplayMode.Current);
				tilePreviewOriginalButton.OnClick = () => SetPlacementPreviewDisplayMode(TilePlacementPreviewDisplayMode.Original);
			}

			var mixModeLabel = selectedPreviewPanel.Get<LabelWidget>("MIX_MODE_LABEL");
			var mixModeDropDown = selectedPreviewPanel.Get<DropDownButtonWidget>("MIX_MODE_DROPDOWN");
			mixModeLabel.IsVisible = mixModeDropDown.IsVisible = () =>
				editor.CurrentBrush is EditorTileBrush or EditorActorBrush or EditorResourceBrush
				&& !ShowTilePlacementPreviewControls() && !ShowActorOverlayPreview();
			mixModeDropDown.GetText = () => MixModeText(editor.AssetMixMode);
			mixModeDropDown.OnClick = () => ShowMixModeDropDown(mixModeDropDown);

			var showFillControls = () => editor.CurrentBrush is EditorTileBrush or EditorActorBrush or EditorResourceBrush
				&& !ShowTilePlacementPreviewControls() && !ShowActorOverlayPreview();

			var fillSpaceLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_SPACE_LABEL");
			var fillSpaceSlider = selectedPreviewPanel.Get<SliderWidget>("FILL_SPACE_SLIDER");
			var fillSpaceValue = selectedPreviewPanel.Get<LabelWidget>("FILL_SPACE_VALUE");
			fillSpaceLabel.IsVisible = fillSpaceSlider.IsVisible = fillSpaceValue.IsVisible = showFillControls;
			fillSpaceSlider.MinimumValue = 10;
			fillSpaceSlider.MaximumValue = 100;
			fillSpaceSlider.Ticks = 10;
			fillSpaceSlider.GetValue = () => editor.AssetFillDensity;
			fillSpaceSlider.OnChange += value => editor.SetAssetFillDensity((int)value);
			fillSpaceValue.GetText = () => $"{editor.AssetFillDensity.ToString(NumberFormatInfo.InvariantInfo)}%";

			var fillModeOverlapLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_MODE_OVERLAP_LABEL");
			var fillModeSlider = selectedPreviewPanel.Get<SliderWidget>("FILL_MODE_SLIDER");
			var fillModeDeleteLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_MODE_DELETE_LABEL");
			fillModeOverlapLabel.IsVisible = fillModeSlider.IsVisible = fillModeDeleteLabel.IsVisible = showFillControls;
			fillModeSlider.MinimumValue = 0;
			fillModeSlider.MaximumValue = 1;
			fillModeSlider.Ticks = 2;
			fillModeSlider.GetValue = () => editor.AssetFillMode == EditorFillMode.Delete ? 1 : 0;
			fillModeSlider.OnChange += value => editor.SetAssetFillMode(value >= 0.5f ? EditorFillMode.Delete : EditorFillMode.Overlap);

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
				var selection = editor.DefaultBrush.Selection;
				if (!selection.Area.HasValue)
					return;

				if (editor.CurrentBrush is EditorTileBrush tileBrush)
					AddFillAction(new FillSelectionWithTileEditorAction(
						tileBrush.Templates,
						editor.AssetMixMode,
						GetFillDensityPercent(),
						map,
						selection.Area.Value,
						selection.GetAreaMask()));
				else if (editor.CurrentBrush is EditorActorBrush actorBrush)
					AddFillAction(new FillSelectionWithActorEditorAction(
						editorActorLayer,
						actorBrush.ActorReferences,
						editor.AssetMixMode,
						GetFillDensityPercent(),
						map,
						selection.Area.Value,
						selection.GetAreaMask()));
				else if (editor.CurrentBrush is EditorResourceBrush resourceBrush && resourceLayer != null)
					AddFillAction(new FillSelectionWithResourceEditorAction(
						resourceLayer,
						resourceBrush.ResourceType,
						editor.AssetMixMode,
						GetFillDensityPercent(),
						selection.Area.Value,
						selection.GetAreaMask()));
			};

			fillAreaSelectionButton.IsDisabled = () => editor.CurrentBrush is not EditorTileBrush
				&& editor.CurrentBrush is not EditorActorBrush
				&& (editor.CurrentBrush is not EditorResourceBrush || resourceLayer == null);

			var closeAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CANCEL_BUTTON");
			closeAreaSelectionButton.OnClick = () => editor.DefaultBrush.CloseAreaPanel();

			var closeAreaPanelButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_CLOSE_BUTTON");
			if (closeAreaPanelButton != null)
				closeAreaPanelButton.OnClick = () => editor.DefaultBrush.HideAreaPanel();

			var findInBrowserButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_FIND_BUTTON");
			if (findInBrowserButton != null)
			{
				findInBrowserButton.IsVisible = () =>
					(templateBoundsOverlay != null && templateBoundsOverlay.Enabled)
					|| (actorBoundsOverlay != null && actorBoundsOverlay.Enabled);
				findInBrowserButton.OnClick = FindSelectionInAssetBrowser;
				findInBrowserButton.IsDisabled = () => !CanFindSelectionInAssetBrowser();
			}

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
			var selection = editor.DefaultBrush.Selection;
			return EditorBlit.CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				selection.Area.Value,
				selectionFilters,
				selection.GetAreaMask());
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
				&& editor.CurrentBrush is not EditorTileBrush and not EditorActorBrush and not EditorResourceBrush;
		}

		(EditorBlitSource Source, MapBlitFilters Filters)? GetAreaPreviewSource()
		{
			if (editor.HasClipboard)
				return (editor.Clipboard.Value, selectionFilters);

			if (!ShowAreaPreview())
				return null;

			var selection = editor.DefaultBrush.Selection;
			var region = selection.Area.Value;
			var mask = selection.GetAreaMask();
			var previewCells = mask != null ? new HashSet<CPos>(mask) : null;
			if (cachedPreviewSource == null || cachedPreviewRegion is not CellCoordsRegion cachedRegion ||
				cachedRegion.TopLeft != region.TopLeft || cachedRegion.BottomRight != region.BottomRight ||
				cachedPreviewFilters != selectionFilters ||
				cachedPreviewTemplateType != selection.TemplatePlacementType ||
				cachedPreviewTemplateAnchor != selection.TemplatePlacementAnchor ||
				cachedPreviewDisplayMode != placementPreviewDisplayMode ||
				!PreviewCellsMatch(cachedPreviewCells, previewCells))
			{
				cachedPreviewRegion = region;
				cachedPreviewFilters = selectionFilters;
				cachedPreviewCells = previewCells;
				cachedPreviewTemplateType = selection.TemplatePlacementType;
				cachedPreviewTemplateAnchor = selection.TemplatePlacementAnchor;
				cachedPreviewDisplayMode = placementPreviewDisplayMode;
				cachedPreviewSource = EditorBlit.CopyRegionContents(
					map,
					editorActorLayer,
					resourceLayer,
					region,
					selectionFilters,
					mask);
			}

			return (cachedPreviewSource.Value, selectionFilters);
		}

		bool ShowTilePlacementPreviewControls()
		{
			return templateBoundsOverlay != null && templateBoundsOverlay.Enabled
				&& ShowAreaPreview()
				&& editor.DefaultBrush.Selection.HasTemplatePlacementContext;
		}

		bool ShowActorOverlayPreview()
		{
			return actorBoundsOverlay != null && actorBoundsOverlay.Enabled
				&& editor.DefaultBrush.Selection.Actor != null
				&& editor.DefaultBrush.AreaPanelOpen;
		}

		void SetPlacementPreviewDisplayMode(TilePlacementPreviewDisplayMode mode)
		{
			if (placementPreviewDisplayMode == mode)
				return;

			placementPreviewDisplayMode = mode;
			InvalidateAreaPreview();
			UpdateAreaPreview();
		}

		TemplatePlacementPreviewDisplay? GetTemplatePlacementDisplay()
		{
			if (!ShowTilePlacementPreviewControls())
				return null;

			var selection = editor.DefaultBrush.Selection;
			return new TemplatePlacementPreviewDisplay(
				new TemplatePlacementPreview(
					selection.TemplatePlacementAnchor.Value,
					selection.TemplatePlacementType.Value),
				placementPreviewDisplayMode);
		}

		void InvalidateAreaPreview()
		{
			cachedPreviewSource = null;
			cachedPreviewRegion = null;
			cachedPreviewCells = null;
			cachedPreviewTemplateType = null;
			cachedPreviewTemplateAnchor = null;
			cachedPreviewDisplayMode = TilePlacementPreviewDisplayMode.Current;
		}

		void UpdateAreaPreview()
		{
			selectedPreviewLabel.GetText = () => FluentProvider.GetMessage(SelectedAreaPreview);
			LayoutClipboardPreview();

			if (editor.HasClipboard || ShowAreaPreview())
				clipboardPreview.PrepareRenderables();
		}

		void UpdateSelectedPreview()
		{
			ClearMultiPreview();
			LayoutClipboardPreview();
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
				selectedResourcePreview.IsVisible = () => false;
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
				selectedResourcePreview.IsVisible = () => false;
			}
			else if (editor.CurrentBrush is EditorResourceBrush resourceBrush)
			{
				selectedResourcePreview.SetResourceType(resourceBrush.ResourceType);
				ScaleResourcePreview();
				selectedResourcePreview.IsVisible = () => true;
				selectedTilePreview.IsVisible = () => false;
				selectedActorPreview.IsVisible = () => false;
			}
			else if (ShowActorOverlayPreview())
			{
				selectedActorPreview.SetPreview(editor.DefaultBrush.Selection.Actor.Export());
				ScaleActorPreview();
				selectedActorPreview.IsVisible = () => ShowActorOverlayPreview();
				selectedTilePreview.IsVisible = () => false;
				selectedResourcePreview.IsVisible = () => false;
			}
			else
			{
				selectedTilePreview.IsVisible = () => false;
				selectedActorPreview.IsVisible = () => false;
				selectedResourcePreview.IsVisible = () => false;
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

		void AddFillAction(IEditorAction fillAction)
		{
			if (editor.AssetFillMode == EditorFillMode.Delete)
			{
				var selection = editor.DefaultBrush.Selection;
				editorActionManager.Add(new ClearAndFillEditorAction(
					new DeleteAreaAction(
						map,
						MapBlitFilters.All,
						selection.Area.Value,
						selection.GetAreaMask(),
						resourceLayer,
						editorActorLayer),
					fillAction));
			}
			else
				editorActionManager.Add(fillAction);
		}

		bool HasMultiAssetSelection()
		{
			return editor.CurrentBrush is EditorTileBrush { Templates.Length: > 1 }
				|| editor.CurrentBrush is EditorActorBrush { Actors.Length: > 1 };
		}

		int GetFillDensityPercent()
		{
			return editor.AssetFillDensity;
		}

		void ClearMultiPreview()
		{
			foreach (var preview in multiPreviewWidgets)
				selectedAssetPreviewBox.RemoveChild(preview);

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
				preview.Scale = LayoutPreviewInGridCell(preview, preview.IdealPreviewSize, MultiPreviewCell(i, previews.Length, bounds));
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedAssetPreviewBox.AddChild(preview);
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
				preview.Scale = LayoutPreviewInGridCell(preview, preview.IdealPreviewSize, MultiPreviewCell(i, previews.Length, bounds));
				preview.PrepareRenderables();
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedAssetPreviewBox.AddChild(preview);
			}
		}

		Rectangle AssetPreviewContentBounds()
		{
			var inset = (int)Math.Round(SelectionPreviewBoxSize * (1 - SelectionPreviewImageScale) / 2);
			var contentSize = SelectionPreviewBoxSize - 2 * inset;
			return new Rectangle(inset, inset, contentSize, contentSize);
		}

		Rectangle MultiPreviewBounds()
		{
			return AssetPreviewContentBounds();
		}

		void LayoutClipboardPreview()
		{
			var bounds = AssetPreviewContentBounds();
			clipboardPreview.Bounds.X = bounds.X;
			clipboardPreview.Bounds.Y = bounds.Y;
			clipboardPreview.Bounds.Width = bounds.Width;
			clipboardPreview.Bounds.Height = bounds.Height;
		}

		static int MultiPreviewColumns(int count)
		{
			return Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
		}

		static Rectangle MultiPreviewCell(int index, int count, Rectangle bounds)
		{
			var columns = MultiPreviewColumns(count);
			var rows = (count + columns - 1) / columns;
			var width = bounds.Width / columns;
			var height = bounds.Height / rows;

			return new Rectangle(
				bounds.X + index % columns * width,
				bounds.Y + index / columns * height,
				width,
				height);
		}

		static float LayoutPreviewInGridCell(Widget preview, int2 idealSize, Rectangle cell)
		{
			var scale = PreviewScale(idealSize, cell.Width, cell.Height);
			var width = Math.Max(1, (int)(scale * idealSize.X));
			var height = Math.Max(1, (int)(scale * idealSize.Y));
			preview.Bounds.X = cell.X + (cell.Width - width) / 2;
			preview.Bounds.Y = cell.Y + (cell.Height - height) / 2;
			preview.Bounds.Width = width;
			preview.Bounds.Height = height;
			return scale;
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
			selectedActorPreview.Bounds.Width = (int)(scale * selectedActorPreview.IdealPreviewSize.X);
			selectedActorPreview.Bounds.Height = (int)(scale * selectedActorPreview.IdealPreviewSize.Y);
			selectedActorPreview.Bounds.X = bounds.X + (bounds.Width - selectedActorPreview.Bounds.Width) / 2;
			selectedActorPreview.Bounds.Y = bounds.Y + (bounds.Height - selectedActorPreview.Bounds.Height) / 2;
		}

		void ScaleResourcePreview()
		{
			var bounds = MultiPreviewBounds();
			var idealSize = new int2(selectedResourcePreview.IdealPreviewSize.Width, selectedResourcePreview.IdealPreviewSize.Height);
			var scale = PreviewScale(idealSize, bounds.Width, bounds.Height);
			selectedResourcePreview.Scale = scale;
			selectedResourcePreview.Bounds.Width = (int)(scale * selectedResourcePreview.IdealPreviewSize.Width);
			selectedResourcePreview.Bounds.Height = (int)(scale * selectedResourcePreview.IdealPreviewSize.Height);
			selectedResourcePreview.Bounds.X = bounds.X + (bounds.Width - selectedResourcePreview.Bounds.Width) / 2;
			selectedResourcePreview.Bounds.Y = bounds.Y + (bounds.Height - selectedResourcePreview.Bounds.Height) / 2;
		}

		static float PreviewScale(int2 idealSize, int maxWidth, int maxHeight)
		{
			if (idealSize.X <= 0 || idealSize.Y <= 0)
				return 1f;

			return Math.Min(maxWidth / (float)idealSize.X, maxHeight / (float)idealSize.Y);
		}

		void HandleSelectionChanged()
		{
			placementPreviewDisplayMode = TilePlacementPreviewDisplayMode.Current;
			InvalidateAreaPreview();
			UpdateAreaPreview();
			UpdateSelectedPreview();

			var selection = editor.DefaultBrush.Selection;
			if (!selection.Area.HasValue)
				return;

			var selectedRegion = selection.Area.Value;

			if (editorResourceLayer == null)
				return;

			var selectionSize = selectedRegion.BottomRight - selectedRegion.TopLeft + new CPos(1, 1);
			var diagonalLength = Math.Round(Math.Sqrt(Math.Pow(selectionSize.X, 2) + Math.Pow(selectionSize.Y, 2)), 3);
			var resourceValueInRegion = CalculateSelectionResourceValue(selection);
			var isSolidRectangle = selection.GetAreaMask() == null;
			var dimensionsLabel = isSolidRectangle
				? DimensionsAsString(selectionSize)
				: $"{selection.EnumerateAreaCells().Count()} cells";

			var areaSelectionLabel =
				$"{FluentProvider.GetMessage(AreaSelection)} ({dimensionsLabel}) " +
				$"{PositionAsString(selectedRegion.TopLeft)} : {PositionAsString(selectedRegion.BottomRight)}";

			AreaEditTitle.GetText = () => areaSelectionLabel;
			DiagonalLabel.GetText = () => $"{diagonalLength}";
			ResourceCounterLabel.GetText = () => $"${resourceValueInRegion:N0}";
		}

		int CalculateSelectionResourceValue(EditorSelection selection)
		{
			return editorResourceLayer.CalculateCellsValue(selection.EnumerateAreaCells());
		}

		static bool PreviewCellsMatch(HashSet<CPos> cached, HashSet<CPos> current)
		{
			if (cached == null && current == null)
				return true;

			if (cached == null || current == null || cached.Count != current.Count)
				return false;

			return cached.SetEquals(current);
		}

		static string PositionAsString(CPos cell) => $"{cell.X},{cell.Y}";
		static string DimensionsAsString(CPos cell) => $"{cell.X}x{cell.Y}";

		bool CanFindSelectionInAssetBrowser()
		{
			return TryCreateLocateRequest(out _);
		}

		void FindSelectionInAssetBrowser()
		{
			if (!TryCreateLocateRequest(out var request))
				return;

			editor.RequestLocateAsset(request);
		}

		bool TryCreateLocateRequest(out EditorLocateAssetRequest request)
		{
			request = default;
			var selection = editor.DefaultBrush.Selection;

			if (selection.TemplatePlacementType is ushort templateId)
			{
				request = EditorLocateAssetRequest.ForTile(templateId);
				return true;
			}

			if (selection.Actor != null)
			{
				request = EditorLocateAssetRequest.ForActor(selection.Actor.Info);
				return true;
			}

			if (!selection.Area.HasValue)
				return false;

			if (selectionFilters.HasFlag(MapBlitFilters.Actors))
			{
				var actors = editorActorLayer.PreviewsInCellRegion(selection.Area.Value)
					.Select(p => p.Info)
					.Distinct()
					.ToArray();

				if (actors.Length == 1)
				{
					request = EditorLocateAssetRequest.ForActor(actors[0]);
					return true;
				}
			}

			if (selectionFilters.HasFlag(MapBlitFilters.Terrain))
			{
				var dominantTemplate = GetDominantTemplateInSelection(selection);
				if (dominantTemplate.HasValue)
				{
					request = EditorLocateAssetRequest.ForTile(dominantTemplate.Value);
					return true;
				}
			}

			if (selectionFilters.HasFlag(MapBlitFilters.Resources) && resourceLayer != null)
			{
				var resourceTypes = selection.EnumerateAreaCells()
					.Select(c => resourceLayer.GetResource(c).Type)
					.Where(t => !string.IsNullOrEmpty(t))
					.Distinct()
					.ToArray();

				if (resourceTypes.Length == 1)
				{
					request = EditorLocateAssetRequest.ForResource(resourceTypes[0]);
					return true;
				}
			}

			return false;
		}

		ushort? GetDominantTemplateInSelection(EditorSelection selection)
		{
			var counts = new Dictionary<ushort, int>();
			foreach (var cell in selection.EnumerateAreaCells())
			{
				if (!map.Contains(cell))
					continue;

				var type = map.Tiles[cell].Type;
				counts.TryGetValue(type, out var count);
				counts[type] = count + 1;
			}

			if (counts.Count == 0)
				return null;

			return counts.MaxBy(kvp => kvp.Value).Key;
		}
	}
}
