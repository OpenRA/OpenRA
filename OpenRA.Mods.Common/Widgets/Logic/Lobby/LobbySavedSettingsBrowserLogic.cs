#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class LobbySavedSettingsBrowserLogic : ChromeLogic
	{
		const string SettingsKeyMapId = "Map";
		const string SettingsKeyOptions = "Options";

		readonly Widget panel;
		readonly ScrollPanelWidget saveList;
		readonly TextFieldWidget saveTextField;
		readonly List<string> saves = new List<string>();
		readonly ModData modData;
		readonly bool isSavePanel;
		readonly OrderManager orderManager;
		readonly MapPreview map;

		readonly string baseSavePath;
		readonly string defaultSaveFilename;
		string selectedSave;

		[ObjectCreator.UseCtor]
		public LobbySavedSettingsBrowserLogic(Widget widget, ModData modData, bool isSavePanel, OrderManager orderManager, MapPreview map)
		{
			panel = widget;

			this.modData = modData;
			this.isSavePanel = isSavePanel;
			this.orderManager = orderManager;
			this.map = map;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
			};

			saveList = panel.Get<ScrollPanelWidget>("SETTINGS_LIST");
			var newTemplate = panel.Get<ScrollItemWidget>("NEW_TEMPLATE");
			var savedTemplate = panel.Get<ScrollItemWidget>("GAME_TEMPLATE");

			baseSavePath = BaseSavePath(modData.Manifest);

			if (isSavePanel)
			{
				panel.Get("SAVE_TITLE").IsVisible = () => true;

				defaultSaveFilename = map.Title;
				var filenameAttempt = 0;
				while (true)
				{
					if (!File.Exists(SettingsFilePath(defaultSaveFilename)))
						break;

					defaultSaveFilename = map.Title + " ({0})".F(++filenameAttempt);
				}

				var saveWidgets = panel.Get("SAVE_WIDGETS");
				saveTextField = saveWidgets.Get<TextFieldWidget>("SAVE_TEXTFIELD");
				saveTextField.OnEnterKey = () =>
				{
					if (CanSave())
						Save();
					return true;
				};
				saveList.Bounds.Height -= saveWidgets.Bounds.Height;
				saveWidgets.IsVisible = () => true;

				var saveButton = panel.Get<ButtonWidget>("SAVE_BUTTON");
				saveButton.OnClick = Save;
				saveButton.IsVisible = () => true;
				saveButton.IsDisabled = () => !CanSave();
			}
			else
			{
				panel.Get("LOAD_TITLE").IsVisible = () => true;

				var loadButton = panel.Get<ButtonWidget>("LOAD_BUTTON");
				loadButton.OnClick = Load;
				loadButton.IsVisible = () => true;
				loadButton.IsDisabled = () => selectedSave == null;
			}

			var renameButton = panel.Get<ButtonWidget>("RENAME_BUTTON");
			renameButton.IsDisabled = () => selectedSave == null;
			renameButton.OnClick = () =>
			{
				var initialName = Path.GetFileNameWithoutExtension(selectedSave);
				var invalidChars = Path.GetInvalidFileNameChars();

				ConfirmationDialogs.TextInputPrompt(
					"Rename Save",
					"Enter a new file name:",
					initialName,
					onAccept: newName => Rename(initialName, newName),
					onCancel: null,
					acceptText: "Rename",
					cancelText: null,
					inputValidator: newName =>
					{
						if (newName == initialName)
							return false;

						if (string.IsNullOrWhiteSpace(newName))
							return false;

						if (newName.IndexOfAny(invalidChars) >= 0)
							return false;

						if (File.Exists(SettingsFilePath(newName)))
							return false;

						return true;
					});
			};

			var deleteButton = panel.Get<ButtonWidget>("DELETE_BUTTON");
			deleteButton.IsDisabled = () => selectedSave == null;
			deleteButton.OnClick = () =>
			{
				ConfirmationDialogs.ButtonPrompt(
					title: "Delete selected game save?",
					text: "Delete '{0}'?".F(Path.GetFileNameWithoutExtension(selectedSave)),
					onConfirm: () =>
					{
						Delete(selectedSave);

						if (!saves.Any() && !isSavePanel)
						{
							Ui.CloseWindow();
						}
						else
							SelectFirstVisible();
					},
					confirmText: "Delete",
					onCancel: () => { });
			};

			LoadExistingSaves(newTemplate, savedTemplate);
			SelectFirstVisible();
		}

		void SelectFirstVisible()
		{
			Select(isSavePanel ? null : saves.FirstOrDefault());
		}

		void Select(string savePath)
		{
			selectedSave = savePath;
			if (isSavePanel)
				saveTextField.Text = savePath == null ? defaultSaveFilename :
					Path.GetFileNameWithoutExtension(savePath);
		}

		void LoadExistingSaves(ScrollItemWidget newTemplate, ScrollItemWidget savedTemplate)
		{
			saveList.RemoveChildren();

			if (isSavePanel)
			{
				var item = ScrollItemWidget.Setup(newTemplate,
					() => selectedSave == null || !saves.Contains(SettingsFilePath(saveTextField.Text)),
					() => { });
				saveList.AddChild(item);
			}

			if (Directory.Exists(baseSavePath))
			{
				var saveFiles = Directory.GetFiles(baseSavePath, "*.yaml", SearchOption.AllDirectories)
					.OrderByDescending(p => File.GetLastWriteTime(p))
					.ToList();

				foreach (var savePath in saveFiles)
				{
					saves.Add(savePath);

					// Create the item manually so the click handlers can refer to itself
					// This simplifies the rename handling (only needs to update ItemKey)
					var item = savedTemplate.Clone() as ScrollItemWidget;
					item.ItemKey = savePath;
					item.IsVisible = () => true;
					item.OnClick = () => Select(item.ItemKey);
					if (isSavePanel)
					{
						item.IsSelected = () => saveTextField.Text == Path.GetFileNameWithoutExtension(item.ItemKey);
						item.OnDoubleClick = Save;
					}
					else
					{
						item.IsSelected = () => selectedSave == item.ItemKey;
						item.OnDoubleClick = Load;
					}

					var title = Path.GetFileNameWithoutExtension(savePath);
					var label = item.Get<LabelWithTooltipWidget>("TITLE");
					WidgetUtils.TruncateLabelToTooltip(label, title);

					var date = File.GetLastWriteTime(savePath).ToString("yyyy-MM-dd HH:mm:ss");
					item.Get<LabelWidget>("DATE").GetText = () => date;

					saveList.AddChild(item);
				}
			}
		}

		void Save()
		{
			var globalSettings = orderManager.LobbyInfo.GlobalSettings.Serialize();
			var options = globalSettings.Value.ToDictionary()["Options"];
			var unlockedOptions = options.Nodes.FindAll(optionYaml => optionYaml.Value.ToDictionary()["IsLocked"].Value == "False");
			var optionsYaml = new List<MiniYamlNode>()
			{
				new MiniYamlNode(SettingsKeyMapId, map.Uid),
				new MiniYamlNode(SettingsKeyOptions, new MiniYaml(options.Value, unlockedOptions))
			};

			if (!Directory.Exists(baseSavePath))
				Directory.CreateDirectory(baseSavePath);

			var filename = SettingsFilePath(saveTextField.Text);

			Action inner = () =>
			{
				optionsYaml.WriteToFile(filename);
				Ui.CloseWindow();
			};

			if (File.Exists(filename))
			{
				ConfirmationDialogs.ButtonPrompt(
					title: "Overwrite saved settings?",
					text: "Overwrite {0}?".F(saveTextField.Text),
					onConfirm: inner,
					confirmText: "Overwrite",
					onCancel: () => { });
			}
			else
				inner();
		}

		void Load()
		{
			var optionsYaml = MiniYaml.FromFile(selectedSave);

			Ui.CloseWindow();

			var savedMap = optionsYaml.Find(node => node.Key == SettingsKeyMapId).Value.Value;
			if (savedMap != map.Uid)
				orderManager.IssueOrder(Order.Command("map {0}".F(savedMap)));

			var savedOptions = optionsYaml.Find(node => node.Key == SettingsKeyOptions);
			foreach (var option in savedOptions.Value.Nodes)
			{
				string optionName = option.Key;
				string optionValue = option.Value.ToDictionary()["Value"].Value;
				orderManager.IssueOrder(Order.Command("option {0} {1}".F(optionName, optionValue)));
			}
		}

		void Rename(string oldName, string newName)
		{
			try
			{
				var oldPath = SettingsFilePath(oldName);
				var newPath = SettingsFilePath(newName);
				File.Move(oldPath, newPath);

				saves[saves.IndexOf(oldPath)] = newPath;
				foreach (var c in saveList.Children)
				{
					var item = c as ScrollItemWidget;
					if (item == null || item.ItemKey != oldPath)
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
				Game.Debug("Failed to delete save file '{0}'. See the logs for details.", savePath);
				Log.Write("debug", ex.ToString());
				return;
			}

			if (savePath == selectedSave)
				Select(null);

			var item = saveList.Children
				.Select(c => c as ScrollItemWidget)
				.FirstOrDefault(c => c.ItemKey == savePath);

			saveList.RemoveChild(item);
			saves.Remove(savePath);
		}

		bool CanSave()
		{
			return saveTextField.Text != "";
		}

		string SettingsFilePath(string id)
		{
			return Path.Combine(baseSavePath, id + ".yaml");
		}

		static string BaseSavePath(Manifest mod)
		{
			return Platform.ResolvePath(Platform.SupportDirPrefix, "SkirmishSettings", mod.Id, mod.Metadata.Version);
		}

		public static bool IsLoadPanelEnabled(Manifest mod)
		{
			var baseSavePath = BaseSavePath(mod);
			return Directory.Exists(baseSavePath) && Directory.GetFiles(baseSavePath, "*.yaml", SearchOption.AllDirectories).Any();
		}
	}
}
