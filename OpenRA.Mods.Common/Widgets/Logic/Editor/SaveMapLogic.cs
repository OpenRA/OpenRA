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
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;
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
		const string SaveMapFailedTitle = "dialog-save-map-failed.title";

		[TranslationReference]
		const string SaveMapFailedPrompt = "dialog-save-map-failed.prompt";

		[TranslationReference]
		const string SaveMapFailedConfirm = "dialog-save-map-failed.confirm";

		[TranslationReference]
		const string Unpacked = "label-unpacked-map";

		[TranslationReference]
		const string OverwriteMapFailedTitle = "dialog-overwrite-map-failed.title";

		[TranslationReference]
		const string OverwriteMapFailedPrompt = "dialog-overwrite-map-failed.prompt";

		[TranslationReference]
		const string OverwriteMapFailedConfirm = "dialog-overwrite-map-failed.confirm";

		[TranslationReference]
		const string OverwriteMapOutsideEditTitle = "dialog-overwrite-map-outside-edit.title";

		[TranslationReference]
		const string OverwriteMapOutsideEditPrompt = "dialog-overwrite-map-outside-edit.prompt";

		[TranslationReference]
		const string SaveMapMapOutsideConfirm = "dialog-overwrite-map-outside-edit.confirm";

		[TranslationReference]
		const string SaveCurrentMap = "notification-save-current-map";

		[ObjectCreator.UseCtor]
		public SaveMapLogic(Widget widget, ModData modData, Map map, Action<string> onSave, Action onExit,
			World world, List<MiniYamlNode> playerDefinitions, List<MiniYamlNode> actorDefinitions)
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
				ScrollItemWidget SetupItem(SaveDirectory option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedDirectory == option,
						() => selectedDirectory = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option.DisplayName;
					return item;
				}

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
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, writableDirectories, SetupItem);
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
				ScrollItemWidget SetupItem(KeyValuePair<MapFileType, MapFileTypeInfo> option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => fileType == option.Key,
						() => { typeDropdown.Text = option.Value.UiLabel; fileType = option.Key; });
					item.Get<LabelWidget>("LABEL").GetText = () => option.Value.UiLabel;
					return item;
				}

				typeDropdown.Text = fileTypes[fileType].UiLabel;

				typeDropdown.OnClick = () =>
					typeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, fileTypes, SetupItem);
			}

			var close = widget.Get<ButtonWidget>("BACK_BUTTON");
			close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			void SaveMap(string combinedPath)
			{
				map.Title = title.Text;
				map.Author = author.Text;

				if (actorDefinitions != null)
					map.ActorDefinitions = actorDefinitions;

				if (playerDefinitions != null)
					map.PlayerDefinitions = playerDefinitions;

				Ui.CloseWindow();
				onExit();

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

					SaveMapInner(map, package, world, modData);
				}
				catch (Exception e)
				{
					SaveMapFailed(e, modData, world);
				}

				onSave(map.Uid);
			}

			var save = widget.Get<ButtonWidget>("SAVE_BUTTON");
			save.IsDisabled = () => string.IsNullOrWhiteSpace(filename.Text) || string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(author.Text);

			save.OnClick = () =>
			{
				var combinedPath = Platform.ResolvePath(Path.Combine(selectedDirectory.Folder.Name, filename.Text + fileTypes[fileType].Extension));
				SaveMapLogic.SaveMap(modData, world, map, combinedPath, SaveMap);
			};
		}

		public static void SaveMap(ModData modData, World world, Map map, string combinedPath, Action<string> saveMap)
		{
			var actionManager = world.WorldActor.TraitOrDefault<EditorActionManager>();

			if (map.Package?.Name != combinedPath)
			{
				// When creating a new map or when file paths don't match
				if (modData.MapCache.Any(m => m.Status == MapStatus.Available && m.Package?.Name == combinedPath))
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: OverwriteMapFailedTitle,
						text: OverwriteMapFailedPrompt,
						confirmText: OverwriteMapFailedConfirm,
						onConfirm: () =>
						{
							saveMap(combinedPath);
							if (actionManager != null)
								actionManager.SaveFailed = false;
						},
						onCancel: () =>
						{
							if (actionManager != null)
								actionManager.SaveFailed = false;
						});

					if (actionManager != null)
						actionManager.SaveFailed = true;

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
						onConfirm: () =>
						{
							saveMap(combinedPath);
							if (actionManager != null)
								actionManager.SaveFailed = false;
						},
						onCancel: () =>
						{
							if (actionManager != null)
								actionManager.SaveFailed = false;
						});

					if (actionManager != null)
						actionManager.SaveFailed = true;

					return;
				}
			}

			saveMap(combinedPath);
		}

		public static void SaveMapInner(Map map, IReadWritePackage package, World world, ModData modData)
		{
			map.RequiresMod = modData.Manifest.Id;

			try
			{
				if (package == null)
					throw new ArgumentNullException(nameof(package));

				map.Save(package);

				var actionManager = world.WorldActor.TraitOrDefault<EditorActionManager>();
				if (actionManager != null)
					actionManager.Modified = false;

				TextNotificationsManager.AddTransientLine(modData.Translation.GetString(SaveCurrentMap), world.LocalPlayer);
			}
			catch (Exception e)
			{
				SaveMapFailed(e, modData, world);
			}
		}

		static void SaveMapFailed(Exception e, ModData modData, World world)
		{
			Log.Write("debug", $"Failed to save map.");
			Log.Write("debug", e);

			var actionManager = world.WorldActor.TraitOrDefault<EditorActionManager>();
			if (actionManager != null)
				actionManager.SaveFailed = true;

			ConfirmationDialogs.ButtonPrompt(modData,
				title: SaveMapFailedTitle,
				text: SaveMapFailedPrompt,
				onConfirm: () =>
				{
					if (actionManager != null)
						actionManager.SaveFailed = false;
				},
				confirmText: SaveMapFailedConfirm);
		}
	}
}
