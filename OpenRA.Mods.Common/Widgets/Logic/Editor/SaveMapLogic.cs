#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SaveMapLogic : ChromeLogic
	{
		enum MapFileType { Unpacked, OraMap }

		struct MapFileTypeInfo
		{
			public string Extension;
			public string UiLabel;
		}

		class SaveDirectory
		{
			public readonly Folder Folder;
			public readonly string DisplayName;
			public readonly MapClassification Classification;

			public SaveDirectory(Folder folder, string displayName, MapClassification classification)
			{
				Folder = folder;
				DisplayName = displayName;
				Classification = classification;
			}
		}

		[TranslationReference]
		static readonly string SaveMapFailedTitle = "save-map-failed-title";

		[TranslationReference]
		static readonly string SaveMapFailedPrompt = "save-map-failed-prompt";

		[TranslationReference]
		static readonly string SaveMapFailedAccept = "save-map-failed-accept";

		[TranslationReference]
		static readonly string Unpacked = "unpacked";

		[TranslationReference]
		static readonly string OverwriteMapFailedTitle = "overwrite-map-failed-title";

		[TranslationReference]
		static readonly string OverwriteMapFailedPrompt = "overwrite-map-failed-prompt";

		[TranslationReference]
		static readonly string SaveMapFailedConfirm = "overwrite-map-failed-confirm";

		[TranslationReference]
		static readonly string OverwriteMapOutsideEditTitle = "overwrite-map-outside-edit-title";

		[TranslationReference]
		static readonly string OverwriteMapOutsideEditPrompt = "overwrite-map-outside-edit-prompt";

		[TranslationReference]
		static readonly string SaveMapMapOutsideConfirm = "overwrite-map-outside-edit-confirm";

		[ObjectCreator.UseCtor]
		public SaveMapLogic(Widget widget, ModData modData, Action<string> onSave, Action onExit,
			Map map, List<MiniYamlNode> playerDefinitions, List<MiniYamlNode> actorDefinitions)
		{
			var title = widget.Get<TextFieldWidget>("TITLE");
			title.Text = map.Title;

			var author = widget.Get<TextFieldWidget>("AUTHOR");
			author.Text = map.Author;

			var visibilityPanel = Ui.LoadWidget("MAP_SAVE_VISIBILITY_PANEL", null, new WidgetArgs());
			var visOptionTemplate = visibilityPanel.Get<CheckboxWidget>("VISIBILITY_TEMPLATE");
			visibilityPanel.RemoveChildren();

			foreach (MapVisibility visibilityOption in Enum.GetValues(typeof(MapVisibility)))
			{
				// To prevent users from breaking the game only show the 'Shellmap' option when it is already set.
				if (visibilityOption == MapVisibility.Shellmap && !map.Visibility.HasFlag(visibilityOption))
					continue;

				var checkbox = (CheckboxWidget)visOptionTemplate.Clone();
				checkbox.GetText = () => visibilityOption.ToString();
				checkbox.IsChecked = () => map.Visibility.HasFlag(visibilityOption);
				checkbox.OnClick = () => map.Visibility ^= visibilityOption;
				visibilityPanel.AddChild(checkbox);
			}

			var visibilityDropdown = widget.Get<DropDownButtonWidget>("VISIBILITY_DROPDOWN");
			visibilityDropdown.OnMouseDown = _ =>
			{
				visibilityDropdown.RemovePanel();
				visibilityDropdown.AttachPanel(visibilityPanel);
			};

			var writableDirectories = new List<SaveDirectory>();
			SaveDirectory selectedDirectory = null;

			var directoryDropdown = widget.Get<DropDownButtonWidget>("DIRECTORY_DROPDOWN");
			{
				Func<SaveDirectory, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedDirectory == option,
						() => selectedDirectory = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option.DisplayName;
					return item;
				};

				foreach (var kv in modData.MapCache.MapLocations)
				{
					if (!(kv.Key is Folder folder))
						continue;

					try
					{
						using (var fs = File.Create(Path.Combine(folder.Name, ".testwritable"), 1, FileOptions.DeleteOnClose))
						{
							// Do nothing: we just want to test whether we can create the file
						}

						writableDirectories.Add(new SaveDirectory(folder, kv.Value.ToString(), kv.Value));
					}
					catch
					{
						// Directory is not writable
					}
				}

				if (map.Package != null)
				{
					selectedDirectory = writableDirectories.FirstOrDefault(k => k.Folder.Contains(map.Package.Name));
					if (selectedDirectory == null)
						selectedDirectory = writableDirectories.FirstOrDefault(k => Directory.GetDirectories(k.Folder.Name).Any(f => f.Contains(map.Package.Name)));
				}

				// Prioritize MapClassification.User directories over system directories
				if (selectedDirectory == null)
					selectedDirectory = writableDirectories.OrderByDescending(kv => kv.Classification).First();

				directoryDropdown.GetText = () => selectedDirectory?.DisplayName ?? "";
				directoryDropdown.OnClick = () =>
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, writableDirectories, setupItem);
			}

			var mapIsUnpacked = map.Package != null && map.Package is Folder;

			var filename = widget.Get<TextFieldWidget>("FILENAME");
			filename.Text = map.Package == null ? "" : mapIsUnpacked ? Path.GetFileName(map.Package.Name) : Path.GetFileNameWithoutExtension(map.Package.Name);
			if (string.IsNullOrEmpty(filename.Text))
				filename.TakeKeyboardFocus();

			var fileType = mapIsUnpacked ? MapFileType.Unpacked : MapFileType.OraMap;

			var fileTypes = new Dictionary<MapFileType, MapFileTypeInfo>()
			{
				{ MapFileType.OraMap, new MapFileTypeInfo { Extension = ".oramap", UiLabel = ".oramap" } },
				{ MapFileType.Unpacked, new MapFileTypeInfo { Extension = "", UiLabel = $"({modData.Translation.GetString(Unpacked)})" } }
			};

			var typeDropdown = widget.Get<DropDownButtonWidget>("TYPE_DROPDOWN");
			{
				Func<KeyValuePair<MapFileType, MapFileTypeInfo>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => fileType == option.Key,
						() => { typeDropdown.Text = option.Value.UiLabel; fileType = option.Key; });
					item.Get<LabelWidget>("LABEL").GetText = () => option.Value.UiLabel;
					return item;
				};

				typeDropdown.Text = fileTypes[fileType].UiLabel;

				typeDropdown.OnClick = () =>
					typeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, fileTypes, setupItem);
			}

			var close = widget.Get<ButtonWidget>("BACK_BUTTON");
			close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			Action<string> saveMap = (string combinedPath) =>
			{
				map.Title = title.Text;
				map.Author = author.Text;

				if (actorDefinitions != null)
					map.ActorDefinitions = actorDefinitions;

				if (playerDefinitions != null)
					map.PlayerDefinitions = playerDefinitions;

				map.RequiresMod = modData.Manifest.Id;

				try
				{
					if (!(map.Package is IReadWritePackage package) || package.Name != combinedPath)
					{
						selectedDirectory.Folder.Delete(combinedPath);
						if (fileType == MapFileType.OraMap)
							package = ZipFileLoader.Create(combinedPath);
						else
							package = new Folder(combinedPath);
					}

					map.Save(package);

					Ui.CloseWindow();
					onSave(map.Uid);
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Failed to save map at {combinedPath}");
					Log.Write("debug", e);

					ConfirmationDialogs.ButtonPrompt(modData,
						title: SaveMapFailedTitle,
						text: SaveMapFailedPrompt,
						onConfirm: () => { },
						confirmText: SaveMapFailedAccept);
				}
			};

			var save = widget.Get<ButtonWidget>("SAVE_BUTTON");
			save.IsDisabled = () => string.IsNullOrWhiteSpace(filename.Text) || string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(author.Text);

			save.OnClick = () =>
			{
				var combinedPath = Platform.ResolvePath(Path.Combine(selectedDirectory.Folder.Name, filename.Text + fileTypes[fileType].Extension));

				if (map.Package?.Name != combinedPath)
				{
					// When creating a new map or when file paths don't match
					if (modData.MapCache.Any(m => m.Status == MapStatus.Available && m.Package?.Name == combinedPath))
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: OverwriteMapFailedTitle,
							text: OverwriteMapFailedPrompt,
							confirmText: SaveMapFailedConfirm,
							onConfirm: () => saveMap(combinedPath),
							onCancel: () => { });

						return;
					}
				}
				else
				{
					// When file paths match
					var recentUid = modData.MapCache.GetUpdatedMap(map.Uid);
					if (recentUid != null && map.Uid != recentUid && modData.MapCache[recentUid].Status == MapStatus.Available)
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: OverwriteMapOutsideEditTitle,
							text: OverwriteMapOutsideEditPrompt,
							confirmText: SaveMapMapOutsideConfirm,
							onConfirm: () => saveMap(combinedPath),
							onCancel: () => { });

						return;
					}
				}

				saveMap(combinedPath);
			};
		}
	}
}
