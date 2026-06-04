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
using OpenRA.Traits;
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

		[FluentReference]
		const string Opposites = "label-editor-opposites";

		[FluentReference]
		const string OppositesIsland = "label-editor-opposites-island";

		[FluentReference]
		const string OppositesRing = "label-editor-opposites-ring";

		[FluentReference]
		const string ShowSimilar = "button-editor-show-similar";

		[FluentReference]
		const string ClearSimilar = "button-editor-clear-similar";

		const float SelectionPreviewImageScale = 0.8f;
		const int SelectionPanelContentMargin = 10;
		const int SelectionPreviewLabelHeight = 20;
		const int SelectionPreviewControlsGap = 6;
		const int MixModeRowHeight = 25;
		const int TilePreviewModeRowHeight = 22;
		const int FillControlRowHeight = 25;
		const int FillModeRowHeight = 25;
		const int SelectionPreviewBoxSize = 148;
		const int SelectionPreviewFullHeight = 180;
		const int SelectionAssetSizeLabelWidth = 36;
		const int SelectionAssetSizeLabelHeight = 16;
		const int SelectionAssetSizeLabelGap = 4;
		const int OppositesPreviewSections = 3;
		const int OppositesSectionCellSpan = 5;
		const int OppositesPreviewSlots = OppositesPreviewSections * OppositesPreviewSections;
		const int OppositesLabelHeight = 20;
		const int OppositesModeButtonHeight = 22;
		const int OppositesSectionGap = 6;
		const int OppositesBoxInset = 4;
		const int SimilarFilterButtonHeight = 22;
		const int SimilarFilterButtonGap = 6;

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
		readonly TerrainTemplatePreviewWidget similarTilePreview;
		readonly ActorPreviewWidget similarActorPreview;
		readonly ActorPreviewWidget selectedActorPreview;
		readonly ResourcePreviewWidget selectedResourcePreview;
		readonly EditorBlitPreviewWidget clipboardPreview;
		readonly EditorSelectionPreviewGridWidget previewGridWidget;
		readonly EditorSelectionPreviewBorderWidget previewBorderWidget;
		readonly LabelWidget selectedPreviewLabel;
		readonly ButtonWidget tilePreviewCurrentButton;
		readonly ButtonWidget tilePreviewOriginalButton;
		readonly ButtonWidget showSimilarButton;
		readonly ButtonWidget clearSimilarButton;
		readonly LabelWidget mixModeLabel;
		readonly DropDownButtonWidget mixModeDropDown;
		readonly LabelWidget fillSpaceLabel;
		readonly SliderWidget fillSpaceSlider;
		readonly LabelWidget fillSpaceValue;
		readonly LabelWidget fillModeOverlapLabel;
		readonly SliderWidget fillModeSlider;
		readonly LabelWidget fillModeDeleteLabel;
		readonly Widget selectedPreviewPanel;
		readonly Widget selectedAssetPreviewBox;
		readonly LabelWidget oppositesLabel;
		readonly ButtonWidget oppositesIslandButton;
		readonly ButtonWidget oppositesRingButton;
		readonly Widget oppositesPreviewBox;
		EditorOppositesMode oppositesMode = EditorOppositesMode.Island;
		readonly EditorSelectionPreviewGridWidget oppositesPreviewGridWidget;
		readonly EditorSelectionPreviewBorderWidget oppositesSelectedBorderWidget;
		readonly EditorOppositesPreviewClickWidget oppositesClickWidget;
		readonly Widget tileDetailPanel;
		readonly LabelWidget tileDetailInfo;
		readonly Widget actorBrushDetailPanel;
		readonly LabelWidget actorBrushDetailInfo;
		readonly Widget actorEditPanel;
		readonly WorldRenderer worldRenderer;
		readonly List<Widget> multiPreviewWidgets = [];
		readonly List<TerrainTemplatePreviewWidget> oppositesPreviewWidgets = [];
		TerrainTemplateInfo[] currentOpposites = [];
		readonly LabelWidget assetSizeLabelTemplate;
		readonly List<LabelWidget> assetSizeLabels = [];
		readonly int maxTemplateCellSpan;
		EditorTileMetadata editorTileMetadata;
		PreviewCellLayout? currentPreviewLayout;
		int similarPreviewIndex;
		bool previewBordersEnabled = true;
		bool similarBrowserFilterActive;
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
			editorTileMetadata = EditorTileMetadata.Load(Game.ModData, map.Rules.TerrainInfo as ITemplatedTerrainInfo);
			if (EditorTileMetadataTraining.Instance != null)
				EditorTileMetadataTraining.Instance.Changed += ReloadEditorTileMetadata;

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
			editor.BrushChanged += HandleAssetSelectionChanged;
			var selectTabContainer = widget.Get("SELECT_WIDGETS");
			var areaEditPanel = selectTabContainer.Get("AREA_EDIT_PANEL");
			selectedPreviewPanel = areaEditPanel.Get("SELECTION_PREVIEW_PANEL");
			actorEditPanel = selectedPreviewPanel.Get("ACTOR_EDIT_PANEL");
			tileDetailPanel = selectedPreviewPanel.Get("SELECTION_TILE_DETAIL");
			tileDetailInfo = tileDetailPanel.Get<LabelWidget>("SELECTION_TILE_DETAIL_INFO");
			actorBrushDetailPanel = selectedPreviewPanel.Get("SELECTION_ACTOR_BRUSH_DETAIL");
			actorBrushDetailInfo = actorBrushDetailPanel.Get<LabelWidget>("SELECTION_ACTOR_BRUSH_DETAIL_INFO");

			actorEditPanel.IsVisible = () => editor.CurrentBrush == editor.DefaultBrush && ShowSelectedMapActorPreview();
			actorBrushDetailPanel.IsVisible = ShowSelectedActorBrushDetail;
			tileDetailPanel.IsVisible = ShowSelectedTileDetail;
			areaEditPanel.IsVisible = () => editor.DefaultBrush.AreaPanelOpen || editor.HasClipboard;

			var copyTerrainCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_TERRAIN_CHECKBOX");
			var copyResourcesCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_RESOURCES_CHECKBOX");
			var copyActorsCheckbox = areaEditPanel.Get<CheckboxWidget>("COPY_FILTER_ACTORS_CHECKBOX");

			copyTerrainCheckbox.IsDisabled = () => false;
			copyResourcesCheckbox.IsDisabled = () => false;
			copyActorsCheckbox.IsDisabled = () => false;

			SetupCopyPasteButton(widget.Get<ButtonWidget>("COPY_BUTTON"));
			SetupCopyPasteButton(areaEditPanel.Get<ButtonWidget>("SELECTION_COPY_BUTTON"));
			var cutAreaSelectionButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_CUT_BUTTON");
			if (cutAreaSelectionButton != null)
			{
				cutAreaSelectionButton.OnClick = CutSelection;
				cutAreaSelectionButton.IsDisabled = () => !editor.DefaultBrush.Selection.Area.HasValue;
			}

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
			var areaInfoTitle = areaEditPanel.Get<LabelWidget>("AREA_INFO_TITLE");
			DiagonalLabel = areaEditPanel.Get<LabelWidget>("DIAGONAL_COUNTER_LABEL");
			ResourceCounterLabel = areaEditPanel.Get<LabelWidget>("RESOURCES_COUNTER_LABEL");
			var diagonalTitle = areaEditPanel.Get<LabelWidget>("DIAGONAL_LABEL");
			var resourceTitle = areaEditPanel.Get<LabelWidget>("RESOURCE_LABEL");
			var showAreaInfo = ShowAreaInfoSection;
			areaInfoTitle.IsVisible = diagonalTitle.IsVisible = resourceTitle.IsVisible = showAreaInfo;
			DiagonalLabel.IsVisible = ResourceCounterLabel.IsVisible = showAreaInfo;
			selectedAssetPreviewBox = selectedPreviewPanel.Get("SELECTION_ASSET_PREVIEW_BOX");
			previewGridWidget = selectedAssetPreviewBox.Get<EditorSelectionPreviewGridWidget>("SELECTION_ASSET_PREVIEW_GRID");
			previewGridWidget.IsVisible = () => false;
			previewBorderWidget = selectedAssetPreviewBox.Get<EditorSelectionPreviewBorderWidget>("SELECTION_ASSET_PREVIEW_BORDER");
			previewBorderWidget.IsVisible = () => false;
			maxTemplateCellSpan = ComputeMaxTemplateCellSpan();
			assetSizeLabelTemplate = selectedPreviewPanel.GetOrNull<LabelWidget>("SELECTION_ASSET_SIZE_LABEL_TEMPLATE");
			selectedPreviewLabel = selectedPreviewPanel.Get<LabelWidget>("SELECTION_PREVIEW_LABEL");
			oppositesLabel = selectedPreviewPanel.Get<LabelWidget>("OPPOSITES_LABEL");
			oppositesIslandButton = selectedPreviewPanel.Get<ButtonWidget>("OPPOSITES_MODE_ISLAND_BUTTON");
			oppositesRingButton = selectedPreviewPanel.Get<ButtonWidget>("OPPOSITES_MODE_RING_BUTTON");
			oppositesPreviewBox = selectedPreviewPanel.Get("OPPOSITES_PREVIEW_BOX");
			oppositesPreviewGridWidget = oppositesPreviewBox.Get<EditorSelectionPreviewGridWidget>("OPPOSITES_PREVIEW_GRID");
			oppositesSelectedBorderWidget = oppositesPreviewBox.Get<EditorSelectionPreviewBorderWidget>("OPPOSITES_SELECTED_BORDER");
			oppositesClickWidget = oppositesPreviewBox.Get<EditorOppositesPreviewClickWidget>("OPPOSITES_CLICK_TARGET");
			similarTilePreview = selectedAssetPreviewBox.Get<TerrainTemplatePreviewWidget>("SIMILAR_TILE_PREVIEW");
			similarActorPreview = selectedAssetPreviewBox.Get<ActorPreviewWidget>("SIMILAR_ACTOR_PREVIEW");
			selectedTilePreview = selectedAssetPreviewBox.Get<TerrainTemplatePreviewWidget>("SELECTION_TILE_PREVIEW");
			selectedActorPreview = selectedAssetPreviewBox.Get<ActorPreviewWidget>("SELECTION_ACTOR_PREVIEW");
			selectedResourcePreview = selectedAssetPreviewBox.Get<ResourcePreviewWidget>("SELECTION_RESOURCE_PREVIEW");
			clipboardPreview = selectedAssetPreviewBox.Get<EditorBlitPreviewWidget>("SELECTION_CLIPBOARD_PREVIEW");
			clipboardPreview.SetPreviewSource(GetAreaPreviewSource);
			clipboardPreview.SetPlacementDisplay(GetTemplatePlacementDisplay);
			clipboardPreview.IsVisible = () => (editor.HasClipboard || ShowAreaPreview()) &&
				!ShowSelectedMapActorPreview() && !ShowSimilarTemplatePreview() && !ShowSelectedTileDetail()
				&& !ShowSelectedActorBrushDetail();
			selectedAssetPreviewBox.RemoveChild(similarTilePreview);
			selectedAssetPreviewBox.AddChild(similarTilePreview);
			selectedAssetPreviewBox.RemoveChild(similarActorPreview);
			selectedAssetPreviewBox.AddChild(similarActorPreview);
			EnsureBorderWidgetOnTop();
			SetupOppositesPreview();

			var previousSimilarButton = selectedPreviewPanel.Get<ButtonWidget>("SIMILAR_PREVIEW_PREVIOUS_BUTTON");
			var nextSimilarButton = selectedPreviewPanel.Get<ButtonWidget>("SIMILAR_PREVIEW_NEXT_BUTTON");
			previousSimilarButton.IsVisible = nextSimilarButton.IsVisible = ShowSimilarCarouselControls;
			previousSimilarButton.OnClick = () => CycleSimilarPreview(-1);
			nextSimilarButton.OnClick = () => CycleSimilarPreview(1);

			showSimilarButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("SHOW_SIMILAR_BUTTON");
			clearSimilarButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("CLEAR_SIMILAR_BUTTON");
			if (showSimilarButton != null && clearSimilarButton != null)
			{
				var showSimilarFilterButtons = () => ShowSimilarFilterButtons();
				showSimilarButton.IsVisible = showSimilarFilterButtons;
				clearSimilarButton.IsVisible = showSimilarFilterButtons;
				showSimilarButton.OnClick = () => ApplySimilarBrowserFilter(scrollToAsset: true);
				clearSimilarButton.OnClick = ClearSimilarBrowserFilter;
			}

			oppositesClickWidget.GridWidth = OppositesPreviewSections;
			oppositesClickWidget.GridHeight = OppositesPreviewSections;
			oppositesClickWidget.OnClickSlot = SelectOppositesSlot;

			tilePreviewCurrentButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("TILE_PREVIEW_CURRENT_BUTTON");
			tilePreviewOriginalButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("TILE_PREVIEW_ORIGINAL_BUTTON");
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

			mixModeLabel = selectedPreviewPanel.Get<LabelWidget>("MIX_MODE_LABEL");
			mixModeDropDown = selectedPreviewPanel.Get<DropDownButtonWidget>("MIX_MODE_DROPDOWN");
			mixModeLabel.IsVisible = mixModeDropDown.IsVisible = () => ShowMixModeControls();
			mixModeDropDown.GetText = () => MixModeText(editor.AssetMixMode);
			mixModeDropDown.OnClick = () => ShowMixModeDropDown(mixModeDropDown);

			fillSpaceLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_SPACE_LABEL");
			fillSpaceSlider = selectedPreviewPanel.Get<SliderWidget>("FILL_SPACE_SLIDER");
			fillSpaceValue = selectedPreviewPanel.Get<LabelWidget>("FILL_SPACE_VALUE");
			fillSpaceLabel.IsVisible = fillSpaceSlider.IsVisible = fillSpaceValue.IsVisible = () => ShowFillControls();
			fillSpaceSlider.MinimumValue = 10;
			fillSpaceSlider.MaximumValue = 100;
			fillSpaceSlider.Ticks = 10;
			fillSpaceSlider.GetValue = () => editor.AssetFillDensity;
			fillSpaceSlider.OnChange += value => editor.SetAssetFillDensity((int)value);
			fillSpaceValue.GetText = () => $"{editor.AssetFillDensity.ToString(NumberFormatInfo.InvariantInfo)}%";

			fillModeOverlapLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_MODE_OVERLAP_LABEL");
			fillModeSlider = selectedPreviewPanel.Get<SliderWidget>("FILL_MODE_SLIDER");
			fillModeDeleteLabel = selectedPreviewPanel.Get<LabelWidget>("FILL_MODE_DELETE_LABEL");
			fillModeOverlapLabel.IsVisible = fillModeSlider.IsVisible = fillModeDeleteLabel.IsVisible = () => ShowFillControls();
			fillModeSlider.MinimumValue = 0;
			fillModeSlider.MaximumValue = 1;
			fillModeSlider.Ticks = 2;
			fillModeSlider.GetValue = () => editor.AssetFillMode == EditorFillMode.Delete ? 1 : 0;
			fillModeSlider.OnChange += value => editor.SetAssetFillMode(value >= 0.5f ? EditorFillMode.Delete : EditorFillMode.Overlap);

			var showPreviewBorderToggle = () => HasMultiAssetSelection()
				&& !ShowTilePlacementPreviewControls() && !ShowSelectedMapActorPreview()
				&& editor.CurrentBrush is EditorTileBrush or EditorActorBrush;

			var previewBorderCheckbox = selectedPreviewPanel.GetOrNull<CheckboxWidget>("SELECTION_PREVIEW_BORDER_CHECKBOX");
			if (previewBorderCheckbox != null)
			{
				previewBorderCheckbox.IsVisible = showPreviewBorderToggle;
				previewBorderCheckbox.IsChecked = () => previewBordersEnabled;
				previewBorderCheckbox.OnClick = () =>
				{
					previewBordersEnabled ^= true;
					ApplyPreviewBorders();
				};
			}

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
						editor.AssetFillMode,
						selection.Area.Value,
						selection.GetAreaMask()));
			};

			fillAreaSelectionButton.IsDisabled = () => editor.CurrentBrush is not EditorTileBrush
				&& editor.CurrentBrush is not EditorActorBrush
				&& (editor.CurrentBrush is not EditorResourceBrush || resourceLayer == null);

			var replaceAreaSelectionButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_REPLACE_BUTTON");
			if (replaceAreaSelectionButton != null)
			{
				replaceAreaSelectionButton.OnClick = () =>
				{
					if (!editor.DefaultBrush.Selection.Area.HasValue)
						return;

					Game.OpenWindow(world, "EDITOR_REPLACE_PANEL");
				};
				replaceAreaSelectionButton.IsDisabled = () => !editor.DefaultBrush.Selection.Area.HasValue;
			}

			var closeAreaSelectionButton = areaEditPanel.Get<ButtonWidget>("SELECTION_CANCEL_BUTTON");
			closeAreaSelectionButton.OnClick = () => editor.DefaultBrush.CloseAreaPanel();

			var closeAreaPanelButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_CLOSE_BUTTON");
			if (closeAreaPanelButton != null)
				closeAreaPanelButton.OnClick = () => editor.DefaultBrush.HideAreaPanel();

			var helpButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_INFO_BUTTON");
			if (helpButton != null)
				helpButton.OnClick = () => Game.OpenWindow(world, "EDITOR_HELP_PANEL");

			var findInBrowserButton = areaEditPanel.GetOrNull<ButtonWidget>("SELECTION_FIND_BUTTON");
			if (findInBrowserButton != null)
			{
				findInBrowserButton.IsVisible = () => ShowSelectedMapActorPreview()
					|| ShowSelectedTileDetail() || ShowSelectedActorBrushDetail();
				findInBrowserButton.OnClick = FindSelectionInAssetBrowser;
				findInBrowserButton.IsDisabled = () => !CanFindSelectionInAssetBrowser();
			}

			CreateCategoryPanel(MapBlitFilters.Terrain, copyTerrainCheckbox);
			CreateCategoryPanel(MapBlitFilters.Resources, copyResourcesCheckbox);
			CreateCategoryPanel(MapBlitFilters.Actors, copyActorsCheckbox);
			AreaEditTitle.GetText = () => FluentProvider.GetMessage(SelectedAreaPreview);
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

		void CutSelection()
		{
			if (!editor.DefaultBrush.Selection.Area.HasValue)
				return;

			CopySelection();
			editor.DefaultBrush.DeleteSelection(selectionFilters);
			InvalidateAreaPreview();
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

		void ReloadEditorTileMetadata()
		{
			editorTileMetadata = EditorTileMetadata.Load(Game.ModData, map.Rules.TerrainInfo as ITemplatedTerrainInfo);
			UpdateOppositesPreview();
			UpdateSimilarPreview();
		}

		protected override void Dispose(bool disposing)
		{
			if (EditorTileMetadataTraining.Instance != null)
				EditorTileMetadataTraining.Instance.Changed -= ReloadEditorTileMetadata;
			editor.DefaultBrush.SelectionChanged -= HandleSelectionChanged;
			editor.BrushChanged -= UpdateSelectedPreview;
			editor.BrushChanged -= UpdateAreaPreview;
			editor.BrushChanged -= HandleAssetSelectionChanged;
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
			return ShowSelectedTileDetail()
				&& editor.DefaultBrush.Selection.HasTemplatePlacementContext;
		}

		bool ShowSelectedMapActorPreview()
		{
			return editor.CurrentBrush == editor.DefaultBrush
				&& editor.DefaultBrush.Selection.Actor != null
				&& editor.DefaultBrush.AreaPanelOpen;
		}

		bool ShowSelectedActorBrushDetail()
		{
			return editor.CurrentBrush is EditorActorBrush { Actors.Length: 1 }
				&& editor.DefaultBrush.AreaPanelOpen;
		}

		bool ShowSelectedTileDetail()
		{
			if (!editor.DefaultBrush.AreaPanelOpen)
				return false;

			if (editor.CurrentBrush is EditorTileBrush)
				return true;

			return editor.DefaultBrush.Selection.Actor == null
				&& editor.DefaultBrush.Selection.HasTemplatePlacementContext;
		}

		bool ShowAreaInfoSection()
		{
			return editor.DefaultBrush.Selection.Area.HasValue
				&& !ShowSelectedMapActorPreview()
				&& !ShowSelectedTileDetail()
				&& !ShowSelectedActorBrushDetail();
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
			LayoutClipboardPreview();

			if (editor.HasClipboard || ShowAreaPreview())
				clipboardPreview.PrepareRenderables();
		}

		void UpdateSelectedPreview()
		{
			ClearMultiPreview();
			ClearAssetSizeLabels();
			LayoutClipboardPreview();
			HideAssetPreviews();
			HidePreviewGrid();

			LayoutSelectionPreviewSection();

			var cellSizes = GetSelectedAssetCellSizes();
			if (cellSizes.Count == 0)
			{
				currentPreviewLayout = null;
				ApplyPreviewBorders();
				UpdateOppositesPreview();
				return;
			}

			LayoutAssetPreviews(cellSizes);
			UpdateAssetSizeLabels(cellSizes);
			UpdateSimilarPreview();
			UpdateOppositesPreview();
		}

		void HideAssetPreviews()
		{
			selectedTilePreview.IsVisible = () => false;
			similarTilePreview.IsVisible = () => false;
			similarActorPreview.IsVisible = () => false;
			selectedActorPreview.IsVisible = () => false;
			selectedResourcePreview.IsVisible = () => false;
		}

		void HidePreviewGrid()
		{
			previewGridWidget.IsVisible = () => false;
			previewGridWidget.GridWidth = 0;
			previewGridWidget.GridHeight = 0;
		}

		bool ShowMixModeControls()
		{
			return editor.CurrentBrush is EditorTileBrush or EditorActorBrush or EditorResourceBrush
				&& !ShowTilePlacementPreviewControls() && !ShowSelectedMapActorPreview();
		}

		bool ShowFillControls()
		{
			return ShowMixModeControls();
		}

		int LayoutSelectionPreviewControls(int rowY)
		{
			if (ShowTilePlacementPreviewControls() && tilePreviewCurrentButton != null && tilePreviewOriginalButton != null)
			{
				rowY += SelectionPreviewControlsGap;
				tilePreviewCurrentButton.Bounds.Y = rowY;
				tilePreviewOriginalButton.Bounds.Y = rowY;
				return rowY + TilePreviewModeRowHeight;
			}

			if (!ShowMixModeControls())
				return rowY;

			rowY += SelectionPreviewControlsGap;
			mixModeLabel.Bounds.Y = rowY + 2;
			mixModeDropDown.Bounds.Y = rowY;
			mixModeDropDown.Bounds.Height = MixModeRowHeight;
			rowY += MixModeRowHeight;

			if (!ShowFillControls())
				return rowY;

			rowY += SelectionPreviewControlsGap;
			fillSpaceLabel.Bounds.Y = rowY;
			fillSpaceSlider.Bounds.Y = rowY;
			fillSpaceValue.Bounds.Y = rowY;
			rowY += FillControlRowHeight;

			fillModeOverlapLabel.Bounds.Y = rowY;
			fillModeSlider.Bounds.Y = rowY - 3;
			fillModeDeleteLabel.Bounds.Y = rowY;
			return rowY + FillModeRowHeight;
		}

		void LayoutSelectionPreviewSection()
		{
			var margin = SelectionPanelContentMargin;
			var panelWidth = selectedPreviewPanel.Bounds.Width;

			selectedPreviewLabel.Bounds.Y = margin;
			selectedPreviewLabel.Bounds.Height = SelectionPreviewLabelHeight;

			var headerBottom = margin + SelectionPreviewLabelHeight;
			var controlsBottom = LayoutSelectionPreviewControls(headerBottom);
			var previewY = (ShowTilePlacementPreviewControls() || ShowMixModeControls())
				? controlsBottom + margin
				: headerBottom + margin;

			selectedAssetPreviewBox.Bounds.X = margin;
			selectedAssetPreviewBox.Bounds.Y = previewY;
			selectedAssetPreviewBox.Bounds.Width = panelWidth - 2 * margin;
			selectedAssetPreviewBox.Bounds.Height = ShowSelectedMapActorPreview()
				? SelectionPreviewBoxSize
				: SelectionPreviewFullHeight;

			var previewBottom = previewY + selectedAssetPreviewBox.Bounds.Height;
			var stackY = previewBottom + margin;

			var previewBorderCheckbox = selectedPreviewPanel.GetOrNull<CheckboxWidget>("SELECTION_PREVIEW_BORDER_CHECKBOX");
			if (previewBorderCheckbox != null)
			{
				previewBorderCheckbox.Bounds.Y = stackY;
				if (previewBorderCheckbox.Visible)
					stackY = previewBorderCheckbox.Bounds.Y + previewBorderCheckbox.Bounds.Height + margin;
			}

			LayoutSimilarFilterButtons(ref stackY);

			tileDetailPanel.Bounds.Y = stackY;
			actorBrushDetailPanel.Bounds.Y = stackY;
			actorEditPanel.Bounds.Y = stackY;

			var similarButtonY = previewY + selectedAssetPreviewBox.Bounds.Height / 2 - 12;
			var previousSimilarButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("SIMILAR_PREVIEW_PREVIOUS_BUTTON");
			var nextSimilarButton = selectedPreviewPanel.GetOrNull<ButtonWidget>("SIMILAR_PREVIEW_NEXT_BUTTON");
			if (previousSimilarButton != null)
			{
				previousSimilarButton.Bounds.X = margin;
				previousSimilarButton.Bounds.Y = similarButtonY;
			}

			if (nextSimilarButton != null)
			{
				nextSimilarButton.Bounds.X = panelWidth - margin - nextSimilarButton.Bounds.Width;
				nextSimilarButton.Bounds.Y = similarButtonY;
			}

			LayoutOppositesSection();
		}

		bool ShowSimilarFilterButtons()
		{
			return ShowSimilarCarouselControls();
		}

		void LayoutSimilarFilterButtons(ref int stackY)
		{
			if (!ShowSimilarFilterButtons() || showSimilarButton == null || clearSimilarButton == null)
				return;

			var margin = SelectionPanelContentMargin;
			var panelWidth = selectedPreviewPanel.Bounds.Width;
			var buttonWidth = (panelWidth - 2 * margin - SimilarFilterButtonGap) / 2;

			showSimilarButton.Bounds.X = margin;
			showSimilarButton.Bounds.Y = stackY;
			showSimilarButton.Bounds.Width = buttonWidth;
			showSimilarButton.Bounds.Height = SimilarFilterButtonHeight;

			clearSimilarButton.Bounds.X = margin + buttonWidth + SimilarFilterButtonGap;
			clearSimilarButton.Bounds.Y = stackY;
			clearSimilarButton.Bounds.Width = buttonWidth;
			clearSimilarButton.Bounds.Height = SimilarFilterButtonHeight;

			stackY += SimilarFilterButtonHeight + margin;
		}

		bool TryGetSimilarFilterActor(out ActorInfo actor)
		{
			actor = null;
			if (ShowSelectedMapActorPreview())
			{
				actor = editor.DefaultBrush.Selection.Actor.Info;
				return true;
			}

			if (TryGetSelectedActor(out _, out var selectedActor))
			{
				actor = selectedActor;
				return true;
			}

			return false;
		}

		bool TryGetSimilarFilterTile(out ushort templateId)
		{
			templateId = 0;
			if (!TryGetSelectedTemplate(out _, out var template))
				return false;

			templateId = template.Id;
			return true;
		}

		void ApplySimilarBrowserFilter(bool scrollToAsset)
		{
			if (TryGetSimilarFilterActor(out var actor))
			{
				similarBrowserFilterActive = true;
				editor.RequestLocateAsset(EditorLocateAssetRequest.ForActor(actor, scrollToAsset));
				return;
			}

			if (TryGetSimilarFilterTile(out var templateId))
			{
				similarBrowserFilterActive = true;
				editor.RequestLocateAsset(EditorLocateAssetRequest.ForTile(templateId, scrollToAsset));
			}
		}

		void ClearSimilarBrowserFilter()
		{
			similarBrowserFilterActive = false;
			editor.RequestLocateAsset(EditorLocateAssetRequest.RestoreAllCategories());
		}

		void ResetSimilarBrowserFilterIfActive()
		{
			if (!similarBrowserFilterActive)
				return;

			ClearSimilarBrowserFilter();
		}

		int GetSelectionContentBottom()
		{
			var margin = SelectionPanelContentMargin;
			var bottom = selectedAssetPreviewBox.Bounds.Y + selectedAssetPreviewBox.Bounds.Height;

			var previewBorderCheckbox = selectedPreviewPanel.GetOrNull<CheckboxWidget>("SELECTION_PREVIEW_BORDER_CHECKBOX");
			if (previewBorderCheckbox != null && previewBorderCheckbox.Visible)
				bottom = Math.Max(bottom, previewBorderCheckbox.Bounds.Y + previewBorderCheckbox.Bounds.Height);

			if (ShowSelectedTileDetail())
				bottom = Math.Max(bottom, tileDetailPanel.Bounds.Y + tileDetailPanel.Bounds.Height);
			if (ShowSelectedActorBrushDetail())
				bottom = Math.Max(bottom, actorBrushDetailPanel.Bounds.Y + actorBrushDetailPanel.Bounds.Height);
			if (ShowSelectedMapActorPreview())
				bottom = Math.Max(bottom, actorEditPanel.Bounds.Y + actorEditPanel.Bounds.Height);

			if (showSimilarButton != null && showSimilarButton.Visible)
				bottom = Math.Max(bottom, showSimilarButton.Bounds.Y + showSimilarButton.Bounds.Height);

			if (oppositesIslandButton != null && oppositesIslandButton.Visible)
				bottom = Math.Max(bottom, oppositesIslandButton.Bounds.Y + oppositesIslandButton.Bounds.Height);

			return bottom + margin;
		}

		void LayoutOppositesSection()
		{
			if (!ShowOppositesPreview())
				return;

			var margin = SelectionPanelContentMargin;
			var panelWidth = selectedPreviewPanel.Bounds.Width;
			var panelHeight = selectedPreviewPanel.Bounds.Height;
			var contentBottom = GetSelectionContentBottom();

			oppositesLabel.Bounds.Y = contentBottom;
			oppositesLabel.Bounds.Height = OppositesLabelHeight;

			var modeButtonY = contentBottom + OppositesLabelHeight + OppositesSectionGap;
			var modeButtonWidth = (panelWidth - 2 * margin - SimilarFilterButtonGap) / 2;
			oppositesIslandButton.Bounds.X = margin;
			oppositesIslandButton.Bounds.Y = modeButtonY;
			oppositesIslandButton.Bounds.Width = modeButtonWidth;
			oppositesIslandButton.Bounds.Height = OppositesModeButtonHeight;
			oppositesIslandButton.IsHighlighted = () => oppositesMode == EditorOppositesMode.Island;

			oppositesRingButton.Bounds.X = margin + modeButtonWidth + SimilarFilterButtonGap;
			oppositesRingButton.Bounds.Y = modeButtonY;
			oppositesRingButton.Bounds.Width = modeButtonWidth;
			oppositesRingButton.Bounds.Height = OppositesModeButtonHeight;
			oppositesRingButton.IsHighlighted = () => oppositesMode == EditorOppositesMode.Ring;

			var boxY = modeButtonY + OppositesModeButtonHeight + OppositesSectionGap;
			var boxWidth = panelWidth - 2 * margin;
			var maxBoxHeight = panelHeight - boxY - margin;
			var boxSize = Math.Max(1, Math.Min(boxWidth, maxBoxHeight));

			oppositesPreviewBox.Bounds.X = margin;
			oppositesPreviewBox.Bounds.Y = boxY;
			oppositesPreviewBox.Bounds.Width = boxSize;
			oppositesPreviewBox.Bounds.Height = boxSize;

			UpdateOppositesPreviewGrid();
			UpdateOppositesClickTarget();
			RelayoutVisibleOppositesPreviews();
		}

		void RelayoutVisibleOppositesPreviews()
		{
			for (var slot = 0; slot < Math.Min(currentOpposites.Length, oppositesPreviewWidgets.Count); slot++)
			{
				var template = currentOpposites[slot];
				if (template == null || !IsOppositesSlotVisible(slot))
					continue;

				var preview = oppositesPreviewWidgets[slot];
				if (!preview.Visible)
					continue;

				LayoutOppositesPreview(preview, template, slot);
			}
		}

		void HidePreviewBorders()
		{
			previewBorderWidget.Clear();
			previewBorderWidget.IsVisible = () => false;
		}

		void LayoutAssetPreviews(IReadOnlyList<CVec> cellSizes)
		{
			var layout = ComputePreviewCellLayout(cellSizes, FullAssetPreviewBounds());
			currentPreviewLayout = layout;
			UpdatePreviewGrid(layout);

			if (TryGetPlacementTemplate(out _, out var placementTemplate))
				LayoutSingleTilePreview(placementTemplate, layout);
			else if (editor.CurrentBrush is EditorTileBrush tileBrush)
				LayoutTilePreviews(tileBrush, layout);
			else if (editor.CurrentBrush is EditorActorBrush actorBrush)
				LayoutActorPreviews(actorBrush, layout);
			else if (editor.CurrentBrush is EditorResourceBrush resourceBrush)
				LayoutResourcePreview(resourceBrush, layout);
			else if (ShowSelectedMapActorPreview())
				LayoutSingleActorPreview(editor.DefaultBrush.Selection.Actor.Export(), layout);
			else if (TryGetSelectedTemplate(out var terrainInfo, out var mapTemplate))
				LayoutSingleTilePreview(mapTemplate, layout);

			ApplyPreviewBorders();
			EnsureBorderWidgetOnTop();
		}

		void LayoutTilePreviews(EditorTileBrush tileBrush, PreviewCellLayout layout)
		{
			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			if (tileBrush.Templates.Length == 1)
			{
				selectedTilePreview.SetTemplate(tileBrush.TerrainTemplate);
				LayoutTerrainPreview(selectedTilePreview, ItemPixelBounds(layout, 0));
				selectedTilePreview.IsVisible = () => true;
				return;
			}

			var templates = tileBrush.Templates.Select(t => terrainInfo.Templates[t]).ToArray();
			for (var i = 0; i < templates.Length; i++)
			{
				var preview = selectedTilePreview.Clone();
				preview.SetTemplate(templates[i]);
				LayoutTerrainPreview(preview, ItemPixelBounds(layout, i));
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedAssetPreviewBox.AddChild(preview);
			}
		}

		void LayoutActorPreviews(EditorActorBrush actorBrush, PreviewCellLayout layout)
		{
			if (actorBrush.Actors.Length == 1)
			{
				selectedActorPreview.SetPreview(actorBrush.Preview.Export());
				LayoutActorPreview(selectedActorPreview, ItemPixelBounds(layout, 0));
				selectedActorPreview.IsVisible = () => true;
				return;
			}

			var references = actorBrush.ActorReferences.Select(a => a.Clone()).ToArray();
			for (var i = 0; i < references.Length; i++)
			{
				var preview = selectedActorPreview.Clone();
				preview.SetPreview(references[i]);
				LayoutActorPreview(preview, ItemPixelBounds(layout, i));
				preview.PrepareRenderables();
				preview.IsVisible = () => true;
				multiPreviewWidgets.Add(preview);
				selectedAssetPreviewBox.AddChild(preview);
			}
		}

		void LayoutSingleActorPreview(ActorReference actor, PreviewCellLayout layout)
		{
			selectedActorPreview.SetPreview(actor);
			LayoutActorPreview(selectedActorPreview, ItemPixelBounds(layout, 0));
			selectedActorPreview.IsVisible = () => ShowSelectedMapActorPreview();
		}

		void LayoutSingleTilePreview(TerrainTemplateInfo template, PreviewCellLayout layout)
		{
			selectedTilePreview.SetTemplate(template);
			LayoutTerrainPreview(selectedTilePreview, ItemPixelBounds(layout, 0));
			selectedTilePreview.IsVisible = ShowSelectedTileDetail;
		}

		void LayoutResourcePreview(EditorResourceBrush resourceBrush, PreviewCellLayout layout)
		{
			selectedResourcePreview.SetResourceType(resourceBrush.ResourceType);
			LayoutResourcePreview(selectedResourcePreview, ItemPixelBounds(layout, 0));
			selectedResourcePreview.IsVisible = () => true;
		}

		bool ShowSimilarCarouselControls()
		{
			return (TryGetSelectedTemplate(out var terrainInfo, out var selectedTemplate) &&
				editorTileMetadata.FindSimilar(terrainInfo, selectedTemplate).Length > 0) ||
				(TryGetSelectedActor(out _, out var selectedActor) &&
				editorTileMetadata.FindSimilarActors(map.Rules, selectedActor).Length > 0);
		}

		bool ShowSimilarTemplatePreview()
		{
			return similarPreviewIndex > 0 &&
				(TryGetSimilarPreviewTemplate(out _) || TryGetSimilarPreviewActor(out _, out _));
		}

		void CycleSimilarPreview(int delta)
		{
			ApplySimilarBrowserFilter(scrollToAsset: false);

			if (TryGetSelectedTemplate(out var terrainInfo, out var selectedTemplate))
			{
				var group = editorTileMetadata.FindSimilarGroup(terrainInfo, selectedTemplate);
				if (group.Length <= 1)
					return;

				var index = Array.FindIndex(group, template => template.Id == selectedTemplate.Id);
				var target = group[(index + delta + group.Length) % group.Length];
				similarPreviewIndex = 0;
				editor.SetBrush(new EditorTileBrush(editor, target.Id, worldRenderer));
				UpdateSelectedPreview();
				UpdateAreaPreview();
				return;
			}

			else if (TryGetSelectedActor(out var actorBrush, out var selectedActor))
			{
				var group = editorTileMetadata.FindSimilarActorGroup(map.Rules, selectedActor);
				if (group.Length <= 1)
					return;

				var index = Array.FindIndex(group, actor => actor == selectedActor);
				var target = group[(index + delta + group.Length) % group.Length];
				similarPreviewIndex = 0;
				var owner = actorBrush?.Owner ?? editor.DefaultBrush.Selection.Actor?.Owner;
				if (owner != null)
					editor.SetBrush(new EditorActorBrush(editor, target, owner, worldRenderer));

				UpdateSelectedPreview();
				UpdateAreaPreview();
			}
		}

		void UpdateSimilarPreview()
		{
			similarTilePreview.IsVisible = () => false;
			similarActorPreview.IsVisible = () => false;
			similarPreviewIndex = 0;
		}

		bool TryGetSimilarPreviewTemplate(out TerrainTemplateInfo template)
		{
			template = null;
			if (!TryGetSelectedTemplate(out var terrainInfo, out var selectedTemplate))
				return false;

			var similar = editorTileMetadata.FindSimilar(terrainInfo, selectedTemplate);
			var index = similarPreviewIndex - 1;
			if (index < 0 || index >= similar.Length)
				return false;

			template = similar[index];
			return true;
		}

		bool TryGetSimilarPreviewActor(out ActorInfo actor, out EditorActorBrush actorBrush)
		{
			actor = null;
			actorBrush = null;
			if (!TryGetSelectedActor(out actorBrush, out var selectedActor))
				return false;

			var similar = editorTileMetadata.FindSimilarActors(map.Rules, selectedActor);
			var index = similarPreviewIndex - 1;
			if (index < 0 || index >= similar.Length)
				return false;

			actor = similar[index];
			return true;
		}

		void SetActorSimilarPreview(ActorInfo actor, PlayerReference owner)
		{
			var td = new TypeDictionary
			{
				new OwnerInit(owner.Name),
				new FactionInit(owner.Faction)
			};

			foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
				foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.MapEditorSidebar))
					td.Add(o);

			similarActorPreview.SetPreview(actor, td);
		}

		bool TryGetSelectedActor(out EditorActorBrush actorBrush, out ActorInfo selectedActor)
		{
			actorBrush = editor.CurrentBrush as EditorActorBrush;
			selectedActor = null;
			if (actorBrush != null && actorBrush.Actors.Length == 1)
			{
				selectedActor = actorBrush.Actors[0];
				return true;
			}

			if (ShowSelectedMapActorPreview())
			{
				selectedActor = editor.DefaultBrush.Selection.Actor.Info;
				return true;
			}

			return false;
		}

		bool TryGetPlacementTemplate(out ITemplatedTerrainInfo terrainInfo, out TerrainTemplateInfo template)
		{
			terrainInfo = null;
			template = null;

			if (map.Rules.TerrainInfo is not ITemplatedTerrainInfo templateInfo)
				return false;

			var selection = editor.DefaultBrush.Selection;
			if (!selection.HasTemplatePlacementContext ||
				!selection.TemplatePlacementType.HasValue ||
				!templateInfo.Templates.TryGetValue(selection.TemplatePlacementType.Value, out template))
				return false;

			terrainInfo = templateInfo;
			return true;
		}

		bool TryGetSelectedTemplate(out ITemplatedTerrainInfo terrainInfo, out TerrainTemplateInfo selectedTemplate)
		{
			terrainInfo = null;
			selectedTemplate = null;

			if (TryGetPlacementTemplate(out terrainInfo, out selectedTemplate))
				return true;

			if (map.Rules.TerrainInfo is not ITemplatedTerrainInfo templateInfo)
				return false;

			if (editor.CurrentBrush is EditorTileBrush { Templates.Length: 1 } tileBrush)
			{
				terrainInfo = templateInfo;
				selectedTemplate = tileBrush.TerrainTemplate;
				return true;
			}

			return false;
		}

		void SetupOppositesPreview()
		{
			oppositesLabel.IsVisible = oppositesPreviewBox.IsVisible = ShowOppositesPreview;
			oppositesIslandButton.IsVisible = oppositesRingButton.IsVisible = ShowOppositesPreview;
			oppositesLabel.GetText = () => FluentProvider.GetMessage(Opposites);
			oppositesIslandButton.OnClick = () => SetOppositesMode(EditorOppositesMode.Island);
			oppositesRingButton.OnClick = () => SetOppositesMode(EditorOppositesMode.Ring);
			UpdateOppositesPreviewGrid();
			oppositesSelectedBorderWidget.IsVisible = () => false;
			oppositesClickWidget.IsVisible = ShowOppositesPreview;
			UpdateOppositesClickTarget();

			for (var i = 0; i < OppositesPreviewSlots; i++)
			{
				var preview = selectedTilePreview.Clone();
				preview.IsVisible = () => false;
				oppositesPreviewWidgets.Add(preview);
				oppositesPreviewBox.AddChild(preview);
			}

			oppositesPreviewBox.RemoveChild(oppositesSelectedBorderWidget);
			oppositesPreviewBox.AddChild(oppositesSelectedBorderWidget);
			oppositesPreviewBox.RemoveChild(oppositesClickWidget);
			oppositesPreviewBox.AddChild(oppositesClickWidget);
		}

		bool ShowOppositesPreview()
		{
			return (editor.CurrentBrush is EditorTileBrush { Templates.Length: 1 } || editor.DefaultBrush.Selection.HasTemplatePlacementContext)
				&& map.Rules.TerrainInfo is ITemplatedTerrainInfo;
		}

		void UpdateOppositesPreview()
		{
			foreach (var preview in oppositesPreviewWidgets)
				preview.IsVisible = () => false;
			HideOppositesSelectedBorder();
			currentOpposites = [];

			if (!TryGetSelectedTemplate(out var terrainInfo, out var selectedTemplate))
				return;

			LayoutOppositesSection();
			var templates = editorTileMetadata.FindOpposites(terrainInfo, selectedTemplate, oppositesMode);
			currentOpposites = templates;
			for (var slot = 0; slot < Math.Min(templates.Length, oppositesPreviewWidgets.Count); slot++)
			{
				var template = templates[slot];
				if (template == null || !IsOppositesSlotVisible(slot))
					continue;

				var preview = oppositesPreviewWidgets[slot];
				preview.SetTemplate(template);
				LayoutOppositesPreview(preview, template, slot);
				preview.IsVisible = ShowOppositesPreview;

				if (template.Id == selectedTemplate.Id)
					ShowOppositesSelectedBorder(slot);
			}
		}

		void SetOppositesMode(EditorOppositesMode mode)
		{
			if (oppositesMode == mode)
				return;

			oppositesMode = mode;
			UpdateOppositesPreview();
		}

		bool IsOppositesSlotVisible(int slot) =>
			EditorTileMetadata.IsOppositesSlotUsed(oppositesMode, slot);

		void SelectOppositesSlot(int slot)
		{
			if (slot < 0 || slot >= currentOpposites.Length || !IsOppositesSlotVisible(slot))
				return;

			var template = currentOpposites[slot];
			if (template == null)
				return;

			// Map placement context overrides the tile brush in the top preview; clear it so the
			// clicked opposite becomes the new main selected asset.
			var selection = editor.DefaultBrush.Selection;
			selection.TemplatePlacementType = null;
			selection.TemplatePlacementAnchor = null;

			similarPreviewIndex = 0;
			editor.SetBrush(new EditorTileBrush(editor, template.Id, worldRenderer));
			UpdateSelectedPreview();
			UpdateAreaPreview();
			UpdateSelectionDetailPanel();
			UpdatePanelTitle();
			editor.RequestLocateAsset(EditorLocateAssetRequest.ForTile(template.Id));
		}

		void HideOppositesSelectedBorder()
		{
			oppositesSelectedBorderWidget.Clear();
			oppositesSelectedBorderWidget.IsVisible = () => false;
		}

		void ShowOppositesSelectedBorder(int slot)
		{
			var square = GetOppositesSquareLayout();
			if (square.SectionSize <= 0)
				return;

			var cell = new CVec(slot % OppositesPreviewSections, slot / OppositesPreviewSections);
			oppositesSelectedBorderWidget.OriginX = square.OffsetX;
			oppositesSelectedBorderWidget.OriginY = square.OffsetY;
			oppositesSelectedBorderWidget.CellPixelSize = square.SectionSize;
			oppositesSelectedBorderWidget.CellRegions = [[cell]];
			oppositesSelectedBorderWidget.IsVisible = ShowOppositesPreview;
		}

		readonly struct OppositesSquareLayout
		{
			public readonly int OffsetX;
			public readonly int OffsetY;
			public readonly int TotalSize;
			public readonly int SectionSize;

			public OppositesSquareLayout(int offsetX, int offsetY, int totalSize, int sectionSize)
			{
				OffsetX = offsetX;
				OffsetY = offsetY;
				TotalSize = totalSize;
				SectionSize = sectionSize;
			}
		}

		OppositesSquareLayout GetOppositesSquareLayout()
		{
			var box = oppositesPreviewBox.Bounds;
			var innerWidth = Math.Max(0, box.Width - 2 * OppositesBoxInset);
			var innerHeight = Math.Max(0, box.Height - 2 * OppositesBoxInset);
			var totalSize = Math.Min(innerWidth, innerHeight);
			if (totalSize <= 0)
				return new OppositesSquareLayout(0, 0, 0, 0);

			return new OppositesSquareLayout(
				OppositesBoxInset + (innerWidth - totalSize) / 2,
				OppositesBoxInset + (innerHeight - totalSize) / 2,
				totalSize,
				totalSize / OppositesPreviewSections);
		}

		void UpdateOppositesPreviewGrid()
		{
			var square = GetOppositesSquareLayout();
			oppositesPreviewGridWidget.Bounds.X = square.OffsetX;
			oppositesPreviewGridWidget.Bounds.Y = square.OffsetY;
			oppositesPreviewGridWidget.Bounds.Width = square.TotalSize;
			oppositesPreviewGridWidget.Bounds.Height = square.TotalSize;
			oppositesPreviewGridWidget.GridWidth = OppositesPreviewSections * OppositesSectionCellSpan;
			oppositesPreviewGridWidget.GridHeight = OppositesPreviewSections * OppositesSectionCellSpan;
			oppositesPreviewGridWidget.IsVisible = ShowOppositesPreview;
		}

		void UpdateOppositesClickTarget()
		{
			var square = GetOppositesSquareLayout();
			oppositesClickWidget.Bounds.X = square.OffsetX;
			oppositesClickWidget.Bounds.Y = square.OffsetY;
			oppositesClickWidget.Bounds.Width = square.TotalSize;
			oppositesClickWidget.Bounds.Height = square.TotalSize;
		}

		void LayoutOppositesPreview(TerrainTemplatePreviewWidget preview, TerrainTemplateInfo template, int slot)
		{
			var square = GetOppositesSquareLayout();
			if (square.SectionSize <= 0)
				return;

			var sectionX = slot % OppositesPreviewSections;
			var sectionY = slot / OppositesPreviewSections;
			var sectionBounds = new Rectangle(
				square.OffsetX + sectionX * square.SectionSize,
				square.OffsetY + sectionY * square.SectionSize,
				square.SectionSize,
				square.SectionSize);

			var templateSize = TileTemplateCellSize(template);
			var span = OppositesSectionCellSpan;
			var cellPixelSize = Math.Max(1, sectionBounds.Width / span);
			var gridPixelSize = span * cellPixelSize;
			var cellOrigin = new CVec((span - templateSize.X) / 2, (span - templateSize.Y) / 2);
			var originX = sectionBounds.X + (sectionBounds.Width - gridPixelSize) / 2;
			var originY = sectionBounds.Y + (sectionBounds.Height - gridPixelSize) / 2;
			var pixelBounds = new Rectangle(
				originX + cellOrigin.X * cellPixelSize,
				originY + cellOrigin.Y * cellPixelSize,
				templateSize.X * cellPixelSize,
				templateSize.Y * cellPixelSize);

			LayoutTerrainPreview(preview, pixelBounds);
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

		void ClearAssetSizeLabels()
		{
			foreach (var label in assetSizeLabels)
				selectedPreviewPanel.RemoveChild(label);

			assetSizeLabels.Clear();
		}

		void UpdateAssetSizeLabels(IReadOnlyList<CVec> cellSizes)
		{
			if (assetSizeLabelTemplate == null || cellSizes.Count == 0)
				return;

			var previewBounds = selectedAssetPreviewBox.Bounds;
			var gridOriginX = currentPreviewLayout?.OriginX ?? previewBounds.X;
			var labelX = Math.Max(
				SelectionPanelContentMargin,
				gridOriginX - SelectionAssetSizeLabelWidth - SelectionAssetSizeLabelGap);

			for (var i = 0; i < cellSizes.Count; i++)
			{
				var sizeText = DimensionsAsString(new CPos(cellSizes[i].X, cellSizes[i].Y));
				var label = assetSizeLabelTemplate.Clone();
				label.GetText = () => sizeText;
				label.Bounds.X = labelX;
				label.Bounds.Y = previewBounds.Y + i * SelectionAssetSizeLabelHeight;
				label.Bounds.Width = SelectionAssetSizeLabelWidth;
				label.Bounds.Height = SelectionAssetSizeLabelHeight;
				label.IsVisible = () => true;
				assetSizeLabels.Add(label);
				selectedPreviewPanel.AddChild(label);
			}
		}

		IReadOnlyList<CVec> GetSelectedAssetCellSizes()
		{
			if (TryGetPlacementTemplate(out _, out var mapTemplate))
				return [TileTemplateCellSize(mapTemplate)];

			if (editor.CurrentBrush is EditorTileBrush tileBrush)
			{
				var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
				return tileBrush.Templates
					.Select(t => TileTemplateCellSize(terrainInfo.Templates[t]))
					.ToArray();
			}

			if (editor.CurrentBrush is EditorActorBrush actorBrush)
				return actorBrush.Actors.Select(ActorCellSize).ToArray();

			if (editor.CurrentBrush is EditorResourceBrush)
				return [new CVec(1, 1)];

			if (ShowSelectedMapActorPreview())
				return [ActorCellSize(editor.DefaultBrush.Selection.Actor.Info)];

			return [];
		}

		int ComputeMaxTemplateCellSpan()
		{
			if (map.Rules.TerrainInfo is not ITemplatedTerrainInfo terrainInfo)
				return 1;

			var max = 1;
			foreach (var template in terrainInfo.Templates.Values)
			{
				if (template.PickAny)
					continue;

				max = Math.Max(max, Math.Max(template.Size.X, template.Size.Y));
			}

			return max;
		}

		static CVec TileTemplateCellSize(TerrainTemplateInfo template)
		{
			if (template.PickAny)
				return new CVec(1, 1);

			return new CVec(template.Size.X, template.Size.Y);
		}

		static CVec ActorCellSize(ActorInfo actorInfo)
		{
			var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (buildingInfo != null)
				return buildingInfo.Dimensions;

			var occupySpaceInfo = actorInfo.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (occupySpaceInfo == null)
				return new CVec(1, 1);

			var cells = occupySpaceInfo.OccupiedCells(actorInfo, default, SubCell.Any).Keys;
			if (!cells.Any())
				return new CVec(1, 1);

			var minX = cells.Min(c => c.X);
			var maxX = cells.Max(c => c.X);
			var minY = cells.Min(c => c.Y);
			var maxY = cells.Max(c => c.Y);
			return new CVec(maxX - minX + 1, maxY - minY + 1);
		}

		readonly struct PreviewCellLayout
		{
			public readonly int GridWidth;
			public readonly int GridHeight;
			public readonly int CellPixelSize;
			public readonly int OriginX;
			public readonly int OriginY;
			public readonly CVec[] ItemCellOrigins;
			public readonly CVec[] ItemCellSizes;

			public PreviewCellLayout(
				int gridWidth,
				int gridHeight,
				int cellPixelSize,
				int originX,
				int originY,
				CVec[] itemCellOrigins,
				CVec[] itemCellSizes)
			{
				GridWidth = gridWidth;
				GridHeight = gridHeight;
				CellPixelSize = cellPixelSize;
				OriginX = originX;
				OriginY = originY;
				ItemCellOrigins = itemCellOrigins;
				ItemCellSizes = itemCellSizes;
			}
		}

		int PreviewGridSpan(CVec itemSize)
		{
			if (ShowSelectedMapActorPreview())
				return Math.Max(1, Math.Max(itemSize.X, itemSize.Y));

			return Math.Max(maxTemplateCellSpan, Math.Max(itemSize.X, itemSize.Y));
		}

		PreviewCellLayout ComputePreviewCellLayout(IReadOnlyList<CVec> itemCellSizes, Rectangle bounds)
		{
			if (itemCellSizes.Count == 1)
			{
				var itemSize = itemCellSizes[0];
				var span = PreviewGridSpan(itemSize);
				var cellPixelSize = Math.Max(1, Math.Min(bounds.Width, bounds.Height) / span);
				var gridPixelSize = span * cellPixelSize;
				var cellOrigin = new CVec((span - itemSize.X) / 2, (span - itemSize.Y) / 2);
				return new PreviewCellLayout(
					span,
					span,
					cellPixelSize,
					bounds.X + (bounds.Width - gridPixelSize) / 2,
					bounds.Y + (bounds.Height - gridPixelSize) / 2,
					[cellOrigin],
					[itemSize]);
			}

			var count = itemCellSizes.Count;
			var columns = MultiPreviewColumns(count);
			var rows = (count + columns - 1) / columns;
			var columnWidths = new int[columns];
			var rowHeights = new int[rows];

			for (var i = 0; i < count; i++)
			{
				columnWidths[i % columns] = Math.Max(columnWidths[i % columns], itemCellSizes[i].X);
				rowHeights[i / columns] = Math.Max(rowHeights[i / columns], itemCellSizes[i].Y);
			}

			var gridWidth = columnWidths.Sum();
			var gridHeight = rowHeights.Sum();
			var multiCellPixelSize = Math.Max(1, Math.Min(bounds.Width / gridWidth, bounds.Height / gridHeight));
			var multiGridPixelWidth = gridWidth * multiCellPixelSize;
			var multiGridPixelHeight = gridHeight * multiCellPixelSize;
			var originX = bounds.X + (bounds.Width - multiGridPixelWidth) / 2;
			var originY = bounds.Y + (bounds.Height - multiGridPixelHeight) / 2;

			var columnStarts = new int[columns];
			var rowStarts = new int[rows];
			for (var c = 1; c < columns; c++)
				columnStarts[c] = columnStarts[c - 1] + columnWidths[c - 1];
			for (var r = 1; r < rows; r++)
				rowStarts[r] = rowStarts[r - 1] + rowHeights[r - 1];

			var itemCellOrigins = new CVec[count];
			for (var i = 0; i < count; i++)
				itemCellOrigins[i] = new CVec(columnStarts[i % columns], rowStarts[i / columns]);

			return new PreviewCellLayout(
				gridWidth,
				gridHeight,
				multiCellPixelSize,
				originX,
				originY,
				itemCellOrigins,
				itemCellSizes.ToArray());
		}

		void UpdatePreviewGrid(PreviewCellLayout layout)
		{
			previewGridWidget.Bounds.X = layout.OriginX;
			previewGridWidget.Bounds.Y = layout.OriginY;
			previewGridWidget.Bounds.Width = layout.GridWidth * layout.CellPixelSize;
			previewGridWidget.Bounds.Height = layout.GridHeight * layout.CellPixelSize;
			previewGridWidget.GridWidth = layout.GridWidth;
			previewGridWidget.GridHeight = layout.GridHeight;
			previewGridWidget.IsVisible = () => true;
		}

		void ApplyPreviewBorders()
		{
			HidePreviewBorders();

			if (!previewBordersEnabled || !HasMultiAssetSelection() || currentPreviewLayout is not PreviewCellLayout layout)
				return;

			var regions = BuildPreviewBorderCellRegions(layout);
			if (regions.Count == 0)
				return;

			previewBorderWidget.OriginX = layout.OriginX;
			previewBorderWidget.OriginY = layout.OriginY;
			previewBorderWidget.CellPixelSize = layout.CellPixelSize;
			previewBorderWidget.CellRegions = regions;
			previewBorderWidget.IsVisible = () => previewBordersEnabled && HasMultiAssetSelection();
		}

		void EnsureBorderWidgetOnTop()
		{
			selectedAssetPreviewBox.RemoveChild(previewBorderWidget);
			selectedAssetPreviewBox.AddChild(previewBorderWidget);
		}

		List<CVec[]> BuildPreviewBorderCellRegions(PreviewCellLayout layout)
		{
			var regions = new List<CVec[]>();

			if (editor.CurrentBrush is EditorTileBrush tileBrush)
			{
				var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
				for (var i = 0; i < tileBrush.Templates.Length; i++)
				{
					var template = terrainInfo.Templates[tileBrush.Templates[i]];
					regions.Add(TemplatePreviewCells(template, layout.ItemCellOrigins[i]));
				}
			}
			else if (editor.CurrentBrush is EditorActorBrush actorBrush)
			{
				for (var i = 0; i < actorBrush.Actors.Length; i++)
					regions.Add(ActorPreviewCells(actorBrush.Actors[i], layout.ItemCellOrigins[i]));
			}

			return regions;
		}

		static CVec[] TemplatePreviewCells(TerrainTemplateInfo template, CVec gridOrigin)
		{
			if (template.PickAny)
				return [gridOrigin];

			var templateWidth = template.Size.X;
			var cells = new List<CVec>();
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				cells.Add(gridOrigin + new CVec(i % templateWidth, i / templateWidth));
			}

			return cells.Count > 0 ? cells.ToArray() : [gridOrigin];
		}

		static CVec[] ActorPreviewCells(ActorInfo actorInfo, CVec gridOrigin)
		{
			var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (buildingInfo != null)
			{
				return buildingInfo.Footprint
					.Where(kv => kv.Value != FootprintCellType.Empty)
					.Select(kv => gridOrigin + kv.Key)
					.ToArray();
			}

			var occupySpaceInfo = actorInfo.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (occupySpaceInfo == null)
				return [gridOrigin];

			var cells = occupySpaceInfo.OccupiedCells(actorInfo, default, SubCell.Any).Keys;
			if (!cells.Any())
				return [gridOrigin];

			var minX = cells.Min(c => c.X);
			var minY = cells.Min(c => c.Y);
			return cells.Select(c => gridOrigin + new CVec(c.X - minX, c.Y - minY)).ToArray();
		}

		static Rectangle ItemPixelBounds(PreviewCellLayout layout, int index)
		{
			var origin = layout.ItemCellOrigins[index];
			var size = layout.ItemCellSizes[index];
			return new Rectangle(
				layout.OriginX + origin.X * layout.CellPixelSize,
				layout.OriginY + origin.Y * layout.CellPixelSize,
				size.X * layout.CellPixelSize,
				size.Y * layout.CellPixelSize);
		}

		Rectangle FullAssetPreviewBounds()
		{
			var bounds = selectedAssetPreviewBox.Bounds;
			var inset = SelectionPanelContentMargin;
			return new Rectangle(
				inset,
				inset,
				Math.Max(1, bounds.Width - 2 * inset),
				Math.Max(1, bounds.Height - 2 * inset));
		}

		void LayoutTerrainPreview(TerrainTemplatePreviewWidget preview, Rectangle pixelBounds)
		{
			var scale = PreviewScale(preview.IdealPreviewSize, pixelBounds.Width, pixelBounds.Height);
			preview.Scale = scale;
			var width = (int)(scale * preview.IdealPreviewSize.X);
			var height = (int)(scale * preview.IdealPreviewSize.Y);
			preview.Bounds.X = pixelBounds.X + (pixelBounds.Width - width) / 2;
			preview.Bounds.Y = pixelBounds.Y + (pixelBounds.Height - height) / 2;
			preview.Bounds.Width = width;
			preview.Bounds.Height = height;
		}

		void LayoutActorPreview(ActorPreviewWidget preview, Rectangle pixelBounds)
		{
			var scale = PreviewScale(preview.IdealPreviewSize, pixelBounds.Width, pixelBounds.Height);
			preview.Scale = scale;
			var width = (int)(scale * preview.IdealPreviewSize.X);
			var height = (int)(scale * preview.IdealPreviewSize.Y);
			preview.Bounds.X = pixelBounds.X + (pixelBounds.Width - width) / 2;
			preview.Bounds.Y = pixelBounds.Y + (pixelBounds.Height - height) / 2;
			preview.Bounds.Width = width;
			preview.Bounds.Height = height;
			preview.PrepareRenderables();
		}

		void LayoutResourcePreview(ResourcePreviewWidget preview, Rectangle pixelBounds)
		{
			var idealSize = new int2(preview.IdealPreviewSize.Width, preview.IdealPreviewSize.Height);
			preview.Scale = PreviewScale(idealSize, pixelBounds.Width, pixelBounds.Height);
			preview.Bounds.X = pixelBounds.X;
			preview.Bounds.Y = pixelBounds.Y;
			preview.Bounds.Width = pixelBounds.Width;
			preview.Bounds.Height = pixelBounds.Height;
		}

		Rectangle AssetPreviewContentBounds()
		{
			var bounds = selectedAssetPreviewBox.Bounds;
			var inset = (int)Math.Round(bounds.Width * (1 - SelectionPreviewImageScale) / 2);
			var contentWidth = bounds.Width - 2 * inset;
			var contentHeight = bounds.Height - 2 * inset;
			return new Rectangle(inset, inset, contentWidth, contentHeight);
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

		static float PreviewScale(int2 idealSize, int maxWidth, int maxHeight)
		{
			if (idealSize.X <= 0 || idealSize.Y <= 0)
				return 1f;

			return Math.Min(maxWidth / (float)idealSize.X, maxHeight / (float)idealSize.Y);
		}

		void HandleAssetSelectionChanged()
		{
			UpdateSelectionDetailPanel();
			UpdatePanelTitle();
		}

		void HandleSelectionChanged()
		{
			ResetSimilarBrowserFilterIfActive();
			placementPreviewDisplayMode = TilePlacementPreviewDisplayMode.Current;
			similarPreviewIndex = 0;
			InvalidateAreaPreview();
			UpdateAreaPreview();
			UpdateSelectedPreview();
			UpdateSelectionDetailPanel();
			UpdatePanelTitle();
			AutoLocateSelectionInAssetBrowser();

			var selection = editor.DefaultBrush.Selection;
			if (selection.Actor != null || TryGetSelectedTemplate(out _, out _)
				|| editor.CurrentBrush is EditorActorBrush or EditorTileBrush)
				return;

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

			selectedPreviewLabel.GetText = () => areaSelectionLabel;
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

		void UpdatePanelTitle()
		{
			var selection = editor.DefaultBrush.Selection;
			if (selection.Actor != null)
			{
				selectedPreviewLabel.GetText = () => FluentProvider.GetMessage(selection.Actor.DescriptiveName);
				return;
			}

			if (editor.CurrentBrush is EditorActorBrush actorBrush && actorBrush.Actors.Length == 1)
			{
				selectedPreviewLabel.GetText = () => GetActorDisplayName(actorBrush.Actors[0]);
				return;
			}

			if (TryGetSelectedTemplate(out _, out var template))
			{
				selectedPreviewLabel.GetText = () => FormatTileDisplayName(template);
				return;
			}

			selectedPreviewLabel.GetText = () => "";
		}

		void UpdateSelectionDetailPanel()
		{
			if (TryGetSelectedTemplate(out var terrainInfo, out var template))
				tileDetailInfo.GetText = () => FormatTileDetailText(terrainInfo, template);
			else
				tileDetailInfo.GetText = () => "";

			if (editor.CurrentBrush is EditorActorBrush actorBrush && actorBrush.Actors.Length == 1)
				actorBrushDetailInfo.GetText = () => FormatActorBrushDetailText(actorBrush);
			else
				actorBrushDetailInfo.GetText = () => "";
		}

		static string GetActorDisplayName(ActorInfo actor)
		{
			var tooltip = actor.TraitInfos<EditorOnlyTooltipInfo>().FirstOrDefault(ti => ti.EnabledByDefault) as TooltipInfoBase
				?? actor.TraitInfos<TooltipInfo>().FirstOrDefault(ti => ti.EnabledByDefault);

			return tooltip != null ? FluentProvider.GetMessage(tooltip.Name) : actor.Name;
		}

		static string FormatActorBrushDetailText(EditorActorBrush actorBrush)
		{
			var actor = actorBrush.Actors[0];
			var reference = actorBrush.Preview.Export();
			var health = reference.GetOrDefault<HealthInit>()?.Value ?? 100;
			var facing = reference.GetOrDefault<FacingInit>()?.Value.Angle ?? 384;
			var lines = new List<string>
			{
				GetActorDisplayName(actor),
				$"Type: {actor.Name}",
				$"Owner: {actorBrush.Owner.Name}",
				$"Health: {health.ToString(NumberFormatInfo.InvariantInfo)}",
				$"Facing: {facing.ToString(NumberFormatInfo.InvariantInfo)}",
			};

			return string.Join("\n", lines);
		}

		static string FormatTileDisplayName(TerrainTemplateInfo template)
		{
			if (template is DefaultTerrainTemplateInfo defaultTemplate && defaultTemplate.Images.Length > 0)
				return defaultTemplate.Images[0];

			return template.Id.ToString(NumberFormatInfo.InvariantInfo);
		}

		static string FormatTileDetailText(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
		{
			var id = template.Id.ToString(NumberFormatInfo.InvariantInfo);
			var displayName = FormatTileDisplayName(template);
			var terrainTypes = TileTerrainTypes(terrainInfo, template);
			var lines = new List<string>
			{
				displayName,
				$"Template ID: {id}",
			};

			if (template.Categories.Length > 0)
				lines.Add($"Category: {string.Join(", ", template.Categories)}");

			if (terrainTypes.Length > 0)
				lines.Add($"Terrain: {string.Join(", ", terrainTypes)}");

			return string.Join("\n", lines);
		}

		static string[] TileTerrainTypes(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
		{
			var terrainTypes = new HashSet<string>();
			for (var i = 0; i < template.TilesCount; i++)
			{
				if (!template.Contains(i) || template[i] == null || template[i].TerrainType == byte.MaxValue)
					continue;

				terrainTypes.Add(terrainInfo.TerrainTypes[template[i].TerrainType].Type);
			}

			return terrainTypes.Order().ToArray();
		}

		bool ShouldAutoLocateSelectionInAssetBrowser()
		{
			var selection = editor.DefaultBrush.Selection;

			// Map tile / area selection must not narrow asset-browser category filters.
			if (selection.Area.HasValue || selection.HasTemplatePlacementContext)
				return false;

			if (editor.CurrentBrush is EditorTileBrush tileBrush && tileBrush.Templates.Length != 1)
				return false;

			if (editor.CurrentBrush is EditorActorBrush actorBrush && actorBrush.Actors.Length != 1)
				return false;

			return ShowSelectedTileDetail() || ShowSelectedActorBrushDetail();
		}

		void AutoLocateSelectionInAssetBrowser()
		{
			if (!ShouldAutoLocateSelectionInAssetBrowser() || !TryCreateLocateRequest(out var request))
				return;

			editor.RequestLocateAsset(request);
		}

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

			if (selection.HasTemplatePlacementContext && selection.TemplatePlacementType is ushort placementTemplateId)
			{
				request = EditorLocateAssetRequest.ForTile(placementTemplateId);
				return true;
			}

			if (editor.CurrentBrush is EditorTileBrush tileBrush && tileBrush.Templates.Length == 1)
			{
				request = EditorLocateAssetRequest.ForTile(tileBrush.Templates[0]);
				return true;
			}

			if (editor.CurrentBrush is EditorActorBrush actorBrush && actorBrush.Actors.Length == 1)
			{
				request = EditorLocateAssetRequest.ForActor(actorBrush.Actors[0]);
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
