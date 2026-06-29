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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SaveGeneratedMapLogic : ChromeLogic
	{
		[FluentReference]
		const string SaveMapFailedTitle = "dialog-save-map-failed.title";

		[FluentReference]
		const string SaveMapFailedPrompt = "dialog-save-map-failed.prompt";

		[FluentReference]
		const string SaveMapFailedConfirm = "dialog-save-map-failed.confirm";

		[FluentReference]
		const string MapAlreadyExistsTitle = "dialog-overwrite-generated-map.title";

		[FluentReference]
		const string MapAlreadyExistsPrompt = "dialog-overwrite-generated-map.prompt";

		[FluentReference]
		const string MapAlreadyExistsConfirm = "dialog-overwrite-generated-map.confirm";

		[FluentReference]
		const string MapAlreadyExistsCancel = "dialog-overwrite-generated-map.cancel";

		[ObjectCreator.UseCtor]
		public SaveGeneratedMapLogic(Widget widget, ModData modData, Map map, Action onSave, Action onExit)
		{
			var titleField = widget.Get<TextFieldWidget>("TITLE");
			titleField.Text = map.Title ?? "";
			titleField.TakeKeyboardFocus();

			var authorField = widget.Get<TextFieldWidget>("AUTHOR");
			authorField.Text = Game.Settings.Player.Name;

			var backButton = widget.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = () => { Ui.CloseWindow(); onExit(); };

			var saveButton = widget.Get<ButtonWidget>("SAVE_BUTTON");
			saveButton.IsDisabled = () => string.IsNullOrWhiteSpace(titleField.Text);
			saveButton.OnClick = () => TrySaveMap(modData, map, titleField.Text, authorField.Text, onSave, onExit);
		}

		static void DoSave(ModData modData, Map map, string name, string author, string path, Folder userDir, string mapFilename, Action onSave, Action onExit)
		{
			// Delete existing file if overwriting
			if (File.Exists(path))
				File.Delete(path);

			map.Title = name;
			map.Author = author;
			map.RequiresMod = modData.Manifest.Id;
			map.Visibility = MapVisibility.Lobby;

			var package = ZipFileLoader.Create(path);
			map.Save(package);

			modData.MapCache.LoadMap(mapFilename, userDir, MapClassification.User, null);

			Ui.CloseWindow();
			onSave();
			onExit();
		}

		static void TrySaveMap(ModData modData, Map map, string name, string author, Action onSave, Action onExit)
		{
			name = name.Trim();
			if (string.IsNullOrEmpty(name))
				return;

			// Find the writable User-classified maps directory
			var userDir = modData.MapCache.MapLocations
				.Where(kv => kv.Value == MapClassification.User && kv.Key is Folder)
				.Select(kv => (Folder)kv.Key)
				.FirstOrDefault();

			if (userDir == null)
			{
				Log.Write("debug", "Failed to save generated map: no writable user maps directory found.");
				ConfirmationDialogs.ButtonPrompt(modData,
					title: SaveMapFailedTitle,
					text: SaveMapFailedPrompt,
					onConfirm: () => { },
					confirmText: SaveMapFailedConfirm);
				return;
			}

			try
			{
				// Sanitize filename
				var filename = name;
				foreach (var c in Path.GetInvalidFileNameChars())
					filename = filename.Replace(c, '_');

				var mapFilename = filename + ".oramap";
				var path = Platform.ResolvePath(Path.Combine(userDir.Name, mapFilename));

				if (File.Exists(path))
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: MapAlreadyExistsTitle,
						text: MapAlreadyExistsPrompt,
						onConfirm: () => DoSave(modData, map, name, author, path, userDir, mapFilename, onSave, onExit),
						confirmText: MapAlreadyExistsConfirm,
						onCancel: () => { },
						cancelText: MapAlreadyExistsCancel);
					return;
				}

				DoSave(modData, map, name, author, path, userDir, mapFilename, onSave, onExit);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to save generated map.");
				Log.Write("debug", e);
				ConfirmationDialogs.ButtonPrompt(modData,
					title: SaveMapFailedTitle,
					text: SaveMapFailedPrompt,
					onConfirm: () => { },
					confirmText: SaveMapFailedConfirm);
			}
		}
	}
}
