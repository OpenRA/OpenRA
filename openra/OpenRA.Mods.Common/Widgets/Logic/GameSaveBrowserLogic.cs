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
	public class GameSaveBrowserLogic : ChromeLogic
	{
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
		const string OverwriteSaveTitle = "dialog-overwrite-save.title";

		[FluentReference("file")]
		const string OverwriteSavePrompt = "dialog-overwrite-save.prompt";

		[FluentReference]
		const string OverwriteSaveAccept = "dialog-overwrite-save.confirm";

		[FluentReference]
		const string NoSaveSelected = "label-gamesave-browser-panel-no-save-selected";

		[FluentReference("name", "number")]
		const string EnumeratedBotName = "enumerated-bot-name";

		[FluentReference]
		const string HumanPlayer = "label-load-game-browser-panel-human-player";

		[FluentReference]
		const string Players = "label-players";

		[FluentReference("team")]
		const string TeamNumber = "label-team-name";

		[FluentReference]
		const string NoTeam = "label-no-team";

		readonly Widget panel;
		readonly ScrollPanelWidget gameList;
		readonly TextFieldWidget saveTextField;
		readonly List<string> games = [];
		readonly Action onExit;
		readonly ModData modData;
		readonly string baseSavePath;

		readonly ScrollPanelWidget playerList;
		readonly ScrollItemWidget playerHeader;
		readonly ScrollItemWidget playerTemplate;
		MapPreview map;

		readonly string defaultSaveFilename;
		string selectedPath;
		GameSave selectedSave;
		readonly World world;

		[ObjectCreator.UseCtor]
		public GameSaveBrowserLogic(Widget widget, ModData modData, Action onExit, Action onStart, World world)
		{
			panel = widget;

			this.modData = modData;
			this.onExit = onExit;
			this.world = world;
			Game.BeforeGameStart += OnGameStart;

			var cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");
			cancelButton.OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			gameList = panel.Get<ScrollPanelWidget>("GAME_LIST");
			var gameTemplate = panel.Get<ScrollItemWidget>("GAME_TEMPLATE");
			var dateHeaderTemplate = panel.Get<ScrollItemWidget>("DATE_HEADER");

			var mod = modData.Manifest;
			baseSavePath = Path.Combine(Platform.SupportDir, "Saves", mod.Id, mod.Metadata.Version);

			panel.Get("SAVE_TITLE").IsVisible = () => true;

			defaultSaveFilename = world.Map.Title;
			var filenameAttempt = 0;
			while (File.Exists(Path.Combine(baseSavePath, defaultSaveFilename + ".orasav")))
				defaultSaveFilename = world.Map.Title + $" ({++filenameAttempt})";

			var saveButton = panel.Get<ButtonWidget>("SAVE_BUTTON");
			saveButton.IsDisabled = () => string.IsNullOrWhiteSpace(saveTextField.Text);
			saveButton.OnClick = () => Save(world);
			saveButton.IsVisible = () => true;

			var saveWidgets = panel.Get("SAVE_WIDGETS");
			gameList.Bounds.Height -= saveWidgets.Bounds.Height;
			saveWidgets.IsVisible = () => true;

			saveTextField = saveWidgets.Get<TextFieldWidget>("SAVE_TEXTFIELD");
			saveTextField.OnEnterKey = saveButton.HandleKeyPress;
			saveTextField.OnEscKey = cancelButton.HandleKeyPress;
			saveTextField.OnTextEdited = () =>
			{
				if (string.IsNullOrEmpty(saveTextField.Text))
				{
					selectedPath = null;
					selectedSave = null;
					map = modData.MapCache[world.Map.Uid];
					UpdatePlayerList();
				}
			};

			if (Directory.Exists(baseSavePath))
				LoadGames(gameTemplate, dateHeaderTemplate, world);

			map = modData.MapCache[world.Map.Uid];

			var mapPreviewRoot = panel.Get("MAP_PREVIEW_ROOT");

			var saveInfo = panel.Get("SAVE_INFO");

			var noSaveSelectedLabel = saveInfo.Get<LabelWidget>("NO_SAVE_SELECTED_LABEL");
			noSaveSelectedLabel.GetText = () => FluentProvider.GetMessage(NoSaveSelected);
			noSaveSelectedLabel.IsVisible = () => false;

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
				savegameInfoDuration.GetText = () =>
				{
					if (selectedSave != null && selectedSave.GlobalSettings.GameTimestep > 0 && selectedSave.LastOrdersFrame >= 0)
					{
						var duration = TimeSpan.FromMilliseconds((long)selectedSave.LastOrdersFrame * selectedSave.GlobalSettings.GameTimestep);
						return "Duration: " + duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
					}

					return "Duration: ?";
				};
				savegameInfoDuration.IsVisible = () => selectedSave != null;
			}

			playerList = saveInfo.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

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

			var renameButton = panel.Get<ButtonWidget>("RENAME_BUTTON");
			renameButton.IsDisabled = () => selectedSave == null;
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
			deleteButton.IsDisabled = () => selectedSave == null;
			deleteButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteSaveTitle,
					text: DeleteSavePrompt,
					textArguments: ["save", Path.GetFileNameWithoutExtension(selectedPath)],
					onConfirm: () =>
					{
						Delete(selectedPath);
						SelectFirstVisible();
					},
					confirmText: DeleteSaveAccept,
					onCancel: () => { });
			};

			var deleteAllButton = panel.Get<ButtonWidget>("DELETE_ALL_BUTTON");
			deleteAllButton.IsDisabled = () => games.Count == 0;
			deleteAllButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteAllSavesTitle,
					text: DeleteAllSavesPrompt,
					textArguments: ["count", games.Count],
					onConfirm: () =>
					{
						foreach (var s in games.ToList())
							Delete(s);

						Ui.CloseWindow();
						onExit();
					},
					confirmText: DeleteAllSavesAccept,
					onCancel: () => { });
			};

			SelectFirstVisible();
		}

		void LoadGames(ScrollItemWidget gameTemplate, ScrollItemWidget dateHeaderTemplate, World world)
		{
			gameList.RemoveChildren();

			var savePaths = Directory.GetFiles(baseSavePath, "*.orasav", SearchOption.AllDirectories)
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
				header.IsVisible = () => true;
				gameList.AddChild(header);

				foreach (var savePath in group)
				{
					games.Add(savePath);

					GameSave save = null;
					try
					{
						save = new GameSave(savePath);
					}
					catch
					{
					}

					// Create the item manually so the click handlers can refer to itself.
					// This simplifies the rename handling (only needs to update ItemKey).
					var gameItem = gameTemplate.Clone();
					gameItem.ItemKey = savePath;
					gameItem.IsVisible = () => true;
					gameItem.IsSelected = () => selectedPath == gameItem.ItemKey;
					gameItem.OnClick = () => Select(gameItem.ItemKey);
					gameItem.OnDoubleClick = () => Save(world);

					var title = Path.GetFileNameWithoutExtension(savePath);
					var label = gameItem.Get<LabelWithTooltipWidget>("TITLE");
					WidgetUtils.TruncateLabelToTooltip(label, title);
					var tooltipText = GameSaveUtils.BuildSaveTooltipText(savePath, save, modData);
					label.GetTooltipText = () => tooltipText;

					var creationTime = File.GetCreationTime(savePath);
					var creationTimeLabel = gameItem.GetOrNull<LabelWidget>("CREATION_TIME");
					if (creationTimeLabel != null)
					{
						creationTimeLabel.GetText = () => creationTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
						creationTimeLabel.IsVisible = () => gameItem.IsSelected();
					}

					gameList.AddChild(gameItem);
				}
			}
		}

		void Rename(string oldName, string newName)
		{
			var oldPath = Path.Combine(baseSavePath, oldName + ".orasav");

			var uniqueName = newName;
			var attempt = 1;
			while (File.Exists(Path.Combine(baseSavePath, uniqueName + ".orasav")))
				uniqueName = newName + $" ({attempt++})";

			var newPath = Path.Combine(baseSavePath, uniqueName + ".orasav");

			try
			{
				File.Move(oldPath, newPath);

				games[games.IndexOf(oldPath)] = newPath;
				foreach (var c in gameList.Children)
				{
					if (c is not ScrollItemWidget item || item.ItemKey != oldPath)
						continue;

					item.ItemKey = newPath;
					item.Get<LabelWidget>("TITLE").GetText = () => uniqueName;
				}

				if (selectedPath == oldPath)
					selectedPath = newPath;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
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
				return;
			}

			if (savePath == selectedPath)
				Select(null);

			var item = gameList.Children
				.Select(c => c as ScrollItemWidget)
				.FirstOrDefault(c => c.ItemKey == savePath);

			gameList.RemoveChild(item);
			games.Remove(savePath);
		}

		void SelectFirstVisible()
		{
			Select(null);
			saveTextField.TakeKeyboardFocus();
			saveTextField.CursorPosition = saveTextField.Text.Length;
		}

		void Select(string savePath)
		{
			selectedPath = savePath;

			if (savePath != null)
			{
				try
				{
					selectedSave = new GameSave(savePath);
					var preview = modData.MapCache[selectedSave.GlobalSettings.Map];
					if (preview.Status != MapStatus.Available && selectedSave.MapGenerationArgs != null)
					{
						// Add to the MapCache so the server will accept the map.
						preview.UpdateFromGenerationArgs(selectedSave.MapGenerationArgs);
						preview.Generate();
					}

					map = preview;
				}
				catch
				{
					selectedSave = null;
					map = MapCache.UnknownMap;
				}

				UpdatePlayerList();
			}
			else
			{
				selectedSave = null;
				map = modData.MapCache[world.Map.Uid];
				UpdatePlayerList();
			}

			saveTextField.Text = savePath == null ? defaultSaveFilename : Path.GetFileNameWithoutExtension(savePath);
			saveTextField.CursorPosition = saveTextField.Text.Length;
		}

		Dictionary<string, SlotClient> GetWorldSlotClients()
		{
			var result = new Dictionary<string, SlotClient>();
			foreach (var client in world.LobbyInfo.Clients)
				if (client.Slot != null)
					result[client.Slot] = new SlotClient(client);
			return result;
		}

		Dictionary<int, SpawnOccupant> GetSpawnOccupants()
		{
			var slotClients = selectedSave?.SlotClients ?? (selectedPath == null ? GetWorldSlotClients() : null);
			if (slotClients == null)
				return [];

			var occupants = new Dictionary<int, SpawnOccupant>();
			foreach (var (_, slotClient) in slotClients)
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
					Name = slotClient.Bot != null ? FluentProvider.GetMessage(slotClient.BotName) : string.Empty
				};

				occupants[slotClient.SpawnPoint] = new SpawnOccupant(client);
			}

			return occupants;
		}

		void UpdatePlayerList()
		{
			playerList.RemoveChildren();

			Dictionary<string, SlotClient> slotClients;
			if (selectedSave != null)
				slotClients = selectedSave.SlotClients;
			else if (selectedPath == null)
				slotClients = GetWorldSlotClients();
			else
				return;

			var factionInfo = modData.DefaultRules.Actors[SystemActors.World].TraitInfos<FactionInfo>();

			var botOrdinals = slotClients
				.Where(kv => kv.Value.Bot != null)
				.GroupBy(kv => kv.Value.Bot)
				.SelectMany(g => g.Select((kv, i) => (SlotKey: kv.Key, Ordinal: i + 1)))
				.ToFrozenDictionary(x => x.SlotKey, x => x.Ordinal);

			var slotClientsByTeam = slotClients
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

		void Save(World world)
		{
			var filename = saveTextField.Text + ".orasav";
			var testPath = Path.Combine(
				Platform.SupportDir,
				"Saves",
				modData.Manifest.Id,
				modData.Manifest.Metadata.Version,
				filename);

			void Inner()
			{
				world.RequestGameSave(filename, false);
				Ui.CloseWindow();
				onExit();
			}

			if (File.Exists(testPath))
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: OverwriteSaveTitle,
					text: OverwriteSavePrompt,
					textArguments: ["file", saveTextField.Text],
					onConfirm: Inner,
					confirmText: OverwriteSaveAccept,
					onCancel: () => { });
			}
			else
				Inner();
		}

		void OnGameStart()
		{
			Ui.CloseWindow();
			onExit();
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
	}
}
