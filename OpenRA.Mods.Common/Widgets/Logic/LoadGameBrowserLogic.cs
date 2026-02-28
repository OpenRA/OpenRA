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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(typeof(GameSaveUtils))]
	public class LoadGameBrowserLogic : ChromeLogic
	{
		enum SaveType
		{
			Any,
			Autosave,
			Manual
		}

		enum DateType
		{
			Any,
			Today,
			LastWeek,
			LastFortnight,
			LastMonth
		}

		enum DurationType
		{
			Any,
			VeryShort,
			Short,
			Medium,
			Long
		}

		sealed class SaveEntry
		{
			public string Path;
			public DateTime LastWrite;
			public DateTime CreationTime;
			public TimeSpan? Duration;
			public string MapTitle;
			public IReadOnlyList<string> Factions = [];
			public bool Visible = true;
			public ScrollItemWidget Item;
		}

		sealed class Filter
		{
			public SaveType Type;
			public DateType Date;
			public DurationType Duration;
			public string SaveName;
			public string MapName;
			public string Faction;

			public bool IsEmpty =>
				Type == default
				&& Date == default
				&& Duration == default
				&& string.IsNullOrEmpty(SaveName)
				&& string.IsNullOrEmpty(MapName)
				&& string.IsNullOrEmpty(Faction);
		}

		[FluentReference]
		const string RenameSaveTitle = "dialog-rename-save.title";

		[FluentReference]
		const string RenameSavePrompt = "dialog-rename-save.prompt";

		[FluentReference]
		const string RenameSaveAccept = "dialog-rename-save.confirm";

		[FluentReference]
		const string DeleteSaveTitle = "dialog-delete-save.title";

		[FluentReference("save")]
		const string DeleteSavePrompt = "dialog-delete-save.prompt";

		[FluentReference]
		const string DeleteSaveAccept = "dialog-delete-save.confirm";

		[FluentReference]
		const string DeleteAllSavesTitle = "dialog-delete-all-saves.title";

		[FluentReference("count")]
		const string DeleteAllSavesPrompt = "dialog-delete-all-saves.prompt";

		[FluentReference]
		const string DeleteAllSavesAccept = "dialog-delete-all-saves.confirm";

		[FluentReference("savePath")]
		const string SaveDeletionFailed = "notification-save-deletion-failed";

		[FluentReference]
		const string Players = "label-players";

		[FluentReference("team")]
		const string TeamNumber = "label-team-name";

		[FluentReference]
		const string NoTeam = "label-no-team";

		[FluentReference]
		const string SaveTypeAutosave = "options-save-type.autosave";

		[FluentReference]
		const string SaveTypeManual = "options-save-type.manual";

		[FluentReference]
		const string Today = "options-replay-date.today";

		[FluentReference]
		const string LastWeek = "options-replay-date.last-week";

		[FluentReference]
		const string LastFortnight = "options-replay-date.last-fortnight";

		[FluentReference]
		const string LastMonth = "options-replay-date.last-month";

		[FluentReference]
		const string SaveDurationVeryShort = "options-replay-duration.very-short";

		[FluentReference]
		const string SaveDurationShort = "options-replay-duration.short";

		[FluentReference]
		const string SaveDurationMedium = "options-replay-duration.medium";

		[FluentReference]
		const string SaveDurationLong = "options-replay-duration.long";

		[FluentReference("name", "number")]
		const string EnumeratedBotName = "enumerated-bot-name";

		[FluentReference]
		const string HumanPlayer = "label-load-game-browser-panel-human-player";

		static Filter filter = new();

		readonly Widget panel;
		readonly ScrollPanelWidget gameList;
		readonly ScrollPanelWidget playerList;
		readonly ScrollItemWidget playerTemplate;
		readonly ScrollItemWidget playerHeader;
		readonly ScrollItemWidget gameTemplate;
		readonly ScrollItemWidget dateHeaderTemplate;
		readonly List<SaveEntry> saves = [];
		readonly Action onStart;
		readonly ModData modData;
		readonly string baseSavePath;

		MapPreview map;
		string selectedPath;
		GameSave selectedSave;
		bool filtersVisible;

		[ObjectCreator.UseCtor]
		public LoadGameBrowserLogic(Widget widget, ModData modData, Action onExit, Action onStart)
		{
			// Reset filters to their neutral state every time the panel opens.
			filter = new Filter();

			map = MapCache.UnknownMap;
			panel = widget;

			this.modData = modData;
			this.onStart = onStart;
			Game.BeforeGameStart += OnGameStart;

			var mod = modData.Manifest;
			baseSavePath = Path.Combine(Platform.SupportDir, "Saves", mod.Id, mod.Metadata.Version);

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			playerList = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

			var loadButton = panel.Get<ButtonWidget>("LOAD_BUTTON");
			loadButton.IsDisabled = () => selectedSave == null || modData.MapCache[selectedSave.GlobalSettings.Map].Status != MapStatus.Available;
			loadButton.OnClick = Load;

			var mapPreviewRoot = panel.Get("MAP_PREVIEW_ROOT");
			mapPreviewRoot.IsVisible = () => selectedPath != null;
			var saveInfo = panel.Get("SAVE_INFO");
			saveInfo.IsVisible = () => selectedPath != null;

			var incompatibleTitleLabel = saveInfo.Get<LabelWidget>("INCOMPATIBLE_TITLE_LABEL");
			incompatibleTitleLabel.IsVisible = () => selectedPath != null && selectedSave == null;

			var incompatibleLabelA = saveInfo.Get<LabelWidget>("INCOMPATIBLE_LABEL_A");
			incompatibleLabelA.IsVisible = () => selectedPath != null && selectedSave == null;

			var incompatibleLabelB = saveInfo.Get<LabelWidget>("INCOMPATIBLE_LABEL_B");
			incompatibleLabelB.IsVisible = () => selectedPath != null && selectedSave == null;

			var savegameInfoDate = saveInfo.GetOrNull<LabelWidget>("SAVEGAME_INFO_DATE");
			if (savegameInfoDate != null)
			{
				savegameInfoDate.GetText = () => selectedSave != null && selectedPath != null
					? "Date created: " + File.GetCreationTime(selectedPath).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
					: string.Empty;
				savegameInfoDate.IsVisible = () => selectedSave != null;
			}

			var savegameInfoDuration = saveInfo.GetOrNull<LabelWidget>("SAVEGAME_INFO_DURATION");
			if (savegameInfoDuration != null)
			{
				savegameInfoDuration.GetText = () => "Duration: " + GameSaveUtils.FormatGameDuration(GameSaveUtils.GetGameDuration(selectedSave));
				savegameInfoDuration.IsVisible = () => selectedSave != null;
			}

			var playerListWidget = saveInfo.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerListWidget.IsVisible = () => selectedPath != null;

			var spawnOccupants = new CachedTransform<GameSave, Dictionary<int, SpawnOccupant>>(_ => GetSpawnOccupants());

			Ui.LoadWidget("MAP_PREVIEW", mapPreviewRoot, new WidgetArgs
			{
				{ "orderManager", null },
				{ "getMap", (Func<(MapPreview, Session.MapStatus)>)(() => (map, Session.MapStatus.Playable)) },
				{ "onMouseDown", null },
				{ "getSpawnOccupants", (Func<Dictionary<int, SpawnOccupant>>)(() => spawnOccupants.Update(selectedSave)) },
				{ "getDisabledSpawnPoints", () => FrozenSet<int>.Empty },
				{ "showUnoccupiedSpawnpoints", false },
				{ "mapUpdatesEnabled", false },
				{ "onMapUpdate", (Action<string>)(_ => { }) },
			});

			gameList = panel.Get<ScrollPanelWidget>("GAME_LIST");
			gameTemplate = panel.Get<ScrollItemWidget>("GAME_TEMPLATE");
			dateHeaderTemplate = panel.Get<ScrollItemWidget>("DATE_HEADER");

			SetupFilters();
			SetupManagement(onExit);

			if (Directory.Exists(baseSavePath))
				LoadGames(gameTemplate, dateHeaderTemplate);

			SetupSaveDependentFilters();
			ApplyFilter();
		}

		void LoadGames(ScrollItemWidget gameTemplate, ScrollItemWidget dateHeaderTemplate)
		{
			gameList.RemoveChildren();

			var savePaths = Directory.GetFiles(baseSavePath, "*.orasav")
				.OrderByDescending(File.GetLastWriteTime)
				.ToList();

			var byDate = savePaths
				.GroupBy(p => File.GetLastWriteTime(p).Date)
				.OrderByDescending(g => g.Key);

			foreach (var group in byDate)
			{
				var dateLabel = group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				var header = ScrollItemWidget.Setup(dateHeaderTemplate, () => false, () => { });
				header.Get<LabelWidget>("LABEL").GetText = () => dateLabel;

				// The header is visible only if at least one save in this group is visible.
				// Assigned after all entries in the group are created.
				var groupEntries = new List<SaveEntry>();
				header.IsVisible = () => groupEntries.Any(e => e.Visible);
				gameList.AddChild(header);

				foreach (var savePath in group)
				{
					GameSave save = null;
					try
					{
						save = new GameSave(savePath);
					}
					catch
					{
					}

					var lastWrite = File.GetLastWriteTime(savePath);
					var creationTime = File.GetCreationTime(savePath);
					var entry = new SaveEntry
					{
						Path = savePath,
						LastWrite = lastWrite,
						CreationTime = creationTime,
						Duration = GameSaveUtils.GetGameDuration(save),
						MapTitle = save != null ? modData.MapCache[save.GlobalSettings.Map].Title : null,
						Factions = save?.SlotClients.Values
							.Select(sc => sc.Faction)
							.Where(f => !string.IsNullOrEmpty(f))
							.ToList() ?? []
					};

					saves.Add(entry);
					groupEntries.Add(entry);

					var item = gameTemplate.Clone();
					item.ItemKey = savePath;
					item.IsSelected = () => selectedPath == item.ItemKey;
					item.OnClick = () => SelectSave(item.ItemKey);
					item.OnDoubleClick = Load;

					var title = Path.GetFileNameWithoutExtension(savePath);
					var label = item.Get<LabelWithTooltipWidget>("TITLE");
					WidgetUtils.TruncateLabelToTooltip(label, title);
					var tooltipText = GameSaveUtils.BuildSaveTooltipText(savePath, save, modData);
					label.GetTooltipText = () => tooltipText;

					var creationTimeLabel = item.GetOrNull<LabelWidget>("CREATION_TIME");
					if (creationTimeLabel != null)
					{
						creationTimeLabel.GetText = () => entry.CreationTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
						creationTimeLabel.IsVisible = () => item.IsSelected();
					}

					entry.Item = item;
					item.IsVisible = () => entry.Visible;

					gameList.AddChild(item);
				}
			}
		}

		void SetupFilters()
		{
			TextFieldWidget nameInput = null;

			// Save name
			{
				nameInput = panel.GetOrNull<TextFieldWidget>("FLT_NAME_INPUT");
				if (nameInput != null)
				{
					nameInput.Text = filter.SaveName ?? string.Empty;
					nameInput.OnEscKey = _ =>
					{
						filter.SaveName = nameInput.Text = null;
						ApplyFilter();
						return true;
					};
					nameInput.OnTextEdited = () =>
					{
						filter.SaveName = string.IsNullOrEmpty(nameInput.Text) ? null : nameInput.Text;
						ApplyFilter();
					};
				}
			}

			// Save type
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_TYPE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					(SaveType SaveType, string Text)[] options =
					[
						(SaveType.Any, ddb.GetText()),
						(SaveType.Autosave, FluentProvider.GetMessage(SaveTypeAutosave)),
						(SaveType.Manual, FluentProvider.GetMessage(SaveTypeManual))
					];

					var lookup = options.ToFrozenDictionary(kvp => kvp.SaveType, kvp => kvp.Text);

					ddb.GetText = () => lookup[filter.Type];
					ddb.OnMouseDown = _ =>
					{
						ScrollItemWidget SetupItem((SaveType SaveType, string Text) option, ScrollItemWidget tpl)
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Type == option.SaveType,
								() => { filter.Type = option.SaveType; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Text;
							return item;
						}

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, SetupItem);
					};
				}
			}

			// Date
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DATE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					(DateType DateType, string Text)[] options =
					[
						(DateType.Any, ddb.GetText()),
						(DateType.Today, FluentProvider.GetMessage(Today)),
						(DateType.LastWeek, FluentProvider.GetMessage(LastWeek)),
						(DateType.LastFortnight, FluentProvider.GetMessage(LastFortnight)),
						(DateType.LastMonth, FluentProvider.GetMessage(LastMonth))
					];

					var lookup = options.ToFrozenDictionary(kvp => kvp.DateType, kvp => kvp.Text);

					ddb.GetText = () => lookup[filter.Date];
					ddb.OnMouseDown = _ =>
					{
						ScrollItemWidget SetupItem((DateType DateType, string Text) option, ScrollItemWidget tpl)
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Date == option.DateType,
								() => { filter.Date = option.DateType; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Text;
							return item;
						}

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, SetupItem);
					};
				}
			}

			// Duration
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DURATION_DROPDOWNBUTTON");
				if (ddb != null)
				{
					(DurationType DurationType, string Text)[] options =
					[
						(DurationType.Any, ddb.GetText()),
						(DurationType.VeryShort, FluentProvider.GetMessage(SaveDurationVeryShort)),
						(DurationType.Short, FluentProvider.GetMessage(SaveDurationShort)),
						(DurationType.Medium, FluentProvider.GetMessage(SaveDurationMedium)),
						(DurationType.Long, FluentProvider.GetMessage(SaveDurationLong))
					];

					var lookup = options.ToFrozenDictionary(kvp => kvp.DurationType, kvp => kvp.Text);

					ddb.GetText = () => lookup[filter.Duration];
					ddb.OnMouseDown = _ =>
					{
						ScrollItemWidget SetupItem((DurationType DurationType, string Text) option, ScrollItemWidget tpl)
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Duration == option.DurationType,
								() => { filter.Duration = option.DurationType; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Text;
							return item;
						}

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, SetupItem);
					};
				}
			}

			// Reset
			{
				var button = panel.Get<ButtonWidget>("FLT_RESET_BUTTON");
				button.IsDisabled = () => filter.IsEmpty;
				button.OnClick = () =>
				{
					filter = new Filter();
					if (nameInput != null)
						nameInput.Text = string.Empty;
					SetupSaveDependentFilters();
					ApplyFilter();
				};
			}
		}

		void SetupSaveDependentFilters()
		{
			// Map name
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_MAPNAME_DROPDOWNBUTTON");
				if (ddb != null)
				{
					var mapNames = saves
						.Select(s => s.MapTitle)
						.Where(t => t != null)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
						.ToList();

					mapNames.Insert(0, null);

					var anyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.MapName) ? anyText : filter.MapName;
					ddb.OnMouseDown = _ =>
					{
						ScrollItemWidget SetupItem(string option, ScrollItemWidget tpl)
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Equals(filter.MapName, option, StringComparison.CurrentCultureIgnoreCase),
								() => { filter.MapName = option; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option ?? anyText;
							return item;
						}

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, mapNames, SetupItem);
					};
				}
			}

			// Faction
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_FACTION_DROPDOWNBUTTON");
				if (ddb != null)
				{
					var factions = saves
						.SelectMany(s => s.Factions)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
						.ToList();

					factions.Insert(0, null);

					var anyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.Faction) ? anyText : FluentProvider.GetMessage(filter.Faction);
					ddb.OnMouseDown = _ =>
					{
						ScrollItemWidget SetupItem(string option, ScrollItemWidget tpl)
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Equals(filter.Faction, option, StringComparison.CurrentCultureIgnoreCase),
								() => { filter.Faction = option; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option != null ? FluentProvider.GetMessage(option) : anyText;
							return item;
						}

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, factions, SetupItem);
					};
				}
			}
		}

		static bool EvaluateSaveVisibility(SaveEntry entry)
		{
			// Save type
			if (filter.Type != SaveType.Any)
			{
				var isAutosave = Path.GetFileNameWithoutExtension(entry.Path)
					.StartsWith("autosave", StringComparison.OrdinalIgnoreCase);

				if (filter.Type == SaveType.Autosave && !isAutosave)
					return false;

				if (filter.Type == SaveType.Manual && isAutosave)
					return false;
			}

			// Date
			if (filter.Date != DateType.Any)
			{
				TimeSpan t;
				switch (filter.Date)
				{
					case DateType.Today:
						t = TimeSpan.FromDays(1d);
						break;

					case DateType.LastWeek:
						t = TimeSpan.FromDays(7d);
						break;

					case DateType.LastFortnight:
						t = TimeSpan.FromDays(14d);
						break;

					case DateType.LastMonth:
					default:
						t = TimeSpan.FromDays(30d);
						break;
				}

				if (entry.LastWrite < DateTime.Now - t)
					return false;
			}

			// Duration
			if (filter.Duration != DurationType.Any)
			{
				if (!entry.Duration.HasValue)
					return true;

				var minutes = entry.Duration.Value.TotalMinutes;
				switch (filter.Duration)
				{
					case DurationType.VeryShort:
						if (minutes >= 5)
							return false;
						break;

					case DurationType.Short:
						if (minutes < 5 || minutes >= 20)
							return false;
						break;

					case DurationType.Medium:
						if (minutes < 20 || minutes >= 60)
							return false;
						break;

					case DurationType.Long:
						if (minutes < 60)
							return false;
						break;
				}
			}

			// Save name
			if (!string.IsNullOrEmpty(filter.SaveName))
			{
				var saveName = Path.GetFileNameWithoutExtension(entry.Path);
				if (!saveName.Contains(filter.SaveName, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			// Map name
			if (!string.IsNullOrEmpty(filter.MapName) &&
				!string.Equals(filter.MapName, entry.MapTitle, StringComparison.CurrentCultureIgnoreCase))
				return false;

			// Faction
			if (!string.IsNullOrEmpty(filter.Faction) &&
				!entry.Factions.Any(f => string.Equals(filter.Faction, f, StringComparison.CurrentCultureIgnoreCase)))
				return false;

			return true;
		}

		void ApplyFilter()
		{
			foreach (var entry in saves)
				entry.Visible = EvaluateSaveVisibility(entry);

			if (selectedPath == null)
				SelectFirstVisible();
			else if (saves.All(s => s.Path != selectedPath || !s.Visible))
			{
				var firstVisible = saves.FirstOrDefault(s => s.Visible);
				if (firstVisible != null)
					SelectSave(firstVisible.Path);
			}

			gameList.Layout.AdjustChildren();
			gameList.ScrollToSelectedItem();
		}

		void SetupFiltersToggle()
		{
			var filtersToggle = panel.GetOrNull<CheckboxWidget>("FILTERS_TOGGLE");
			if (filtersToggle == null)
				return;

			var filterContainer = panel.GetOrNull("FILTER_AND_MANAGE_CONTAINER") ?? panel.GetOrNull("FILTERS");
			var saveListContainer = panel.GetOrNull("SAVE_LIST_CONTAINER");
			if (filterContainer == null || saveListContainer == null)
				return;

			filterContainer.IsVisible = () => filtersVisible;
			filtersToggle.IsChecked = () => filtersVisible;

			// The YAML lays the save list out as if the filter panel were visible.
			var saveListNormalBounds = saveListContainer.Bounds;
			var expandedX = filterContainer.Bounds.X;
			var expandedWidth = saveListNormalBounds.Right - expandedX;

			void ApplyFiltersVisibility(bool reloadGames)
			{
				var newX = filtersVisible ? saveListNormalBounds.X : expandedX;
				var newWidth = filtersVisible ? saveListNormalBounds.Width : expandedWidth;
				var widthDelta = newWidth - saveListContainer.Bounds.Width;

				saveListContainer.Bounds = new WidgetBounds(newX, saveListNormalBounds.Y, newWidth, saveListNormalBounds.Height);

				var saveListLabel = saveListContainer.GetOrNull<LabelWidget>("SAVE_LIST_LABEL");
				if (saveListLabel != null)
					saveListLabel.Bounds.Width += widthDelta;

				gameList.Bounds.Width += widthDelta;

				gameTemplate.Bounds.Width += widthDelta;
				foreach (var child in gameTemplate.Children)
					child.Bounds.Width += widthDelta;

				dateHeaderTemplate.Bounds.Width += widthDelta;
				foreach (var child in dateHeaderTemplate.Children)
					child.Bounds.Width += widthDelta;

				if (reloadGames)
				{
					LoadGames(gameTemplate, dateHeaderTemplate);
					ApplyFilter();
				}
			}

			filtersToggle.OnClick = () =>
			{
				filtersVisible = !filtersVisible;
				ApplyFiltersVisibility(reloadGames: true);
			};

			// Filters are hidden by default, so widen the save list to fill the freed space.
			if (!filtersVisible)
				ApplyFiltersVisibility(reloadGames: false);
		}

		void SetupManagement(Action onExit)
		{
			SetupFiltersToggle();

			var renameButton = panel.Get<ButtonWidget>("RENAME_BUTTON");
			renameButton.IsDisabled = () => selectedPath == null;
			renameButton.OnClick = () =>
			{
				var initialName = Path.GetFileNameWithoutExtension(selectedPath);

				ConfirmationDialogs.TextInputPrompt(modData,
					RenameSaveTitle,
					RenameSavePrompt,
					initialName,
					onAccept: newName => Rename(initialName, newName),
					onCancel: null,
					acceptText: RenameSaveAccept,
					cancelText: null,
					inputValidator: newName => GameSaveUtils.IsValidNewSaveName(newName, initialName, baseSavePath));
			};

			var deleteButton = panel.Get<ButtonWidget>("DELETE_BUTTON");
			deleteButton.IsDisabled = () => selectedPath == null;
			deleteButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteSaveTitle,
					text: DeleteSavePrompt,
					textArguments: ["save", Path.GetFileNameWithoutExtension(selectedPath)],
					onConfirm: () =>
					{
						Delete(selectedPath);

						if (!saves.Any(s => s.Visible))
						{
							Ui.CloseWindow();
							onExit();
						}
						else
							SelectFirstVisible();
					},
					confirmText: DeleteSaveAccept,
					onCancel: () => { });
			};

			var deleteAllButton = panel.Get<ButtonWidget>("DELETE_ALL_BUTTON");
			deleteAllButton.IsDisabled = () => !saves.Any(s => s.Visible);
			deleteAllButton.OnClick = () =>
			{
				var visible = saves.Where(s => s.Visible).ToList();

				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteAllSavesTitle,
					text: DeleteAllSavesPrompt,
					textArguments: ["count", visible.Count],
					onConfirm: () =>
					{
						foreach (var s in visible)
							Delete(s.Path);

						if (!saves.Any(s => s.Visible))
						{
							Ui.CloseWindow();
							onExit();
						}
					},
					confirmText: DeleteAllSavesAccept,
					onCancel: () => { });
			};
		}

		void Rename(string oldName, string newName)
		{
			try
			{
				var oldPath = Path.Combine(baseSavePath, oldName + ".orasav");
				var newPath = Path.Combine(baseSavePath, newName + ".orasav");
				File.Move(oldPath, newPath);

				var entry = saves.First(s => s.Path == oldPath);
				entry.Path = newPath;

				foreach (var c in gameList.Children)
				{
					if (c is not ScrollItemWidget item || item.ItemKey != oldPath)
						continue;

					item.ItemKey = newPath;
					item.Get<LabelWidget>("TITLE").GetText = () => newName;
				}

				if (selectedPath == oldPath)
					selectedPath = newPath;
			}
			catch (Exception ex)
			{
				Log.Write("debug", ex.ToString());
			}
		}

		void Delete(string savePath)
		{
			try
			{
				File.Delete(savePath);
			}
			catch (Exception ex)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(SaveDeletionFailed, "savePath", savePath));
				Log.Write("debug", ex.ToString());
			}

			if (File.Exists(savePath))
				return;

			if (savePath == selectedPath)
				SelectSave(null);

			var entry = saves.First(s => s.Path == savePath);
			gameList.RemoveChild(entry.Item);
			saves.Remove(entry);
		}

		void SelectFirstVisible()
		{
			SelectSave(saves.FirstOrDefault(s => s.Visible)?.Path);
		}

		void SelectSave(string savePath)
		{
			selectedPath = savePath;
			playerList.RemoveChildren();

			if (savePath == null)
			{
				selectedSave = null;
				map = MapCache.UnknownMap;
				return;
			}

			try
			{
				selectedSave = new GameSave(savePath);
			}
			catch (Exception ex)
			{
				Log.Write("debug", $"Failed to load save file '{savePath}': {ex}");
				selectedSave = null;
				map = MapCache.UnknownMap;
				return;
			}

			var preview = modData.MapCache[selectedSave.GlobalSettings.Map];
			if (preview.Status != MapStatus.Available && selectedSave.MapGenerationArgs != null)
			{
				preview.UpdateFromGenerationArgs(selectedSave.MapGenerationArgs);
				preview.Generate();
			}

			map = preview;

			UpdatePlayerList();
		}

		Dictionary<int, SpawnOccupant> GetSpawnOccupants()
		{
			if (selectedSave == null)
				return [];

			var occupants = new Dictionary<int, SpawnOccupant>();
			foreach (var (_, slotClient) in selectedSave.SlotClients)
			{
				if (slotClient.SpawnPoint == 0)
					continue;

				var client = new Session.Client
				{
					Color = slotClient.Color,
					Faction = slotClient.Faction,
					SpawnPoint = slotClient.SpawnPoint,
					Team = slotClient.Team,
					Bot = slotClient.Bot,
					Name = slotClient.Bot != null ? slotClient.BotName : string.Empty
				};

				occupants[slotClient.SpawnPoint] = new SpawnOccupant(client);
			}

			return occupants;
		}

		void UpdatePlayerList()
		{
			playerList.RemoveChildren();

			if (selectedSave == null)
				return;

			var factionInfo = modData.DefaultRules.Actors[SystemActors.World].TraitInfos<FactionInfo>();

			var botOrdinals = selectedSave.SlotClients
				.Where(kv => kv.Value.Bot != null)
				.GroupBy(kv => kv.Value.Bot)
				.SelectMany(g => g.Select((kv, i) => (SlotKey: kv.Key, Ordinal: i + 1)))
				.ToFrozenDictionary(x => x.SlotKey, x => x.Ordinal);

			var slotClientsByTeam = selectedSave.SlotClients
				.GroupBy(kv => kv.Value.Team)
				.OrderBy(g => g.Key)
				.ToList();

			var noTeams = slotClientsByTeam.Count == 1;

			foreach (var teamGroup in slotClientsByTeam)
			{
				var team = teamGroup.Key;
				var label = noTeams ? FluentProvider.GetMessage(Players) : team > 0
					? FluentProvider.GetMessage(TeamNumber, "team", team)
					: FluentProvider.GetMessage(NoTeam);

				if (label.Length > 0)
				{
					var header = ScrollItemWidget.Setup(playerHeader, () => false, () => { });
					header.Get<LabelWidget>("LABEL").GetText = () => label;
					playerList.AddChild(header);
				}

				foreach (var (slotKey, slotClient) in teamGroup)
				{
					var displayName = slotClient.Bot != null
						? FluentProvider.GetMessage(EnumeratedBotName,
							"name", FluentProvider.GetMessage(slotClient.BotName),
							"number", botOrdinals[slotKey])
						: FluentProvider.GetMessage(HumanPlayer);

					var color = slotClient.Color;
					var item = ScrollItemWidget.Setup(playerTemplate, () => false, () => { });

					var nameLabel = item.Get<LabelWidget>("LABEL");
					var font = Game.Renderer.Fonts[nameLabel.Font];
					var name = WidgetUtils.TruncateText(displayName, nameLabel.Bounds.Width, font);
					nameLabel.GetText = () => name;
					nameLabel.GetColor = () => color;

					var flag = item.Get<ImageWidget>("FLAG");
					flag.GetImageCollection = () => "flags";
					var faction = slotClient.Faction;
					flag.GetImageName = () => factionInfo != null && factionInfo.Any(f => f.InternalName == faction) ? faction : "Random";

					playerList.AddChild(item);
				}
			}
		}

		void Load()
		{
			if (selectedSave == null)
				return;

			var mapPreview = modData.MapCache[selectedSave.GlobalSettings.Map];
			if (mapPreview.Status != MapStatus.Available)
				return;

			Ui.CloseWindow();

			var orders = new List<Order>
			{
				Order.FromTargetString("LoadGameSave", Path.GetFileName(selectedPath), true),
				Order.Command($"state {Session.ClientState.Ready}")
			};

			Game.CreateAndStartLocalServer(mapPreview.Uid, orders);
		}

		void OnGameStart()
		{
			Ui.CloseWindow();
			onStart();
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.BeforeGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}

		public static bool IsLoadPanelEnabled(Manifest mod)
		{
			var baseSavePath = Path.Combine(Platform.SupportDir, "Saves", mod.Id, mod.Metadata.Version);
			if (!Directory.Exists(baseSavePath))
				return false;

			return Directory.GetFiles(baseSavePath, "*.orasav").Length > 0;
		}
	}
}
