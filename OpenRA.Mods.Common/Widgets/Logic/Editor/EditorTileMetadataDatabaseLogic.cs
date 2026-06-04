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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class EditorTileMetadataDatabaseLogic : ChromeLogic
	{
		[FluentReference]
		const string Title = "label-editor-tile-metadata-database";

		[FluentReference]
		const string TemplatesTab = "label-editor-metadata-templates";

		[FluentReference]
		const string ActorsTab = "label-editor-metadata-actors";

		[FluentReference]
		const string PickColumnHeader = "label-editor-metadata-pick";

		[FluentReference]
		const string CurrentlyTraining = "label-editor-currently-training";

		[FluentReference]
		const string PickPrimaryTile = "label-editor-metadata-pick-primary";

		const int CheckboxColumnWidth = 36;
		const int PreviewWidth = 112;
		const int RowHeight = 96;
		const int CellWidth = 88;
		const int HeaderHeight = 26;
		static readonly Color TrainedEntryColor = Color.FromArgb(0xFF4CFF00);
		static readonly Color TrainedHeaderColor = Color.FromArgb(0xFF4CFF00);
		static readonly Color TrainedCellBorderColor = Color.FromArgb(0xFF228B22);

		static bool IsTrainingHighlightColumn(string column) => column switch
		{
			"Orientation_island" or "Orientation_ring" or "Related_corners_island" or "Related_corners_ring" or
			"Opposites_island" or "Opposites_ring" or "Similar" or "OppositesSlot" => true,
			_ => false
		};

		static string SlotArrowGlyph(int slot) => slot switch
		{
			0 => "↖",
			1 => "↑",
			2 => "↗",
			3 => "←",
			4 => "·",
			5 => "→",
			6 => "↙",
			7 => "↓",
			8 => "↘",
			EditorTileMetadata.HorizontalSlot => "↔",
			EditorTileMetadata.VerticalSlot => "↕",
			_ => "·"
		};

		static string SlotDisplay(int slot) => slot switch
		{
			0 => "↖ TopLeft",
			1 => "↑ Top",
			2 => "↗ TopRight",
			3 => "← Left",
			4 => "· Center",
			5 => "→ Right",
			6 => "↙ BottomLeft",
			7 => "↓ Bottom",
			8 => "↘ BottomRight",
			EditorTileMetadata.HorizontalSlot => "↔ Horizontal",
			EditorTileMetadata.VerticalSlot => "↕ Vertical",
			_ => "· Center"
		};

		static string FormatTrainingCellText(string column, string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			if (column is not ("OppositesSlot" or "Orientation_island" or "Orientation_ring"))
				return text;

			var slot = EditorTileMetadata.TryParseOrientationSlot(text);
			return slot.HasValue ? SlotDisplay(slot.Value) : text;
		}

		static string ColumnDisplayName(string column) => column switch
		{
			"OriginalFilename" => "Filename",
			"TemplateId" => "TmplId",
			"TerrainTypes" => "Terrain",
			"EdgeSignature" => "Edges",
			"Orientation_island" => "Ori.Isl",
			"Orientation_ring" => "Ori.Ring",
			"Related_corners_island" => "Crnr.Isl",
			"Related_corners_ring" => "Crnr.Ring",
			"OppositesGroup" => "OppGrp",
			"Opposites_island" => "OppIsl",
			"Opposites_ring" => "OppRing",
			"OppositesSlot" => "Slot",
			"TrainingStatus" => "Status",
			"DeepDescription" => "Deep",
			"Description" => "Desc",
			"OriginalActorName" => "Actor",
			"RuleFile" => "Rule",
			"DisplayNameKey" => "NameKey",
			"SimilarGroup" => "SimGrp",
			"BuildQueue" => "Queue",
			_ => column
		};

		static EditorTileMetadataDatabaseLogic active;

		readonly Widget parentRoot;
		readonly Widget panel;
		readonly EditorTileMetadataTraining training;
		readonly EditorTileMetadataFile metadataFile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly Ruleset rules;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ScrollPanelWidget rowPanel;
		readonly Widget headerPanel;
		ContainerWidget headerContent;
		readonly ScrollItemWidget rowTemplate;
		readonly ButtonWidget templatesTab;
		readonly ButtonWidget actorsTab;
		readonly Widget saveBar;
		readonly Widget trainingStatusPanel;
		readonly LabelWidget trainingModeLabel;
		readonly LabelWidget trainingNameLabel;
		readonly Widget trainingPreviewBox;
		Widget trainingPreviewWidget;
		bool showActors;
		bool isClosing;
		string focusedTemplateRowKey;
		ushort? focusedTemplateId;

		[ObjectCreator.UseCtor]
		public EditorTileMetadataDatabaseLogic(Widget widget, World world, WorldRenderer worldRenderer, EditorTileMetadataTraining training)
		{
			parentRoot = widget.Parent;
			this.training = training;
			metadataFile = training.MetadataFile;
			this.world = world;
			terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			rules = world.Map.Rules;
			this.worldRenderer = worldRenderer;
			panel = widget;
			active = this;

			panel.Get<LabelWidget>("DATABASE_TITLE").GetText = () => FluentProvider.GetMessage(Title);

			var closeButton = panel.Get<ButtonWidget>("DATABASE_CLOSE_BUTTON");
			closeButton.OnClick = Close;

			templatesTab = panel.Get<ButtonWidget>("DATABASE_TEMPLATES_TAB");
			actorsTab = panel.Get<ButtonWidget>("DATABASE_ACTORS_TAB");
			templatesTab.GetText = () => FluentProvider.GetMessage(TemplatesTab);
			actorsTab.GetText = () => FluentProvider.GetMessage(ActorsTab);
			templatesTab.IsHighlighted = () => !showActors;
			actorsTab.IsHighlighted = () => showActors;
			templatesTab.OnClick = () => { showActors = false; RebuildRows(); };
			actorsTab.OnClick = () => { showActors = true; RebuildRows(); };

			SetupTrainButton(panel, "DATABASE_TRAIN_OPPOSITE_ISLAND_BUTTON", EditorMetadataTrainingKind.OppositeIsland);
			SetupTrainButton(panel, "DATABASE_TRAIN_OPPOSITE_RING_BUTTON", EditorMetadataTrainingKind.OppositeRing);
			SetupTrainButton(panel, "DATABASE_TRAIN_SIMILAR_BUTTON", EditorMetadataTrainingKind.Similar);
			SetupTrainButton(panel, "DATABASE_TRAIN_ORIENTATION_ISLAND_BUTTON", EditorMetadataTrainingKind.OrientationIsland);
			SetupTrainButton(panel, "DATABASE_TRAIN_ORIENTATION_RING_BUTTON", EditorMetadataTrainingKind.OrientationRing);
			SetupTrainButton(panel, "DATABASE_TRAIN_RELATED_CORNERS_ISLAND_BUTTON", EditorMetadataTrainingKind.RelatedCornersIsland);
			SetupTrainButton(panel, "DATABASE_TRAIN_RELATED_CORNERS_RING_BUTTON", EditorMetadataTrainingKind.RelatedCornersRing);

			saveBar = panel.Get("DATABASE_TRAINING_SAVE_BAR");
			saveBar.IsVisible = () => training.ShowSecondarySelection || training.ShowOrientationSave;
			saveBar.Get<ButtonWidget>("DATABASE_TRAINING_SAVE_BUTTON").OnClick = () =>
			{
				if (training.ShowOrientationSave)
					training.SaveOrientation();
				else
					training.Save();
			};
			saveBar.Get<ButtonWidget>("DATABASE_TRAINING_CANCEL_BUTTON").OnClick = () =>
			{
				training.Cancel();
				RebuildRows();
			};

			headerPanel = panel.Get("DATABASE_HEADER_PANEL");
			rowPanel = panel.Get<ScrollPanelWidget>("DATABASE_ROW_PANEL");
			rowTemplate = rowPanel.Get<ScrollItemWidget>("DATABASE_ROW_TEMPLATE");
			rowTemplate.Visible = false;
			trainingStatusPanel = panel.Get("DATABASE_TRAINING_STATUS");
			trainingStatusPanel.IsVisible = () => training.IsActive;
			trainingModeLabel = panel.Get<LabelWidget>("DATABASE_TRAINING_MODE");
			trainingNameLabel = panel.Get<LabelWidget>("DATABASE_TRAINING_NAME");
			trainingPreviewBox = panel.Get("DATABASE_TRAINING_PREVIEW_BOX");

			metadataFile.Changed += RebuildRows;
			training.Changed += OnTrainingChanged;
			RebuildRows();
			UpdateTrainingStatus();
		}

		void OnTrainingChanged()
		{
			if (isClosing)
				return;

			EnsureOrientationPanelRaised();
			UpdateTrainingStatus();
			RebuildRows();
		}

		void EnsureOrientationPanelRaised()
		{
			if (!training.ShowOrientationTraining)
				return;

			var orient = parentRoot.GetOrNull("EDITOR_ORIENTATION_TRAINING_PANEL");
			if (orient == null)
				return;

			if (orient.Parent != panel)
			{
				orient.Parent?.RemoveChild(orient);
				panel.AddChild(orient);
			}
			else
			{
				panel.RemoveChild(orient);
				panel.AddChild(orient);
			}
		}

		void Close()
		{
			if (isClosing || panel.Parent == null)
				return;

			isClosing = true;

			if (active == this)
				active = null;

			metadataFile.Changed -= RebuildRows;
			training.Changed -= OnTrainingChanged;

			var orientation = panel.GetOrNull("EDITOR_ORIENTATION_TRAINING_PANEL");
			if (orientation != null && parentRoot != null)
			{
				panel.RemoveChild(orientation);
				parentRoot.AddChild(orientation);
			}

			training.Cancel();

			if (panel.Parent != null)
				panel.Parent.RemoveChild(panel);

			isClosing = false;
		}

		public static void CloseIfOpen()
		{
			if (active == null)
				return;

			active.Close();
		}

		void UpdateTrainingStatus()
		{
			if (!training.IsActive)
			{
				trainingPreviewBox.RemoveChildren();
				trainingPreviewWidget = null;
				return;
			}

			trainingModeLabel.GetText = () => training.ModeDisplayName;

			var primaryName = training.GetPrimaryDisplayName();
			trainingNameLabel.GetText = () => primaryName ?? FluentProvider.GetMessage(PickPrimaryTile);

			trainingPreviewBox.RemoveChildren();
			trainingPreviewWidget = null;

			if (training.PrimaryTemplateCount == 1 && training.PrimaryTemplateId != null &&
				terrainInfo?.Templates.TryGetValue(training.PrimaryTemplateId.Value, out var template) == true)
			{
				var preview = new TerrainTemplatePreviewWidget(Game.ModData, worldRenderer, world)
				{
					Bounds = new WidgetBounds(0, 0, 72, 72)
				};
				preview.SetTemplate(template);
				var scale = Math.Min(1f, 72f / preview.IdealPreviewSize.X);
				preview.Scale = Math.Min(scale, 72f / preview.IdealPreviewSize.Y);
				trainingPreviewWidget = preview;
				trainingPreviewBox.AddChild(preview);
				AddTrainingPreviewSelection();
			}
			else if (training.PrimaryActorCount == 1 && training.PrimaryActorName != null &&
				rules.Actors.TryGetValue(training.PrimaryActorName.ToLowerInvariant(), out var actor))
			{
				try
				{
					var preview = new ActorPreviewWidget(Game.ModData, worldRenderer)
					{
						Bounds = new WidgetBounds(0, 0, 72, 72)
					};
					preview.SetPreview(actor, CreateActorPreviewInit(actor));
					trainingPreviewWidget = preview;
					trainingPreviewBox.AddChild(preview);
					AddTrainingPreviewSelection();
				}
				catch
				{
					// Actor preview not available for this tileset.
				}
			}
		}

		public static bool IsOpen => active != null;

		public static void Open(Widget parent, World world, WorldRenderer worldRenderer, ModData modData, EditorTileMetadataTraining training)
		{
			if (!training.MetadataFile.IsAvailable)
				return;

			if (active != null)
			{
				active.panel.IsVisible = () => true;
				return;
			}

			var panel = Ui.LoadWidget("EDITOR_TILE_METADATA_DATABASE", parent, new WidgetArgs
			{
				{ "world", world },
				{ "worldRenderer", worldRenderer },
				{ "training", training }
			});
			panel.IsVisible = () => true;
			parent.AddChild(panel);
			if (active != null)
				active.EnsureOrientationPanelRaised();
		}

		public static void Toggle(Widget parent, World world, WorldRenderer worldRenderer, ModData modData, EditorTileMetadataTraining training)
		{
			if (active != null)
				active.Close();
			else
				Open(parent, world, worldRenderer, modData, training);
		}

		protected override void Dispose(bool disposing)
		{
			metadataFile.Changed -= RebuildRows;
			training.Changed -= OnTrainingChanged;
			if (active == this)
				active = null;

			base.Dispose(disposing);
		}

		void SetupTrainButton(Widget root, string id, EditorMetadataTrainingKind mode)
		{
			var button = root.GetOrNull<ButtonWidget>(id);
			if (button == null)
				return;

			button.IsHighlighted = () => training.Mode == mode;
			button.OnClick = () =>
			{
				if (training.Mode == mode)
					training.Cancel();
				else
				{
					training.Start(mode);
					if (focusedTemplateId.HasValue)
						training.ApplyFocusedTemplateAsPrimary(focusedTemplateId.Value);
				}
			};
		}

		void RebuildRows()
		{
			var scrollOffset = rowPanel.CurrentListOffset;
			rowPanel.RemoveChildren();

			var columns = DisplayColumns(showActors ? metadataFile.ActorColumns : metadataFile.TemplateColumns);
			if (columns.Length == 0)
				return;

			BuildHeader(columns);

			if (showActors)
			{
				foreach (var row in metadataFile.ActorRows().OrderBy(r => r.ActorName, StringComparer.OrdinalIgnoreCase))
				{
					try
					{
						rowPanel.AddChild(CreateActorRow(rowTemplate, row, columns));
					}
					catch (Exception ex)
					{
						Log.Write("debug", $"Metadata database ignoring actor {row.ActorName}: {ex.Message}");
					}
				}
			}
			else if (terrainInfo != null)
			{
				foreach (var row in metadataFile.TemplateRows(terrainInfo.Id).OrderBy(r => r.OriginalFilename, StringComparer.OrdinalIgnoreCase))
					rowPanel.AddChild(CreateTemplateRow(rowTemplate, row, columns));
			}

			EnsureScrollbarCapture();
			rowPanel.ApplyListOffset(scrollOffset);
		}

		void EnsureScrollbarCapture()
		{
			foreach (var child in rowPanel.Children)
			{
				if (child is MetadataScrollbarCaptureWidget)
					return;
			}

			rowPanel.AddChild(new MetadataScrollbarCaptureWidget(rowPanel));
		}

		int RowContentWidth(int columnCount) =>
			(training.IsActive ? CheckboxColumnWidth : 0) + PreviewWidth + columnCount * CellWidth;

		static string[] DisplayColumns(string[] columns) =>
			columns.Where(c => c != "OppositesSlot").ToArray();

		void BuildHeader(string[] columns)
		{
			headerPanel.RemoveChildren();
			var contentWidth = RowContentWidth(columns.Length);
			headerContent = new ContainerWidget
			{
				Bounds = new WidgetBounds(0, 0, contentWidth, HeaderHeight)
			};

			var x = 0;
			if (training.IsActive)
			{
				AddHeaderCell(x, CheckboxColumnWidth, FluentProvider.GetMessage(PickColumnHeader));
				x += CheckboxColumnWidth;
			}

			AddHeaderCell(x, PreviewWidth, "Preview");
			x += PreviewWidth;

			foreach (var column in columns)
			{
				AddHeaderCell(x, CellWidth, ColumnDisplayName(column), column);
				x += CellWidth;
			}

			headerPanel.AddChild(headerContent);
		}

		void AddHeaderCell(int x, int width, string displayText, string columnKey = null)
		{
			var label = new LabelWidget(Game.ModData)
			{
				Bounds = new WidgetBounds(x, 0, width, HeaderHeight),
				Align = TextAlign.Left,
				VAlign = TextVAlign.Top,
				Font = "TinyBold",
				Text = displayText
			};
			label.GetText = () => displayText;
			if (columnKey != null && IsTrainingHighlightColumn(columnKey))
				label.GetColor = () => TrainedHeaderColor;

			headerContent.AddChild(label);
		}

		EditorPreviewSelectionKind TemplatePreviewSelection(ushort templateId, string rowKey)
		{
			if (training.IsActive && training.IsPrimaryTemplateSelected(templateId))
				return EditorPreviewSelectionKind.Primary;

			if (training.ShowSecondarySelection && training.IsSecondaryTemplateSelected(templateId))
				return EditorPreviewSelectionKind.Secondary;

			return EditorPreviewSelectionKind.None;
		}

		Widget CreateTemplateRow(ScrollItemWidget template, MetadataTemplateRow row, string[] columns)
		{
			var templateId = row.TemplateId;
			var rowWidth = RowContentWidth(columns.Length);
			var item = ScrollItemWidget.Setup(row.Key, template, () => false, () => HandleTemplateRowClick(templateId, row.Key), () => { });
			item.Bounds.Height = RowHeight;
			item.Bounds.Width = rowWidth;
			item.IgnoreChildMouseOver = false;
			item.ShowLocateOutline = () => focusedTemplateRowKey == row.Key;

			var x = 0;
			if (training.IsActive)
				x += AddTrainingCheckbox(item, x, () => training.ShowTrainingCheckboxes, () => training.IsPrimaryTemplateSelected(templateId), () => HandleTemplateRowClick(templateId, row.Key));

			if (terrainInfo.Templates.TryGetValue(templateId, out var templateInfo))
			{
				var previewBounds = new WidgetBounds(x + 4, 4, PreviewWidth - 8, RowHeight - 8);
				var preview = new TerrainTemplatePreviewWidget(Game.ModData, worldRenderer, world)
				{
					Bounds = previewBounds
				};
				preview.SetTemplate(templateInfo);
				var scale = Math.Min(1f, (PreviewWidth - 8) / (float)preview.IdealPreviewSize.X);
				preview.Scale = Math.Min(scale, (RowHeight - 8) / (float)preview.IdealPreviewSize.Y);
				item.AddChild(preview);
				if (training.IsActive)
					item.AddChild(CreatePreviewClickTarget(previewBounds, () => HandleTemplateRowClick(templateId, row.Key)));
				item.AddChild(new EditorPreviewSelectionWidget
				{
					Bounds = previewBounds,
					GetSelection = () => TemplatePreviewSelection(templateId, row.Key)
				});
			}

			x += PreviewWidth;
			foreach (var column in columns)
			{
				var raw = metadataFile.ReadField(row.Data, column);
				item.AddChild(CreateTemplateCell(x, CellWidth, column, FormatTrainingCellText(column, raw), row.Data, templateId));
				x += CellWidth;
			}

			item.AddChild(CreatePreviewClickTarget(new WidgetBounds(0, 0, rowWidth, RowHeight), () => HandleTemplateRowClick(templateId, row.Key)));
			return item;
		}

		EditorPreviewSelectionKind ActorPreviewSelection(string actorName)
		{
			if (training.IsPrimaryActorSelected(actorName))
				return EditorPreviewSelectionKind.Primary;

			if (training.ShowSecondarySelection && training.IsSecondaryActorSelected(actorName))
				return EditorPreviewSelectionKind.Secondary;

			return EditorPreviewSelectionKind.None;
		}

		Widget CreateActorRow(ScrollItemWidget template, MetadataActorRow row, string[] columns)
		{
			var actorName = row.ActorName;
			var item = ScrollItemWidget.Setup(row.Key, template, () => false, () => { }, () => { });
			item.Bounds.Height = RowHeight;
			item.Bounds.Width = RowContentWidth(columns.Length);
			item.IgnoreChildMouseOver = false;

			var x = 0;
			if (training.IsActive)
				x += AddTrainingCheckbox(item, x, () => training.ShowTrainingCheckboxes, () => training.IsPrimaryActorSelected(actorName), () => HandleActorRowClick(actorName));

			if (rules.Actors.TryGetValue(actorName.ToLowerInvariant(), out var actor))
			{
				var previewBounds = new WidgetBounds(x + 4, 4, PreviewWidth - 8, RowHeight - 8);
				var preview = new ActorPreviewWidget(Game.ModData, worldRenderer)
				{
					Bounds = previewBounds
				};
				preview.SetPreview(actor, CreateActorPreviewInit(actor));
				var scale = Math.Min(1f, (PreviewWidth - 8) / (float)Math.Max(1, preview.IdealPreviewSize.X));
				preview.Scale = Math.Min(scale, (RowHeight - 8) / (float)Math.Max(1, preview.IdealPreviewSize.Y));
				item.AddChild(preview);
				if (training.IsActive)
					item.AddChild(CreatePreviewClickTarget(previewBounds, () => HandleActorRowClick(actorName)));
				item.AddChild(new EditorPreviewSelectionWidget
				{
					Bounds = previewBounds,
					GetSelection = () => ActorPreviewSelection(actorName)
				});
			}

			x += PreviewWidth;
			foreach (var column in columns)
			{
				var raw = metadataFile.ReadField(row.Data, column);
				item.AddChild(CreateCell(x, CellWidth, column, FormatTrainingCellText(column, raw), row.Data));
				x += CellWidth;
			}

			return item;
		}

		void AddTrainingPreviewSelection()
		{
			trainingPreviewBox.AddChild(new EditorPreviewSelectionWidget
			{
				Bounds = new WidgetBounds(0, 0, 72, 72),
				GetSelection = () => EditorPreviewSelectionKind.Primary
			});
		}

		ButtonWidget CreatePreviewClickTarget(WidgetBounds bounds, Action onClick)
		{
			var button = new ButtonWidget(Game.ModData)
			{
				Bounds = bounds,
				Background = "scrollitem-nohover",
				Text = ""
			};
			button.OnClick = onClick;
			return button;
		}

		int AddTrainingCheckbox(Widget item, int x, Func<bool> isVisible, Func<bool> isChecked, Action onToggle)
		{
			var checkbox = new CheckboxWidget(Game.ModData)
			{
				Bounds = new WidgetBounds(x + 8, (RowHeight - 20) / 2, 20, 20)
			};
			checkbox.IsVisible = isVisible;
			checkbox.IsChecked = isChecked;
			checkbox.OnClick = onToggle;
			item.AddChild(checkbox);
			return CheckboxColumnWidth;
		}

		void SetFocusedTemplateRow(string rowKey, ushort templateId)
		{
			focusedTemplateRowKey = rowKey;
			focusedTemplateId = templateId;
		}

		void HandleTemplateRowClick(ushort templateId, string rowKey)
		{
			SetFocusedTemplateRow(rowKey, templateId);

			if (!training.IsActive)
				return;

			if (training.ShowOrientationTraining)
			{
				training.TogglePrimaryTemplate(templateId);
				return;
			}

			if (training.IsPrimaryTemplateSelected(templateId))
			{
				training.TogglePrimaryTemplate(templateId);
				return;
			}

			if (training.ShowSecondarySelection)
			{
				training.ToggleSecondaryTemplate(templateId);
				return;
			}

			training.TogglePrimaryTemplate(templateId);
		}

		void HandleActorRowClick(string actorName)
		{
			if (!training.IsActive)
				return;

			if (training.IsPrimaryActorSelected(actorName))
			{
				training.TogglePrimaryActor(actorName);
				return;
			}

			if (training.ShowSecondarySelection)
			{
				training.ToggleSecondaryActor(actorName);
				return;
			}

			training.TogglePrimaryActor(actorName);
		}

		TypeDictionary CreateActorPreviewInit(ActorInfo actor)
		{
			var td = new TypeDictionary();
			var editorLayer = world.WorldActor.TraitOrDefault<EditorActorLayer>();
			if (editorLayer != null)
			{
				var owner = editorLayer.Players.Players.Values.First();
				td.Add(new OwnerInit(owner.Name));
				td.Add(new FactionInit(owner.Faction));
			}

			foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
			{
				foreach (var init in api.ActorPreviewInits(actor, ActorPreviewType.MapEditorSidebar))
					td.Add(init);
			}

			return td;
		}

		Widget CreateTemplateCell(int x, int width, string column, string text, IReadOnlyDictionary<string, MiniYaml> data, ushort templateId)
		{
			if (ShouldShowOrientationGrid(column, templateId))
				return CreateOrientationGridCell(x, width, column, data, templateId);

			var cornersField = column switch
			{
				"Opposites_ring" => "Related_corners_ring",
				"Opposites_island" => "Related_corners_island",
				_ => null
			};
			return CreateTextCell(x, width, column, text, data, cornersField);
		}

		bool ShouldShowOrientationGrid(string column, ushort templateId)
		{
			if (!training.ShowOrientationTraining || !training.IsPrimaryTemplateSelected(templateId))
				return false;

			return training.Mode == EditorMetadataTrainingKind.OrientationIsland
				? column == "Orientation_island"
				: training.Mode == EditorMetadataTrainingKind.OrientationRing && column == "Orientation_ring";
		}

		Widget CreateOrientationGridCell(int x, int width, string column, IReadOnlyDictionary<string, MiniYaml> data, ushort templateId)
		{
			var cellBounds = new WidgetBounds(x + 2, 2, width - 4, RowHeight - 4);
			var container = new EditorMetadataCellWidget { Bounds = cellBounds };
			var ringCenter = column == "Orientation_ring" || training.Mode == EditorMetadataTrainingKind.OrientationRing;
			var grid = new EditorOrientationGridWidget
			{
				Bounds = new WidgetBounds(0, 0, cellBounds.Width, cellBounds.Height),
				RingCenterSlots = ringCenter,
				GetSelectedSlot = () => training.PendingOrientationSlot,
				OnSelectSlot = training.SelectOrientationSlot
			};
			container.AddChild(TrainedCellBackground(cellBounds, metadataFile.IsColumnTrained(data, column)));
			container.AddChild(grid);
			return container;
		}

		Widget CreateTextCell(int x, int width, string column, string text, IReadOnlyDictionary<string, MiniYaml> data, string cornersField)
		{
			var cellBounds = new WidgetBounds(x + 2, 2, width - 4, RowHeight - 4);
			var container = new EditorMetadataCellWidget { Bounds = cellBounds };
			var visibleText = FormatCellText(text, cellBounds.Width - 4, cellBounds.Height - 4);

			var trained = IsTrainingHighlightColumn(column) &&
				!string.IsNullOrWhiteSpace(visibleText) &&
				metadataFile.IsColumnTrained(data, column);
			container.AddChild(TrainedCellBackground(cellBounds, trained));

			var showCornersOverlay = cornersField != null;
			var labelHeight = showCornersOverlay ? Math.Max(20, cellBounds.Height - 36) : cellBounds.Height - 4;
			var label = new EditorMetadataCellLabelWidget(Game.ModData)
			{
				Bounds = new WidgetBounds(2, 2, cellBounds.Width - 4, labelHeight),
				Align = TextAlign.Left,
				VAlign = TextVAlign.Top,
				Font = "Tiny"
			};
			label.GetText = () => visibleText;

			if (IsTrainingHighlightColumn(column))
			{
				label.GetColor = () => string.IsNullOrWhiteSpace(visibleText)
					? label.TextColor
					: TrainedEntryColor;
			}

			container.AddChild(label);

			if (showCornersOverlay)
			{
				var orientationField = cornersField == "Related_corners_ring" ? "Orientation_ring" : "Orientation_island";
				container.AddChild(new EditorRelatedCornersOverlayWidget
				{
					Bounds = new WidgetBounds(cellBounds.Width - 38, cellBounds.Height - 38, 36, 36),
					GetCornerSlots = () => ResolveRelatedCornerSlots(data, cornersField, orientationField)
				});
			}

			return container;
		}

		static EditorTrainedCellWidget TrainedCellBackground(WidgetBounds cellBounds, bool trained) => new()
		{
			Bounds = new WidgetBounds(0, 0, cellBounds.Width, cellBounds.Height),
			BorderColor = TrainedCellBorderColor,
			BorderWidth = 3,
			IsTrained = () => trained,
		};

		Widget CreateCell(int x, int width, string column, string text, IReadOnlyDictionary<string, MiniYaml> data) =>
			CreateTextCell(x, width, column, text, data, cornersField: null);

		IEnumerable<int> ResolveRelatedCornerSlots(
			IReadOnlyDictionary<string, MiniYaml> primaryData,
			string cornersField,
			string orientationField)
		{
			if (terrainInfo == null)
				yield break;

			var cornersText = metadataFile.ReadField(primaryData, cornersField);
			if (string.IsNullOrWhiteSpace(cornersText))
				yield break;

			var mode = cornersField == "Related_corners_ring"
				? EditorOppositesMode.Ring
				: EditorOppositesMode.Island;
			var primarySlot = EditorTileMetadata.TryParseOrientationSlot(metadataFile.ReadField(primaryData, orientationField))
				?? EditorTileMetadata.TryParseOrientationSlot(metadataFile.ReadField(primaryData, "OppositesSlot"));
			if (!primarySlot.HasValue)
				yield break;

			var cornerSlots = EditorTileMetadata.TopologicalCornerSlotsForRelated(mode, primarySlot.Value);
			var usedSlots = new HashSet<int>();
			foreach (var reference in cornersText.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0))
			{
				TerrainTemplateInfo template = null;
				foreach (var row in metadataFile.TemplateRows(terrainInfo.Id))
				{
					if (!TemplateReferenceMatches(row, reference))
						continue;

					if (terrainInfo.Templates.TryGetValue(row.TemplateId, out template))
						break;
				}

				if (template == null)
					continue;

				var slot = EditorTileMetadata.InferCornerSlotFromWater(terrainInfo, template, cornerSlots, primarySlot.Value);
				if (slot == null)
				{
					foreach (var candidate in cornerSlots)
					{
						if (!usedSlots.Contains(candidate))
						{
							slot = candidate;
							break;
						}
					}
				}

				if (slot == null || usedSlots.Contains(slot.Value))
					continue;

				usedSlots.Add(slot.Value);
				yield return EditorTileMetadata.OppositesGridIndex(slot.Value);
			}
		}

		static bool TemplateReferenceMatches(MetadataTemplateRow row, string reference)
		{
			if (!string.IsNullOrEmpty(row.OriginalFilename) &&
				string.Equals(row.OriginalFilename, reference, StringComparison.OrdinalIgnoreCase))
				return true;

			return ushort.TryParse(reference, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) &&
				row.TemplateId == id;
		}

		static string FormatCellText(string text, int width, int height)
		{
			if (string.IsNullOrWhiteSpace(text) || !Game.Renderer.Fonts.TryGetValue("Tiny", out var font))
				return text;

			var lineHeight = Math.Max(1, font.Measure("Ag").Y);
			var maxLines = Math.Max(1, height / lineHeight);
			var lines = new List<string>();

			foreach (var part in text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
			{
				var wrapped = WidgetUtils.WrapText(part, width, font);
				lines.AddRange(wrapped.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
			}

			if (lines.Count == 0)
				return "";

			var clipped = lines.Take(maxLines).ToList();
			var overflow = lines.Count > maxLines;
			for (var i = 0; i < clipped.Count; i++)
				clipped[i] = WidgetUtils.TruncateText(clipped[i], width, font);

			if (overflow)
				clipped[^1] = WidgetUtils.TruncateText(clipped[^1] + " ...", width, font);

			return string.Join("\n", clipped);
		}
	}
}
