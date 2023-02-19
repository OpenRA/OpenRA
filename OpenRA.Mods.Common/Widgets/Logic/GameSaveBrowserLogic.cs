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
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class GameSaveBrowserLogic : ChromeLogic
	{
		[TranslationReference]
		const string RenameSaveTitle = "dialog-rename-save.title";

		[TranslationReference]
		const string RenameSavePrompt = "dialog-rename-save.prompt";

		[TranslationReference]
		const string RenameSaveAccept = "dialog-rename-save.confirm";

		[TranslationReference]
		const string DeleteSaveTitle = "dialog-delete-save.title";

		[TranslationReference("save")]
		const string DeleteSavePrompt = "dialog-delete-save.prompt";

		[TranslationReference]
		const string DeleteSaveAccept = "dialog-delete-save.confirm";

		[TranslationReference]
		const string DeleteAllSavesTitle = "dialog-delete-all-saves.title";

		[TranslationReference("count")]
		const string DeleteAllSavesPrompt = "dialog-delete-all-saves.prompt";

		[TranslationReference]
		const string DeleteAllSavesAccept = "dialog-delete-all-saves.confirm";

		[TranslationReference("savePath")]
		const string SaveDeletionFailed = "notification-save-deletion-failed";

		[TranslationReference]
		const string OverwriteSaveTitle = "dialog-overwrite-save.title";

		[TranslationReference("file")]
		const string OverwriteSavePrompt = "dialog-overwrite-save.prompt";

		[TranslationReference]
		const string OverwriteSaveAccpet = "dialog-overwrite-save.confirm";

		readonly Widget panel;
		readonly ScrollPanelWidget gameList;
		readonly TextFieldWidget saveTextField;
		readonly List<string> games = new List<string>();
		readonly Action onStart;
		readonly Action onExit;
		readonly ModData modData;
		readonly bool isSavePanel;
		readonly string baseSavePath;

		readonly string defaultSaveFilename;
		string selectedSave;

		[ObjectCreator.UseCtor]
		public GameSaveBrowserLogic(Widget widget, ModData modData, Action onExit, Action onStart, bool isSavePanel, World world)
		{
			panel = widget;

			this.modData = modData;
			this.onStart = onStart;
			this.onExit = onExit;
			this.isSavePanel = isSavePanel;
			Game.BeforeGameStart += OnGameStart;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			gameList = panel.Get<ScrollPanelWidget>("GAME_LIST");
			var gameTemplate = panel.Get<ScrollItemWidget>("GAME_TEMPLATE");
			var newTemplate = panel.Get<ScrollItemWidget>("NEW_TEMPLATE");

			var mod = modData.Manifest;
			baseSavePath = Path.Combine(Platform.SupportDir, "Saves", mod.Id, mod.Metadata.Version);

			// Avoid filename conflicts when creating new saves
			if (isSavePanel)
			{
				panel.Get("SAVE_TITLE").IsVisible = () => true;

				defaultSaveFilename = world.Map.Title;
				var filenameAttempt = 0;
				while (true)
				{
					if (!File.Exists(Path.Combine(baseSavePath, defaultSaveFilename + ".orasav")))
						break;

					defaultSaveFilename = world.Map.Title + $" ({++filenameAttempt})";
				}

				var saveButton = panel.Get<ButtonWidget>("SAVE_BUTTON");
				saveButton.IsDisabled = () => string.IsNullOrWhiteSpace(saveTextField.Text);
				saveButton.OnClick = () => { Save(world); };
				saveButton.IsVisible = () => true;

				var saveWidgets = panel.Get("SAVE_WIDGETS");
				saveTextField = saveWidgets.Get<TextFieldWidget>("SAVE_TEXTFIELD");
				gameList.Bounds.Height -= saveWidgets.Bounds.Height;
				saveWidgets.IsVisible = () => true;
			}
			else
			{
				panel.Get("LOAD_TITLE").IsVisible = () => true;
				var loadButton = panel.Get<ButtonWidget>("LOAD_BUTTON");
				loadButton.IsVisible = () => true;
				loadButton.IsDisabled = () => selectedSave == null;
				loadButton.OnClick = () => { Load(); };
			}

			if (Directory.Exists(baseSavePath))
				LoadGames(gameTemplate, newTemplate, world);

			var renameButton = panel.Get<ButtonWidget>("RENAME_BUTTON");
			renameButton.IsDisabled = () => selectedSave == null;
			renameButton.OnClick = () =>
			{
				var initialName = Path.GetFileNameWithoutExtension(selectedSave);
				var invalidChars = Path.GetInvalidFileNameChars();

				ConfirmationDialogs.TextInputPrompt(modData,
					RenameSaveTitle,
					RenameSavePrompt,
					initialName,
					onAccept: newName => Rename(initialName, newName),
					onCancel: null,
					acceptText: RenameSaveAccept,
					cancelText: null,
					inputValidator: newName =>
					{
						if (newName == initialName)
							return false;

						if (string.IsNullOrWhiteSpace(newName))
							return false;

						if (newName.IndexOfAny(invalidChars) >= 0)
							return false;

						if (File.Exists(Path.Combine(baseSavePath, newName)))
							return false;

						return true;
					});
			};

			var deleteButton = panel.Get<ButtonWidget>("DELETE_BUTTON");
			deleteButton.IsDisabled = () => selectedSave == null;
			deleteButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteSaveTitle,
					text: DeleteSavePrompt,
					textArguments: Translation.Arguments("save", Path.GetFileNameWithoutExtension(selectedSave)),
					onConfirm: () =>
					{
						Delete(selectedSave);

						if (games.Count == 0 && !isSavePanel)
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
			deleteAllButton.IsDisabled = () => games.Count == 0;
			deleteAllButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: DeleteAllSavesTitle,
					text: DeleteAllSavesPrompt,
					textArguments: Translation.Arguments("count", games.Count),
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

		void LoadGames(ScrollItemWidget gameTemplate, ScrollItemWidget newTemplate, World world)
		{
			gameList.RemoveChildren();
			if (isSavePanel)
			{
				var item = ScrollItemWidget.Setup(newTemplate,
					() => selectedSave == null,
					() => Select(null),
					() => { });
				gameList.AddChild(item);
			}

			var savePaths = Directory.GetFiles(baseSavePath, "*.orasav", SearchOption.AllDirectories)
				.OrderByDescending(p => File.GetLastWriteTime(p))
				.ToList();

			foreach (var savePath in savePaths)
			{
				games.Add(savePath);

				// Create the item manually so the click handlers can refer to itself
				// This simplifies the rename handling (only needs to update ItemKey)
				var item = gameTemplate.Clone() as ScrollItemWidget;
				item.ItemKey = savePath;
				item.IsVisible = () => true;
				item.IsSelected = () => selectedSave == item.ItemKey;
				item.OnClick = () => Select(item.ItemKey);

				if (isSavePanel)
					item.OnDoubleClick = () => Save(world);
				else
					item.OnDoubleClick = Load;

				var title = Path.GetFileNameWithoutExtension(savePath);
				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				WidgetUtils.TruncateLabelToTooltip(label, title);

				var date = File.GetLastWriteTime(savePath).ToString("yyyy-MM-dd HH:mm:ss");
				item.Get<LabelWidget>("DATE").GetText = () => date;

				gameList.AddChild(item);
			}
		}

		void Rename(string oldName, string newName)
		{
			try
			{
				var oldPath = Path.Combine(baseSavePath, oldName + ".orasav");
				var newPath = Path.Combine(baseSavePath, newName + ".orasav");
				File.Move(oldPath, newPath);

				games[games.IndexOf(oldPath)] = newPath;
				foreach (var c in gameList.Children)
				{
					if (!(c is ScrollItemWidget item) || item.ItemKey != oldPath)
						continue;

					item.ItemKey = newPath;
					item.Get<LabelWidget>("TITLE").GetText = () => newName;
				}

				if (selectedSave == oldPath)
					selectedSave = newPath;
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
				TextNotificationsManager.Debug(modData.Translation.GetString(SaveDeletionFailed, Translation.Arguments("savePath", savePath)));
				Log.Write("debug", ex.ToString());
				return;
			}

			if (savePath == selectedSave)
				Select(null);

			var item = gameList.Children
				.Select(c => c as ScrollItemWidget)
				.FirstOrDefault(c => c.ItemKey == savePath);

			gameList.RemoveChild(item);
			games.Remove(savePath);
		}

		void SelectFirstVisible()
		{
			Select(isSavePanel ? null : games.FirstOrDefault());
		}

		void Select(string savePath)
		{
			selectedSave = savePath;
			if (isSavePanel)
				saveTextField.Text = savePath == null ? defaultSaveFilename :
					Path.GetFileNameWithoutExtension(savePath);
		}

		void Load()
		{
			if (selectedSave == null)
				return;

			// Parse the save to find the map UID
			var save = new GameSave(selectedSave);
			var map = modData.MapCache[save.GlobalSettings.Map];
			if (map.Status != MapStatus.Available)
				return;

			var orders = new List<Order>()
			{
				Order.FromTargetString("LoadGameSave", Path.GetFileName(selectedSave), true),
				Order.Command($"state {Session.ClientState.Ready}")
			};

			Game.CreateAndStartLocalServer(map.Uid, orders);
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
				world.RequestGameSave(filename);
				Ui.CloseWindow();
				onExit();
			}

			if (selectedSave != null || File.Exists(testPath))
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: OverwriteSaveTitle,
					text: OverwriteSavePrompt,
					textArguments: Translation.Arguments("file", saveTextField.Text),
					onConfirm: Inner,
					confirmText: OverwriteSaveAccpet,
					onCancel: () => { });
			}
			else
				Inner();
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

			return Directory.GetFiles(baseSavePath, "*.orasav", SearchOption.AllDirectories).Length > 0;
		}
	}
}
